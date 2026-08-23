using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using BackgroundAutomationStudio.Models;
using BackgroundAutomationStudio.Native;

namespace BackgroundAutomationStudio.Services;

public interface IWindowManager
{
    WindowTarget GetTarget(IntPtr hwnd);
    IntPtr Resolve(WindowTarget target);
    void RestoreLayout(WindowTarget target, IntPtr hwnd);
    void Activate(IntPtr hwnd);
    bool IsPointInsideTarget(IntPtr targetHwnd, POINT screenPoint);
}

public sealed class WindowManager : IWindowManager
{
    public WindowTarget GetTarget(IntPtr hwnd)
    {
        hwnd = NativeMethods.GetAncestor(hwnd, NativeMethods.GaRoot);
        if (hwnd == IntPtr.Zero || !NativeMethods.IsWindow(hwnd)) throw new InvalidOperationException("The selected window is no longer available.");
        NativeMethods.GetWindowThreadProcessId(hwnd, out var pid);
        using var process = Process.GetProcessById((int)pid);
        if (!NativeMethods.GetWindowRect(hwnd, out var rect)) throw new Win32Exception("Unable to read the selected window bounds.");
        var title = ReadWindowText(hwnd);
        return new WindowTarget
        {
            ProcessName = process.ProcessName + ".exe",
            ProcessId = (int)pid,
            WindowTitle = title,
            WindowTitleContains = CreateStableTitleFragment(title),
            WindowClassName = ReadClassName(hwnd),
            RecordedX = rect.Left,
            RecordedY = rect.Top,
            RecordedWidth = rect.Width,
            RecordedHeight = rect.Height,
            LastKnownHwnd = hwnd.ToInt64()
        };
    }

    public IntPtr Resolve(WindowTarget target)
    {
        var last = new IntPtr(target.LastKnownHwnd);
        if (last != IntPtr.Zero && NativeMethods.IsWindow(last) && Matches(last, target)) return last;
        IntPtr exact = IntPtr.Zero;
        IntPtr contains = IntPtr.Zero;
        NativeMethods.EnumWindows((hwnd, _) =>
        {
            if (!NativeMethods.IsWindowVisible(hwnd) || ReadClassName(hwnd) != target.WindowClassName) return true;
            NativeMethods.GetWindowThreadProcessId(hwnd, out var pid);
            try
            {
                using var process = Process.GetProcessById((int)pid);
                if (!string.Equals(process.ProcessName + ".exe", target.ProcessName, StringComparison.OrdinalIgnoreCase)) return true;
            }
            catch { return true; }
            var title = ReadWindowText(hwnd);
            if (string.Equals(title, target.WindowTitle, StringComparison.OrdinalIgnoreCase)) { exact = hwnd; return false; }
            if (contains == IntPtr.Zero && !string.IsNullOrWhiteSpace(target.WindowTitleContains) && title.Contains(target.WindowTitleContains, StringComparison.OrdinalIgnoreCase)) contains = hwnd;
            return true;
        }, IntPtr.Zero);
        var result = exact != IntPtr.Zero ? exact : contains;
        if (result != IntPtr.Zero) target.LastKnownHwnd = result.ToInt64();
        return result;
    }

    public void RestoreLayout(WindowTarget target, IntPtr hwnd)
    {
        if (target.RecordedWidth <= 0 || target.RecordedHeight <= 0) throw new InvalidOperationException("The project has no valid recorded window layout.");
        if (NativeMethods.IsIconic(hwnd)) NativeMethods.ShowWindow(hwnd, NativeMethods.SwRestore);
        if (!NativeMethods.SetWindowPos(hwnd, IntPtr.Zero, target.RecordedX, target.RecordedY, target.RecordedWidth, target.RecordedHeight, NativeMethods.SwpNoZOrder | NativeMethods.SwpNoActivate))
            throw new Win32Exception("Windows could not restore the target layout.");
    }

    public void Activate(IntPtr hwnd)
    {
        if (NativeMethods.IsIconic(hwnd)) NativeMethods.ShowWindow(hwnd, NativeMethods.SwRestore);
        if (!NativeMethods.SetForegroundWindow(hwnd)) throw new InvalidOperationException("Windows blocked foreground activation. Select the target window once, then try Run again.");
    }

    public bool IsPointInsideTarget(IntPtr targetHwnd, POINT screenPoint)
    {
        var atPoint = NativeMethods.GetAncestor(NativeMethods.WindowFromPoint(screenPoint), NativeMethods.GaRoot);
        return atPoint == NativeMethods.GetAncestor(targetHwnd, NativeMethods.GaRoot);
    }

    private static bool Matches(IntPtr hwnd, WindowTarget target)
    {
        NativeMethods.GetWindowThreadProcessId(hwnd, out var pid);
        try
        {
            using var process = Process.GetProcessById((int)pid);
            return string.Equals(process.ProcessName + ".exe", target.ProcessName, StringComparison.OrdinalIgnoreCase)
                && string.Equals(ReadClassName(hwnd), target.WindowClassName, StringComparison.Ordinal)
                && (string.Equals(ReadWindowText(hwnd), target.WindowTitle, StringComparison.OrdinalIgnoreCase)
                    || ReadWindowText(hwnd).Contains(target.WindowTitleContains, StringComparison.OrdinalIgnoreCase));
        }
        catch { return false; }
    }

    private static string ReadWindowText(IntPtr hwnd)
    {
        var text = new StringBuilder(1024);
        NativeMethods.GetWindowText(hwnd, text, text.Capacity);
        return text.ToString();
    }

    private static string ReadClassName(IntPtr hwnd)
    {
        var text = new StringBuilder(256);
        NativeMethods.GetClassName(hwnd, text, text.Capacity);
        return text.ToString();
    }

    private static string CreateStableTitleFragment(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return string.Empty;
        var separators = new[] { " - ", " — ", " – " };
        foreach (var separator in separators)
        {
            var parts = title.Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length > 1) return parts.OrderByDescending(p => p.Length).First();
        }
        return title;
    }
}
