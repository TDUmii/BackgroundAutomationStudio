using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using BackgroundAutomationStudio.Native;

namespace BackgroundAutomationStudio.Services;

public sealed class WindowPickerService : IDisposable
{
    private readonly IWindowManager _windowManager;
    private NativeMethods.HookProc? _hookProc;
    private IntPtr _hook;
    private TaskCompletionSource<IntPtr>? _completion;

    public WindowPickerService(IWindowManager windowManager) => _windowManager = windowManager;

    public async Task<Models.WindowTarget?> PickWindowAsync(IntPtr excludedHwnd, CancellationToken cancellationToken = default)
    {
        StopHook();
        _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _hookProc = MouseCallback;
        using var process = Process.GetCurrentProcess();
        using var module = process.MainModule!;
        _hook = NativeMethods.SetWindowsHookEx(NativeMethods.WhMouseLl, _hookProc, NativeMethods.GetModuleHandle(module.ModuleName), 0);
        if (_hook == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to start window picking.");
        using var registration = cancellationToken.Register(() => _completion.TrySetCanceled(cancellationToken));
        try
        {
            while (true)
            {
                var hwnd = await _completion.Task;
                hwnd = NativeMethods.GetAncestor(hwnd, NativeMethods.GaRoot);
                if (hwnd == excludedHwnd) throw new InvalidOperationException("Select another application window, not Background Automation Studio.");
                return _windowManager.GetTarget(hwnd);
            }
        }
        finally { StopHook(); }
    }

    public async Task<POINT> PickClientPointAsync(IntPtr targetHwnd, CancellationToken cancellationToken = default)
    {
        StopHook();
        var pointCompletion = new TaskCompletionSource<POINT>(TaskCreationOptions.RunContinuationsAsynchronously);
        _hookProc = (code, wParam, lParam) =>
        {
            if (code >= 0 && wParam.ToInt32() == NativeMethods.WmLButtonDown)
            {
                var data = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                if (_windowManager.IsPointInsideTarget(targetHwnd, data.Point)) pointCompletion.TrySetResult(data.Point);
            }
            return NativeMethods.CallNextHookEx(_hook, code, wParam, lParam);
        };
        using var process = Process.GetCurrentProcess();
        using var module = process.MainModule!;
        _hook = NativeMethods.SetWindowsHookEx(NativeMethods.WhMouseLl, _hookProc, NativeMethods.GetModuleHandle(module.ModuleName), 0);
        if (_hook == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to start point picking.");
        using var registration = cancellationToken.Register(() => pointCompletion.TrySetCanceled(cancellationToken));
        try
        {
            var screen = await pointCompletion.Task;
            NativeMethods.ScreenToClient(targetHwnd, ref screen);
            return screen;
        }
        finally { StopHook(); }
    }

    private IntPtr MouseCallback(int code, IntPtr wParam, IntPtr lParam)
    {
        if (code >= 0 && wParam.ToInt32() == NativeMethods.WmLButtonDown)
        {
            var data = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
            _completion?.TrySetResult(NativeMethods.WindowFromPoint(data.Point));
        }
        return NativeMethods.CallNextHookEx(_hook, code, wParam, lParam);
    }

    private void StopHook()
    {
        if (_hook != IntPtr.Zero) NativeMethods.UnhookWindowsHookEx(_hook);
        _hook = IntPtr.Zero;
        _hookProc = null;
    }

    public void Dispose() => StopHook();
}
