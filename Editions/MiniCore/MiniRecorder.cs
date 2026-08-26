using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace BackgroundAutomationStudio.MiniCore;

public sealed class MiniRecorder : IDisposable
{
    private readonly List<RecordedMiniStep> _steps = [];
    private MiniNative.HookProc? _mouseProc;
    private MiniNative.HookProc? _keyboardProc;
    private nint _mouseHook;
    private nint _keyboardHook;
    private nint _target;
    private Stopwatch _clock = new();
    private long _lastEvent;

    public bool IsRecording => _mouseHook != nint.Zero || _keyboardHook != nint.Zero;
    public event EventHandler<RecordedMiniStep>? StepCaptured;

    public void Start(nint target)
    {
        if (IsRecording) throw new InvalidOperationException("Recording is already active.");
        if (!MiniNative.IsWindow(target)) throw new InvalidOperationException("Select an open target window first.");
        _steps.Clear(); _target = target; _clock.Restart(); _lastEvent = 0;
        _mouseProc = MouseHook; _keyboardProc = KeyboardHook;
        var module = MiniNative.GetModuleHandle(null);
        _mouseHook = MiniNative.SetWindowsHookEx(14, _mouseProc, module, 0);
        _keyboardHook = MiniNative.SetWindowsHookEx(13, _keyboardProc, module, 0);
        if (_mouseHook == nint.Zero || _keyboardHook == nint.Zero) { Stop(); throw new InvalidOperationException("Windows could not start the recording hooks."); }
    }

    public IReadOnlyList<RecordedMiniStep> Stop()
    {
        if (_mouseHook != nint.Zero) MiniNative.UnhookWindowsHookEx(_mouseHook);
        if (_keyboardHook != nint.Zero) MiniNative.UnhookWindowsHookEx(_keyboardHook);
        _mouseHook = _keyboardHook = nint.Zero; _mouseProc = _keyboardProc = null; _clock.Stop();
        return _steps.ToList();
    }

    private nint MouseHook(int code, nint wParam, nint lParam)
    {
        if (code >= 0 && wParam is 0x0201 or 0x0204 or 0x020A)
        {
            var data = Marshal.PtrToStructure<MiniNative.MSLLHOOKSTRUCT>(lParam);
            var hit = MiniNative.WindowFromPoint(data.Point);
            if (MiniWindowService.IsTargetOrChild(_target, hit))
            {
                var point = MiniWindowService.ScreenToClient(_target, data.Point.X, data.Point.Y);
                var type = wParam == 0x0201 ? "Click" : wParam == 0x0204 ? "RightClick" : "Scroll";
                var wheel = type == "Scroll" ? (short)(data.MouseData >> 16) : 0;
                Add(new(type, NextDelay(), point.X, point.Y, wheel));
            }
        }
        return MiniNative.CallNextHookEx(nint.Zero, code, wParam, lParam);
    }

    private nint KeyboardHook(int code, nint wParam, nint lParam)
    {
        if (code >= 0 && wParam is 0x0100 or 0x0104 && MiniWindowService.IsTargetOrChild(_target, MiniNative.GetForegroundWindow()))
        {
            var data = Marshal.PtrToStructure<MiniNative.KBDLLHOOKSTRUCT>(lParam);
            var key = KeyInterop.KeyFromVirtualKey((int)data.VkCode).ToString().ToUpperInvariant();
            Add(new("Key", NextDelay(), Key: key));
        }
        return MiniNative.CallNextHookEx(nint.Zero, code, wParam, lParam);
    }

    private int NextDelay()
    {
        var now = _clock.ElapsedMilliseconds;
        var delay = (int)Math.Clamp(now - _lastEvent, 0, int.MaxValue);
        _lastEvent = now;
        return delay;
    }

    private void Add(RecordedMiniStep step) { _steps.Add(step); StepCaptured?.Invoke(this, step); }
    public void Dispose() => Stop();
}
