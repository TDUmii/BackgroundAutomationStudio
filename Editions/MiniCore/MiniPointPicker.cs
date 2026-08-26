using System.Runtime.InteropServices;

namespace BackgroundAutomationStudio.MiniCore;

public sealed class MiniPointPicker : IDisposable
{
    private MiniNative.HookProc? _hookProc;
    private nint _hook;
    private nint _target;
    private TaskCompletionSource<MiniPoint?>? _completion;

    public async Task<MiniPoint?> PickAsync(nint target, CancellationToken token = default)
    {
        if (!MiniNative.IsWindow(target)) throw new InvalidOperationException("The selected target window is no longer open.");
        Dispose(); _target = target;
        _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var completion = _completion;
        _hookProc = Hook;
        _hook = MiniNative.SetWindowsHookEx(14, _hookProc, MiniNative.GetModuleHandle(null), 0);
        if (_hook == nint.Zero) throw new InvalidOperationException("Windows could not start point picking.");
        using var registration = token.Register(() => Complete(null));
        while (!completion.Task.IsCompleted)
        {
            await Task.WhenAny(completion.Task, Task.Delay(250));
            if (!MiniNative.IsWindow(target)) Complete(null);
        }
        return await completion.Task;
    }

    private nint Hook(int code, nint wParam, nint lParam)
    {
        if (code >= 0 && wParam == 0x0201)
        {
            var data = Marshal.PtrToStructure<MiniNative.MSLLHOOKSTRUCT>(lParam);
            var hit = MiniNative.WindowFromPoint(data.Point);
            if (MiniWindowService.IsTargetOrChild(_target, hit)) Complete(MiniWindowService.ScreenToClient(_target, data.Point.X, data.Point.Y));
        }
        return MiniNative.CallNextHookEx(nint.Zero, code, wParam, lParam);
    }

    private void Complete(MiniPoint? point)
    {
        if (_hook != nint.Zero) MiniNative.UnhookWindowsHookEx(_hook);
        _hook = nint.Zero; _hookProc = null;
        _completion?.TrySetResult(point);
    }
    public void Dispose() => Complete(null);
}
