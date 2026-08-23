using System.Runtime.InteropServices;
using System.Windows.Automation;
using BackgroundAutomationStudio.Models;
using BackgroundAutomationStudio.Native;

namespace BackgroundAutomationStudio.Services;

public sealed class BackgroundAutomationRunner : IAutomationRunner, IDisposable
{
    private readonly IWindowManager _windowManager;
    private readonly Func<string> _playbackModeProvider;
    private readonly ManualResetEventSlim _pauseGate = new(true);
    private CancellationTokenSource? _runCts;

    public BackgroundAutomationRunner(IWindowManager windowManager, Func<string>? playbackModeProvider = null)
    {
        _windowManager = windowManager;
        _playbackModeProvider = playbackModeProvider ?? (() => PlaybackModes.Automatic);
    }

    public bool IsRunning { get; private set; }
    public bool IsPaused => IsRunning && !_pauseGate.IsSet;
    public event EventHandler<AutomationAction?>? CurrentActionChanged;
    public event EventHandler<string>? StatusChanged;

    public async Task RunAsync(WindowTarget target, IReadOnlyList<AutomationAction> actions, int repeatCount = 1, CancellationToken cancellationToken = default)
    {
        if (IsRunning) throw new InvalidOperationException("A workflow is already running.");
        var hwnd = _windowManager.Resolve(target);
        if (hwnd == IntPtr.Zero) throw new InvalidOperationException("Target window was not found. Open it or use Select window again.");
        if (!NativeMethods.IsWindowVisible(hwnd)) throw new InvalidOperationException(L("The target is hidden. Show its window before running the workflow.", "Cửa sổ đích đang bị ẩn. Hãy hiển thị cửa sổ trước khi chạy quy trình."));
        repeatCount = Math.Clamp(repeatCount, 1, 999);
        var playbackMode = PlaybackModes.Normalize(_playbackModeProvider());
        _runCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = _runCts.Token;
        IsRunning = true;
        _pauseGate.Set();
        try
        {
            var wasMinimized = NativeMethods.IsIconic(hwnd);
            StatusChanged?.Invoke(this, L("Restoring target layout without activation...", "Đang khôi phục bố cục cửa sổ đích mà không kích hoạt..."));
            _windowManager.RestoreLayout(target, hwnd);
            await Task.Delay(150, token);
            if (wasMinimized) StatusChanged?.Invoke(this, L("Target was minimized - restored without activation", "Cửa sổ đích đã thu nhỏ - đã khôi phục mà không kích hoạt"));
            for (var iteration = 1; iteration <= repeatCount; iteration++)
            {
                foreach (var action in actions.Where(a => a.Enabled))
                {
                    token.ThrowIfCancellationRequested();
                    await WaitWhilePausedAsync(token);
                    CurrentActionChanged?.Invoke(this, action);
                    StatusChanged?.Invoke(this, L($"Run {iteration}/{repeatCount} - {action.ActionType}", $"Lần chạy {iteration}/{repeatCount} - {action.ActionType}"));
                    if (action.DelayBefore > 0) await DelayWithPauseAsync(action.DelayBefore, token);
                    switch (action)
                    {
                        case WaitAction wait: await DelayWithPauseAsync(wait.Milliseconds, token); break;
                        case ClickAction click: DispatchClick(hwnd, click, false, playbackMode); break;
                        case RightClickAction click: DispatchRightClick(hwnd, click); break;
                        case DoubleClickAction click: DispatchClick(hwnd, click, true, playbackMode); break;
                        case TypeTextAction text: PostText(hwnd, text.Text); ReportClassicKeyboard(); break;
                        case KeyPressAction key: PostKey(hwnd, key.KeyName); ReportClassicKeyboard(); break;
                    }
                }
            }
            StatusChanged?.Invoke(this, L($"Workflow completed in background - {repeatCount} run(s)", $"Đã hoàn tất quy trình trong nền - {repeatCount} lần chạy"));
        }
        catch (OperationCanceledException) { StatusChanged?.Invoke(this, "Workflow stopped"); }
        finally
        {
            CurrentActionChanged?.Invoke(this, null);
            IsRunning = false;
            _pauseGate.Set();
            _runCts?.Dispose();
            _runCts = null;
        }
    }

    private void DispatchClick(IntPtr root, PointerAction action, bool twice, string playbackMode)
    {
        var screen = new POINT(action.ClientX, action.ClientY);
        if (!NativeMethods.ClientToScreen(root, ref screen)) throw new InvalidOperationException("Could not convert the client click point.");
        if (playbackMode != PlaybackModes.Win32Messages && TryInvokeAutomationElement(root, screen, twice ? 2 : 1))
        {
            StatusChanged?.Invoke(this, L("Modern control invoked in background - physical pointer unchanged", "Đã gọi điều khiển hiện đại trong nền - con trỏ thật không thay đổi"));
            return;
        }

        if (playbackMode == PlaybackModes.UiAutomation)
        {
            throw new InvalidOperationException(L(
                $"No actionable UI Automation control was found at client point {action.ClientX}, {action.ClientY}. Choose Automatic or Classic Win32 messages for fallback.",
                $"Không tìm thấy điều khiển UI Automation có thể thao tác tại điểm {action.ClientX}, {action.ClientY}. Hãy chọn Tự động hoặc Thông điệp Win32 cổ điển để dùng phương án dự phòng."));
        }

        PostClick(root, action, false, twice);
        StatusChanged?.Invoke(this, playbackMode == PlaybackModes.Automatic
            ? L("No modern control found - Classic Win32 message sent", "Không tìm thấy điều khiển hiện đại - đã gửi thông điệp Win32 cổ điển")
            : L("Classic Win32 click message sent in background", "Đã gửi thông điệp nhấp Win32 cổ điển trong nền"));
    }

    private void DispatchRightClick(IntPtr root, RightClickAction action)
    {
        PostClick(root, action, true, false);
        StatusChanged?.Invoke(this, L("Right-click sent through Classic Win32 messages", "Đã gửi nhấp phải bằng thông điệp Win32 cổ điển"));
    }

    private void ReportClassicKeyboard() => StatusChanged?.Invoke(this,
        L("Keyboard action sent through Classic Win32 messages", "Đã gửi thao tác bàn phím bằng thông điệp Win32 cổ điển"));

    private static bool TryInvokeAutomationElement(IntPtr root, POINT screenPoint, int invokeCount)
    {
        try
        {
            var rootElement = AutomationElement.FromHandle(root);
            if (rootElement is null) return false;
            var point = new System.Windows.Point(screenPoint.X, screenPoint.Y);
            AutomationElement? best = null;
            var bestArea = double.MaxValue;
            var candidates = rootElement.FindAll(TreeScope.Descendants, Condition.TrueCondition);
            for (var i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                System.Windows.Rect bounds;
                try
                {
                    bounds = candidate.Current.BoundingRectangle;
                    if (bounds.IsEmpty || !bounds.Contains(point) || !candidate.Current.IsEnabled) continue;
                }
                catch (ElementNotAvailableException) { continue; }

                if (!SupportsBackgroundAction(candidate)) continue;
                var area = Math.Max(1, bounds.Width * bounds.Height);
                if (area < bestArea) { best = candidate; bestArea = area; }
            }
            if (best is null) return false;
            for (var i = 0; i < invokeCount; i++)
            {
                if (!InvokeBackgroundAction(best)) return false;
                if (i + 1 < invokeCount) Thread.Sleep(Math.Min(100, (int)NativeMethods.GetDoubleClickTime() / 3));
            }
            return true;
        }
        catch (Exception ex) when (ex is ElementNotAvailableException or InvalidOperationException or UnauthorizedAccessException or COMException)
        {
            return false;
        }
    }

    private static bool SupportsBackgroundAction(AutomationElement element) =>
        element.TryGetCurrentPattern(InvokePattern.Pattern, out _) ||
        element.TryGetCurrentPattern(TogglePattern.Pattern, out _) ||
        element.TryGetCurrentPattern(SelectionItemPattern.Pattern, out _) ||
        element.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out _);

    private static bool InvokeBackgroundAction(AutomationElement element)
    {
        if (element.TryGetCurrentPattern(InvokePattern.Pattern, out var invoke)) { ((InvokePattern)invoke).Invoke(); return true; }
        if (element.TryGetCurrentPattern(TogglePattern.Pattern, out var toggle)) { ((TogglePattern)toggle).Toggle(); return true; }
        if (element.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var selection)) { ((SelectionItemPattern)selection).Select(); return true; }
        if (element.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out var expand))
        {
            var pattern = (ExpandCollapsePattern)expand;
            if (pattern.Current.ExpandCollapseState == ExpandCollapseState.Collapsed) pattern.Expand(); else pattern.Collapse();
            return true;
        }
        return false;
    }

    public void Pause() { if (IsRunning) { _pauseGate.Reset(); StatusChanged?.Invoke(this, "Paused"); } }
    public void Resume() { if (IsRunning) { _pauseGate.Set(); StatusChanged?.Invoke(this, "Resuming..."); } }
    public void Stop() { _runCts?.Cancel(); _pauseGate.Set(); }

    private async Task WaitWhilePausedAsync(CancellationToken token)
    {
        while (!_pauseGate.IsSet) await Task.Delay(40, token);
    }

    private async Task DelayWithPauseAsync(int milliseconds, CancellationToken token)
    {
        var remaining = milliseconds;
        while (remaining > 0)
        {
            await WaitWhilePausedAsync(token);
            var slice = Math.Min(remaining, 40);
            await Task.Delay(slice, token);
            remaining -= slice;
        }
    }

    private static void PostClick(IntPtr root, PointerAction action, bool right, bool twice)
    {
        var screen = new POINT(action.ClientX, action.ClientY);
        if (!NativeMethods.ClientToScreen(root, ref screen)) throw new InvalidOperationException("Could not convert the client click point.");
        var recipient = NativeMethods.WindowFromPoint(screen);
        if (recipient == IntPtr.Zero || NativeMethods.GetAncestor(recipient, NativeMethods.GaRoot) != root) recipient = root;
        var client = screen;
        if (!NativeMethods.ScreenToClient(recipient, ref client)) throw new InvalidOperationException("Could not address the target control.");
        var position = PackPoint(client.X, client.Y);
        var down = right ? NativeMethods.WmRButtonDown : NativeMethods.WmLButtonDown;
        var up = right ? NativeMethods.WmRButtonUp : NativeMethods.WmLButtonUp;
        var state = right ? NativeMethods.MkRButton : NativeMethods.MkLButton;
        Post(recipient, down, new IntPtr(state), position);
        Post(recipient, up, IntPtr.Zero, position);
        if (twice)
        {
            Post(recipient, NativeMethods.WmLButtonDblClk, new IntPtr(NativeMethods.MkLButton), position);
            Post(recipient, NativeMethods.WmLButtonUp, IntPtr.Zero, position);
        }
    }

    private static void PostText(IntPtr root, string text)
    {
        var recipient = GetKeyboardRecipient(root);
        foreach (var ch in text) Post(recipient, NativeMethods.WmChar, new IntPtr(ch), IntPtr.Zero);
    }

    private static void PostKey(IntPtr root, string keyName)
    {
        var recipient = GetKeyboardRecipient(root);
        var keys = keyName.ToUpperInvariant().Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(ToVirtualKey).ToArray();
        foreach (var key in keys) PostKeyMessage(recipient, key, false);
        foreach (var key in keys.Reverse()) PostKeyMessage(recipient, key, true);
    }

    private static IntPtr GetKeyboardRecipient(IntPtr root)
    {
        var threadId = NativeMethods.GetWindowThreadProcessId(root, out _);
        var info = new GUITHREADINFO { Size = (uint)Marshal.SizeOf<GUITHREADINFO>() };
        return NativeMethods.GetGUIThreadInfo(threadId, ref info) && info.Focus != IntPtr.Zero ? info.Focus : root;
    }

    private static void PostKeyMessage(IntPtr hwnd, ushort key, bool up)
    {
        var scan = NativeMethods.MapVirtualKey(key, NativeMethods.MapvkVkToVsc);
        var lParam = 1L | ((long)scan << 16);
        if (up) lParam |= 1L << 30 | 1L << 31;
        Post(hwnd, up ? NativeMethods.WmKeyUp : NativeMethods.WmKeyDown, new IntPtr(key), new IntPtr(lParam));
    }

    internal static ushort ToVirtualKey(string key) => key switch
    {
        "ENTER" => 0x0D, "TAB" => 0x09, "ESCAPE" => 0x1B, "BACKSPACE" => 0x08, "DELETE" => 0x2E,
        "UP" => 0x26, "DOWN" => 0x28, "LEFT" => 0x25, "RIGHT" => 0x27, "CTRL" => 0x11, "SHIFT" => 0x10, "ALT" => 0x12,
        "HOME" => 0x24, "END" => 0x23, "PAGEUP" => 0x21, "PAGEDOWN" => 0x22, "SPACE" => 0x20,
        _ when key.Length > 1 && key[0] == 'F' && int.TryParse(key[1..], out var f) && f is >= 1 and <= 24 => (ushort)(0x6F + f),
        _ when key.Length == 1 && char.IsLetterOrDigit(key[0]) => char.ToUpperInvariant(key[0]),
        _ => throw new InvalidOperationException($"Unsupported key {key}.")
    };

    private static IntPtr PackPoint(int x, int y) => new((y & 0xFFFF) << 16 | (x & 0xFFFF));
    private static void Post(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam)
    {
        if (!NativeMethods.PostMessage(hwnd, message, wParam, lParam)) throw new InvalidOperationException("The target window rejected a background input message.");
    }

    private static string L(string english, string vietnamese) => LocalizationService.Language == "vi" ? vietnamese : english;

    public void Dispose() { Stop(); _pauseGate.Dispose(); }
}
