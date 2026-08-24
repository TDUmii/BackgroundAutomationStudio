using BackgroundAutomationStudio.Native;

namespace BackgroundAutomationStudio.Services;

internal static class BackgroundMessageTargetResolver
{
    private const int MaxChildDepth = 32;
    private const uint SearchFlags = NativeMethods.CwpSkipInvisible |
                                     NativeMethods.CwpSkipDisabled |
                                     NativeMethods.CwpSkipTransparent;

    public static (IntPtr Hwnd, POINT ClientPoint) Resolve(IntPtr root, int rootClientX, int rootClientY)
    {
        if (root == IntPtr.Zero || !NativeMethods.IsWindow(root))
            throw new InvalidOperationException("The target window is no longer available.");

        var current = root;
        var currentPoint = new POINT(rootClientX, rootClientY);
        for (var depth = 0; depth < MaxChildDepth; depth++)
        {
            var child = NativeMethods.ChildWindowFromPointEx(current, currentPoint, SearchFlags);
            if (child == IntPtr.Zero || child == current) break;
            if (NativeMethods.GetAncestor(child, NativeMethods.GaRoot) != root) break;

            var screenPoint = currentPoint;
            if (!NativeMethods.ClientToScreen(current, ref screenPoint) ||
                !NativeMethods.ScreenToClient(child, ref screenPoint)) break;

            current = child;
            currentPoint = screenPoint;
        }

        return (current, currentPoint);
    }

    public static POINT TranslateRootPoint(IntPtr root, IntPtr recipient, int rootClientX, int rootClientY)
    {
        var point = new POINT(rootClientX, rootClientY);
        if (recipient == root) return point;
        if (!NativeMethods.ClientToScreen(root, ref point) || !NativeMethods.ScreenToClient(recipient, ref point))
            throw new InvalidOperationException("Could not address the target control.");
        return point;
    }
}
