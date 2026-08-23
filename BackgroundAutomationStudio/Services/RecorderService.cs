using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using BackgroundAutomationStudio.Models;
using BackgroundAutomationStudio.Native;

namespace BackgroundAutomationStudio.Services;

public sealed class RecorderService : IDisposable
{
    private readonly IWindowManager _windowManager;
    private NativeMethods.HookProc? _mouseProc;
    private NativeMethods.HookProc? _keyboardProc;
    private IntPtr _mouseHook;
    private IntPtr _keyboardHook;
    private IntPtr _targetHwnd;
    private readonly object _sync = new();
    private readonly List<AutomationAction> _actions = [];
    private readonly StringBuilder _textBuffer = new();
    private DateTime _sessionStart;
    private DateTime _lastCommitted;
    private DateTime _textStarted;
    private PendingClick? _pendingLeft;
    private System.Threading.Timer? _clickTimer;
    private bool _ctrlDown;
    private bool _altDown;
    private bool _shiftDown;
    private bool _ctrlConsumed;
    private bool _altConsumed;
    private bool _shiftConsumed;
    public bool IsRecording { get; private set; }
    public TimeSpan Elapsed => IsRecording ? DateTime.UtcNow - _sessionStart : TimeSpan.Zero;
    public event EventHandler<AutomationAction>? ActionRecorded;

    public RecorderService(IWindowManager windowManager) => _windowManager = windowManager;

    public void Start(IntPtr targetHwnd)
    {
        if (IsRecording) throw new InvalidOperationException("Recording is already active.");
        if (!NativeMethods.IsWindow(targetHwnd)) throw new InvalidOperationException("The target window is not available.");
        lock (_sync)
        {
            _actions.Clear(); _textBuffer.Clear(); _pendingLeft = null;
            _targetHwnd = NativeMethods.GetAncestor(targetHwnd, NativeMethods.GaRoot);
            _sessionStart = _lastCommitted = DateTime.UtcNow;
        }
        _mouseProc = MouseCallback;
        _keyboardProc = KeyboardCallback;
        using var process = Process.GetCurrentProcess();
        using var module = process.MainModule!;
        var handle = NativeMethods.GetModuleHandle(module.ModuleName);
        _mouseHook = NativeMethods.SetWindowsHookEx(NativeMethods.WhMouseLl, _mouseProc, handle, 0);
        _keyboardHook = NativeMethods.SetWindowsHookEx(NativeMethods.WhKeyboardLl, _keyboardProc, handle, 0);
        if (_mouseHook == IntPtr.Zero || _keyboardHook == IntPtr.Zero) { RemoveHooks(); throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to install recorder hooks."); }
        IsRecording = true;
    }

    public IReadOnlyList<AutomationAction> Stop()
    {
        if (!IsRecording) return [];
        IsRecording = false;
        RemoveHooks();
        lock (_sync) { FlushPendingClick(); FlushText(); return _actions.Select(a => a.Clone()).ToList(); }
    }

    public void Cancel()
    {
        IsRecording = false;
        RemoveHooks();
        lock (_sync) { _clickTimer?.Dispose(); _clickTimer = null; _pendingLeft = null; _textBuffer.Clear(); _actions.Clear(); }
    }

    private IntPtr MouseCallback(int code, IntPtr wParam, IntPtr lParam)
    {
        // WM_MOUSEMOVE is deliberately never handled or stored.
        if (code >= 0 && IsRecording && (wParam.ToInt32() == NativeMethods.WmLButtonDown || wParam.ToInt32() == NativeMethods.WmRButtonDown))
        {
            var data = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
            if (_windowManager.IsPointInsideTarget(_targetHwnd, data.Point))
            {
                var client = data.Point;
                NativeMethods.ScreenToClient(_targetHwnd, ref client);
                lock (_sync)
                {
                    FlushText();
                    if (wParam.ToInt32() == NativeMethods.WmRButtonDown)
                    {
                        FlushPendingClick();
                        Commit(new RightClickAction { ClientX = client.X, ClientY = client.Y }, DateTime.UtcNow);
                    }
                    else HandleLeftClick(client);
                }
            }
        }
        return NativeMethods.CallNextHookEx(_mouseHook, code, wParam, lParam);
    }

    private IntPtr KeyboardCallback(int code, IntPtr wParam, IntPtr lParam)
    {
        var message = wParam.ToInt32();
        var isDown = message is NativeMethods.WmKeyDown or NativeMethods.WmSysKeyDown;
        var isUp = message is NativeMethods.WmKeyUp or NativeMethods.WmSysKeyUp;
        if (code < 0 || !IsRecording || (!isDown && !isUp))
            return NativeMethods.CallNextHookEx(_keyboardHook, code, wParam, lParam);
        if (NativeMethods.GetAncestor(NativeMethods.GetForegroundWindow(), NativeMethods.GaRoot) != _targetHwnd)
            return NativeMethods.CallNextHookEx(_keyboardHook, code, wParam, lParam);
        var data = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
        var vk = (int)data.VkCode;
        lock (_sync)
        {
            FlushPendingClick();
            if (vk is 0x11 or 0x12 or 0x10)
            {
                HandleModifier(vk, isDown);
                return NativeMethods.CallNextHookEx(_keyboardHook, code, wParam, lParam);
            }
            if (!isDown) return NativeMethods.CallNextHookEx(_keyboardHook, code, wParam, lParam);
            var special = SpecialKeyName(vk);
            if (_ctrlDown || _altDown)
            {
                FlushText();
                var key = special ?? ((char)vk).ToString().ToUpperInvariant();
                Commit(new KeyPressAction { KeyName = string.Join('+', new[] { _ctrlDown ? "CTRL" : null, _altDown ? "ALT" : null, _shiftDown ? "SHIFT" : null, key }.Where(x => x is not null)) }, DateTime.UtcNow);
                _ctrlConsumed |= _ctrlDown; _altConsumed |= _altDown; _shiftConsumed |= _shiftDown;
            }
            else if (special is not null)
            {
                FlushText();
                Commit(new KeyPressAction { KeyName = special }, DateTime.UtcNow);
            }
            else
            {
                var text = TranslateKey(data.VkCode, data.ScanCode);
                if (!string.IsNullOrEmpty(text))
                {
                    _shiftConsumed |= _shiftDown;
                    if (_textBuffer.Length == 0) _textStarted = DateTime.UtcNow;
                    _textBuffer.Append(text);
                }
            }
        }
        return NativeMethods.CallNextHookEx(_keyboardHook, code, wParam, lParam);
    }

    private void HandleModifier(int vk, bool isDown)
    {
        if (isDown)
        {
            if (vk == 0x11) { _ctrlDown = true; _ctrlConsumed = false; }
            else if (vk == 0x12) { _altDown = true; _altConsumed = false; }
            else { _shiftDown = true; _shiftConsumed = false; }
            return;
        }
        var (name, consumed) = vk switch { 0x11 => ("CTRL", _ctrlConsumed), 0x12 => ("ALT", _altConsumed), _ => ("SHIFT", _shiftConsumed) };
        if (!consumed) { FlushText(); Commit(new KeyPressAction { KeyName = name }, DateTime.UtcNow); }
        if (vk == 0x11) { _ctrlDown = false; _ctrlConsumed = false; }
        else if (vk == 0x12) { _altDown = false; _altConsumed = false; }
        else { _shiftDown = false; _shiftConsumed = false; }
    }

    private void HandleLeftClick(POINT point)
    {
        var now = DateTime.UtcNow;
        var maxTime = TimeSpan.FromMilliseconds(NativeMethods.GetDoubleClickTime());
        var maxX = NativeMethods.GetSystemMetrics(NativeMethods.SmCxDoubleclk);
        var maxY = NativeMethods.GetSystemMetrics(NativeMethods.SmCyDoubleclk);
        if (_pendingLeft is { } pending && now - pending.Time <= maxTime && Math.Abs(point.X - pending.Point.X) <= maxX && Math.Abs(point.Y - pending.Point.Y) <= maxY)
        {
            _clickTimer?.Dispose(); _clickTimer = null; _pendingLeft = null;
            Commit(new DoubleClickAction { ClientX = point.X, ClientY = point.Y }, pending.Time);
            return;
        }
        FlushPendingClick();
        _pendingLeft = new(point, now);
        _clickTimer = new(_ => { lock (_sync) FlushPendingClick(); }, null, (int)NativeMethods.GetDoubleClickTime() + 20, Timeout.Infinite);
    }

    private void FlushPendingClick()
    {
        _clickTimer?.Dispose(); _clickTimer = null;
        if (_pendingLeft is not { } pending) return;
        _pendingLeft = null;
        Commit(new ClickAction { ClientX = pending.Point.X, ClientY = pending.Point.Y }, pending.Time);
    }

    private void FlushText()
    {
        if (_textBuffer.Length == 0) return;
        var action = new TypeTextAction { Text = _textBuffer.ToString() };
        _textBuffer.Clear();
        Commit(action, _textStarted);
    }

    private void Commit(AutomationAction action, DateTime occurred)
    {
        var delay = Math.Max(0, (int)(occurred - _lastCommitted).TotalMilliseconds);
        if (_actions.Count > 0 && delay > 0)
        {
            var wait = new WaitAction { Milliseconds = delay };
            _actions.Add(wait); ActionRecorded?.Invoke(this, wait);
        }
        _actions.Add(action); _lastCommitted = occurred;
        ActionRecorded?.Invoke(this, action);
    }

    private static string? SpecialKeyName(int vk) => vk switch
    {
        0x0D => "ENTER", 0x09 => "TAB", 0x1B => "ESCAPE", 0x08 => "BACKSPACE", 0x2E => "DELETE",
        0x26 => "UP", 0x28 => "DOWN", 0x25 => "LEFT", 0x27 => "RIGHT",
        >= 0x70 and <= 0x7B => $"F{vk - 0x6F}", _ => null
    };

    private static string TranslateKey(uint vk, uint scan)
    {
        var state = new byte[256];
        if (!NativeMethods.GetKeyboardState(state)) return string.Empty;
        var buffer = new StringBuilder(8);
        var count = NativeMethods.ToUnicodeEx(vk, scan, state, buffer, buffer.Capacity, 0, NativeMethods.GetKeyboardLayout(0));
        return count > 0 ? buffer.ToString(0, count) : string.Empty;
    }

    private void RemoveHooks()
    {
        if (_mouseHook != IntPtr.Zero) NativeMethods.UnhookWindowsHookEx(_mouseHook);
        if (_keyboardHook != IntPtr.Zero) NativeMethods.UnhookWindowsHookEx(_keyboardHook);
        _mouseHook = _keyboardHook = IntPtr.Zero;
        _mouseProc = _keyboardProc = null;
    }

    public void Dispose() => Cancel();
    private sealed record PendingClick(POINT Point, DateTime Time);
}
