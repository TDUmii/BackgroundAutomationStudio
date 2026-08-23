using System.Windows.Input;
using System.Windows.Interop;
using BackgroundAutomationStudio.Native;

namespace BackgroundAutomationStudio.Services;

public sealed class GlobalHotkeyService : IDisposable
{
    private const int WmHotkey = 0x0312;
    private readonly int _id;
    private IntPtr _hwnd;
    private HwndSource? _source;
    public event EventHandler? Pressed;

    public GlobalHotkeyService(int id = 0xB451) => _id = id;

    public bool Register(IntPtr hwnd, string hotkey)
    {
        Unregister();
        if (!HotkeyParser.TryParse(hotkey, out var modifiers, out var key)) return false;
        _hwnd = hwnd;
        _source = HwndSource.FromHwnd(hwnd);
        _source?.AddHook(WndProc);
        if (NativeMethods.RegisterHotKey(hwnd, _id, modifiers, key)) return true;
        _source?.RemoveHook(WndProc); _source = null; _hwnd = IntPtr.Zero;
        return false;
    }

    private IntPtr WndProc(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message == WmHotkey && wParam.ToInt32() == _id) { handled = true; Pressed?.Invoke(this, EventArgs.Empty); }
        return IntPtr.Zero;
    }

    public void Unregister()
    {
        if (_hwnd != IntPtr.Zero) NativeMethods.UnregisterHotKey(_hwnd, _id);
        _source?.RemoveHook(WndProc); _source = null; _hwnd = IntPtr.Zero;
    }

    public void Dispose() => Unregister();
}

public static class HotkeyParser
{
    public static bool TryParse(string text, out uint modifiers, out uint virtualKey)
    {
        modifiers = 0; virtualKey = 0;
        var parts = text.ToUpperInvariant().Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var part in parts)
        {
            switch (part)
            {
                case "ALT": modifiers |= 0x0001; break;
                case "CTRL": case "CONTROL": modifiers |= 0x0002; break;
                case "SHIFT": modifiers |= 0x0004; break;
                case "WIN": case "WINDOWS": modifiers |= 0x0008; break;
                default:
                    try { virtualKey = BackgroundAutomationRunner.ToVirtualKey(part); }
                    catch { return false; }
                    break;
            }
        }
        return modifiers != 0 && virtualKey != 0;
    }

    public static string FromKeyEvent(Key key, ModifierKeys modifiers)
    {
        var parts = new List<string>();
        if (modifiers.HasFlag(ModifierKeys.Control)) parts.Add("CTRL");
        if (modifiers.HasFlag(ModifierKeys.Shift)) parts.Add("SHIFT");
        if (modifiers.HasFlag(ModifierKeys.Alt)) parts.Add("ALT");
        if (modifiers.HasFlag(ModifierKeys.Windows)) parts.Add("WIN");
        var name = key switch { >= Key.F1 and <= Key.F24 => key.ToString().ToUpperInvariant(), >= Key.A and <= Key.Z => key.ToString().ToUpperInvariant(), >= Key.D0 and <= Key.D9 => key.ToString()[1..], _ => string.Empty };
        if (!string.IsNullOrEmpty(name)) parts.Add(name);
        return parts.Count >= 2 ? string.Join('+', parts) : string.Empty;
    }
}
