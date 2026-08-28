using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace BackgroundAutomationStudio.MiniCore;

public sealed class MiniWindowService
{
    public IReadOnlyList<MiniWindowTarget> GetWindows()
    {
        var currentPid = Environment.ProcessId;
        var result = new List<MiniWindowTarget>();
        MiniNative.EnumWindows((hwnd, _) =>
        {
            if (!MiniNative.IsWindowVisible(hwnd) || MiniNative.GetWindowTextLength(hwnd) == 0) return true;
            MiniNative.GetWindowThreadProcessId(hwnd, out var pid);
            if (pid == 0 || pid == currentPid) return true;
            var title = new StringBuilder(512);
            MiniNative.GetWindowText(hwnd, title, title.Capacity);
            try
            {
                var process = Process.GetProcessById((int)pid);
                result.Add(new MiniWindowTarget(hwnd, title.ToString(), process.ProcessName));
            }
            catch { }
            return true;
        }, nint.Zero);
        return result.OrderBy(item => item.ProcessName).ThenBy(item => item.Title).ToList();
    }

    public static bool IsTargetOrChild(nint target, nint candidate) => candidate != nint.Zero && MiniNative.GetAncestor(candidate, 2) == target;

    public static MiniPoint ScreenToClient(nint target, int x, int y)
    {
        var point = new MiniNative.POINT(x, y);
        if (!MiniNative.ScreenToClient(target, ref point)) throw new InvalidOperationException("Could not convert the selected point to target coordinates.");
        return new MiniPoint(point.X, point.Y);
    }

    public static bool TryPackClientPoint(nint target, int x, int y, out nint packed)
    {
        packed = nint.Zero;
        if (x < 0 || y < 0 || x > short.MaxValue || y > short.MaxValue || !MiniNative.GetClientRect(target, out var rect) || x >= rect.Right || y >= rect.Bottom) return false;
        packed = (nint)((y << 16) | (x & 0xFFFF));
        return true;
    }
}

internal static class MiniNative
{
    internal delegate bool EnumWindowsProc(nint hwnd, nint lParam);
    internal delegate nint HookProc(int code, nint wParam, nint lParam);

    [StructLayout(LayoutKind.Sequential)] internal struct POINT(int x, int y) { public int X = x; public int Y = y; }
    [StructLayout(LayoutKind.Sequential)] internal struct MSLLHOOKSTRUCT { public POINT Point; public uint MouseData; public uint Flags; public uint Time; public nuint ExtraInfo; }
    [StructLayout(LayoutKind.Sequential)] internal struct KBDLLHOOKSTRUCT { public uint VkCode; public uint ScanCode; public uint Flags; public uint Time; public nuint ExtraInfo; }
    [StructLayout(LayoutKind.Sequential)] internal struct RECT { public int Left; public int Top; public int Right; public int Bottom; }
    [StructLayout(LayoutKind.Sequential)] internal struct INPUT { public uint Type; public INPUTUNION Data; }
    [StructLayout(LayoutKind.Explicit)] internal struct INPUTUNION { [FieldOffset(0)] public MOUSEINPUT Mouse; [FieldOffset(0)] public KEYBDINPUT Keyboard; }
    [StructLayout(LayoutKind.Sequential)] internal struct MOUSEINPUT { public int X; public int Y; public uint MouseData; public uint Flags; public uint Time; public nuint ExtraInfo; }
    [StructLayout(LayoutKind.Sequential)] internal struct KEYBDINPUT { public ushort VirtualKey; public ushort ScanCode; public uint Flags; public uint Time; public nuint ExtraInfo; }

    [DllImport("user32.dll")] internal static extern bool EnumWindows(EnumWindowsProc callback, nint lParam);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] internal static extern int GetWindowText(nint hwnd, StringBuilder text, int maxCount);
    [DllImport("user32.dll")] internal static extern int GetWindowTextLength(nint hwnd);
    [DllImport("user32.dll")] internal static extern bool IsWindowVisible(nint hwnd);
    [DllImport("user32.dll")] internal static extern bool IsWindow(nint hwnd);
    [DllImport("user32.dll")] internal static extern uint GetWindowThreadProcessId(nint hwnd, out uint processId);
    [DllImport("user32.dll")] internal static extern nint GetAncestor(nint hwnd, uint flags);
    [DllImport("user32.dll")] internal static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] internal static extern nint WindowFromPoint(POINT point);
    [DllImport("user32.dll")] internal static extern bool ScreenToClient(nint hwnd, ref POINT point);
    [DllImport("user32.dll")] internal static extern bool GetClientRect(nint hwnd, out RECT rect);
    [DllImport("user32.dll")] internal static extern bool ClientToScreen(nint hwnd, ref POINT point);
    [DllImport("user32.dll")] internal static extern bool IsIconic(nint hwnd);
    [DllImport("user32.dll")] internal static extern bool ShowWindow(nint hwnd, int command);
    [DllImport("user32.dll")] internal static extern bool SetForegroundWindow(nint hwnd);
    [DllImport("user32.dll")] internal static extern int GetSystemMetrics(int index);
    [DllImport("user32.dll")] internal static extern short GetAsyncKeyState(int virtualKey);
    [DllImport("user32.dll", SetLastError = true)] internal static extern uint SendInput(uint count, INPUT[] inputs, int size);
    [DllImport("user32.dll")] internal static extern bool PostMessage(nint hwnd, uint message, nint wParam, nint lParam);
    [DllImport("user32.dll")] internal static extern nint SetWindowsHookEx(int hookId, HookProc callback, nint module, uint threadId);
    [DllImport("user32.dll")] internal static extern bool UnhookWindowsHookEx(nint hook);
    [DllImport("user32.dll")] internal static extern nint CallNextHookEx(nint hook, int code, nint wParam, nint lParam);
    [DllImport("kernel32.dll")] internal static extern nint GetModuleHandle(string? moduleName);
    [DllImport("dwmapi.dll")] internal static extern int DwmSetWindowAttribute(nint hwnd, int attribute, ref int value, int size);
    [DllImport("user32.dll")] internal static extern bool RegisterHotKey(nint hwnd, int id, uint modifiers, uint virtualKey);
    [DllImport("user32.dll")] internal static extern bool UnregisterHotKey(nint hwnd, int id);
}
