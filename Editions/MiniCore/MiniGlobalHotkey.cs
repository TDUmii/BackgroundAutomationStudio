using System.Windows;
using System.Windows.Interop;

namespace BackgroundAutomationStudio.MiniCore;

public sealed class MiniGlobalHotkey : IDisposable
{
    private const int Id = 0xB560;
    private HwndSource? _source;
    private nint _hwnd;
    public event EventHandler? Pressed;

    public bool Register(Window window)
    {
        _hwnd = new WindowInteropHelper(window).Handle;
        _source = HwndSource.FromHwnd(_hwnd);
        _source?.AddHook(WindowProc);
        return MiniNative.RegisterHotKey(_hwnd, Id, 0x0002 | 0x0004, 0x78);
    }

    private nint WindowProc(nint hwnd, int message, nint wParam, nint lParam, ref bool handled)
    {
        if (message == 0x0312 && wParam.ToInt32() == Id) { handled = true; Pressed?.Invoke(this, EventArgs.Empty); }
        return nint.Zero;
    }

    public void Dispose()
    {
        if (_hwnd != nint.Zero) MiniNative.UnregisterHotKey(_hwnd, Id);
        _source?.RemoveHook(WindowProc); _source = null; _hwnd = nint.Zero;
    }
}
