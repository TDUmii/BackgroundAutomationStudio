using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace BackgroundAutomationStudio.MiniCore;

public static class MiniForegroundInput
{
    private static readonly int InputSize = Marshal.SizeOf<MiniNative.INPUT>();

    public static bool IsTargetFocused(nint target) =>
        target != nint.Zero && MiniNative.GetAncestor(MiniNative.GetForegroundWindow(), 2) == target;

    public static async Task ActivateAsync(nint target, CancellationToken token)
    {
        if (!MiniNative.IsWindow(target)) throw new InvalidOperationException("The selected target window is no longer open.");
        if (MiniNative.IsIconic(target)) MiniNative.ShowWindow(target, 9);
        MiniNative.SetForegroundWindow(target);
        await Task.Delay(160, token);
        if (!IsTargetFocused(target))
            throw new InvalidOperationException("Windows could not focus the target. Focus it once, then use the global shortcut to start.");
    }

    public static async Task WaitForFocusAsync(nint target, Action<bool>? focusStateChanged, CancellationToken token)
    {
        var reportedPause = false;
        while (!IsTargetFocused(target))
        {
            if (!MiniNative.IsWindow(target)) throw new InvalidOperationException("The selected target window is no longer open.");
            if (!reportedPause) { focusStateChanged?.Invoke(false); reportedPause = true; }
            await Task.Delay(80, token);
        }
        if (reportedPause) focusStateChanged?.Invoke(true);
    }

    public static async Task DelayWithFocusAsync(nint target, int milliseconds, Action<bool>? focusStateChanged, CancellationToken token)
    {
        var remaining = Math.Max(0, milliseconds);
        while (remaining > 0)
        {
            await WaitForFocusAsync(target, focusStateChanged, token);
            var slice = Math.Min(remaining, 10);
            await Task.Delay(slice, token);
            if (IsTargetFocused(target)) remaining -= slice;
        }
    }

    public static async Task<bool> DelayWhileReadyAsync(int milliseconds, Func<bool> isReady, CancellationToken token)
    {
        var remaining = Math.Max(1, milliseconds);
        while (remaining > 0)
        {
            if (!isReady()) return false;
            var slice = Math.Min(remaining, 10);
            await Task.Delay(slice, token);
            remaining -= slice;
        }
        return isReady();
    }

    public static async Task SendStepAsync(nint target, RecordedMiniStep step, int pressMilliseconds, Action<bool>? focusStateChanged, CancellationToken token)
    {
        await WaitForFocusAsync(target, focusStateChanged, token);
        switch (step.Type)
        {
            case "Click":
                await SendClickAsync(target, step.X, step.Y, false, pressMilliseconds, focusStateChanged, token);
                break;
            case "RightClick":
                await SendClickAsync(target, step.X, step.Y, true, pressMilliseconds, focusStateChanged, token);
                break;
            case "Scroll":
                MoveToClientPoint(target, step.X, step.Y);
                SendChecked(new MiniNative.INPUT { Type = 0, Data = new MiniNative.INPUTUNION { Mouse = new MiniNative.MOUSEINPUT { MouseData = unchecked((uint)step.Value), Flags = 0x0800 } } });
                break;
            case "Key":
                await PressKeyAsync(step.Key, pressMilliseconds, focusStateChanged, target, token);
                break;
        }
    }

    public static async Task SendClickAsync(nint target, int x, int y, bool rightButton, int pressMilliseconds, Action<bool>? focusStateChanged, CancellationToken token)
    {
        if (!MiniWindowService.TryPackClientPoint(target, x, y, out _)) throw new ArgumentOutOfRangeException(nameof(x), "The click point must be inside the target's current client area.");
        var settle = Math.Clamp(pressMilliseconds / 2, 8, 40);
        while (true)
        {
            await WaitForFocusAsync(target, focusStateChanged, token);
            MoveToClientPoint(target, x, y);
            if (await DelayWhileReadyAsync(settle, () => IsTargetFocused(target), token)) break;
            await WaitForFocusAsync(target, focusStateChanged, token);
        }
        SendMouse(rightButton ? 0x0008u : 0x0002u);
        var completed = false;
        try { completed = await DelayWhileReadyAsync(Math.Clamp(pressMilliseconds, 10, 1000), () => IsTargetFocused(target), token); }
        finally { SendMouse(rightButton ? 0x0010u : 0x0004u); }
        if (!completed) await WaitForFocusAsync(target, focusStateChanged, token);
    }

    private static async Task PressKeyAsync(string keyName, int pressMilliseconds, Action<bool>? focusStateChanged, nint target, CancellationToken token)
    {
        var virtualKeys = ResolveVirtualKeys(keyName);
        await WaitForFocusAsync(target, focusStateChanged, token);
        foreach (var virtualKey in virtualKeys) SendKeyboard(virtualKey, false);
        var completed = false;
        try { completed = await DelayWhileReadyAsync(Math.Clamp(pressMilliseconds, 10, 1000), () => IsTargetFocused(target), token); }
        finally { for (var index = virtualKeys.Count - 1; index >= 0; index--) SendKeyboard(virtualKeys[index], true); }
        if (!completed) await WaitForFocusAsync(target, focusStateChanged, token);
    }

    public static IReadOnlyList<ushort> ResolveVirtualKeys(string keyName)
    {
        var virtualKeys = keyName.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => Enum.TryParse<Key>(part, true, out var key) ? (ushort)KeyInterop.VirtualKeyFromKey(key) : (ushort)0)
            .ToArray();
        if (virtualKeys.Length == 0 || virtualKeys.Any(key => key == 0)) throw new InvalidOperationException($"Unsupported recorded key: {keyName}");
        return virtualKeys;
    }

    private static void MoveToClientPoint(nint target, int clientX, int clientY)
    {
        var point = new MiniNative.POINT(clientX, clientY);
        if (!MiniNative.ClientToScreen(target, ref point)) throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not resolve the current target point.");
        var left = MiniNative.GetSystemMetrics(76);
        var top = MiniNative.GetSystemMetrics(77);
        var width = Math.Max(2, MiniNative.GetSystemMetrics(78));
        var height = Math.Max(2, MiniNative.GetSystemMetrics(79));
        var absoluteX = (int)Math.Round((point.X - left) * 65535d / (width - 1));
        var absoluteY = (int)Math.Round((point.Y - top) * 65535d / (height - 1));
        SendChecked(new MiniNative.INPUT { Type = 0, Data = new MiniNative.INPUTUNION { Mouse = new MiniNative.MOUSEINPUT { X = absoluteX, Y = absoluteY, Flags = 0x0001 | 0x8000 | 0x4000 } } });
    }

    private static void SendMouse(uint flags) => SendChecked(new MiniNative.INPUT { Type = 0, Data = new MiniNative.INPUTUNION { Mouse = new MiniNative.MOUSEINPUT { Flags = flags } } });
    private static void SendKeyboard(ushort key, bool up) => SendChecked(new MiniNative.INPUT { Type = 1, Data = new MiniNative.INPUTUNION { Keyboard = new MiniNative.KEYBDINPUT { VirtualKey = key, Flags = up ? 0x0002u : 0 } } });

    private static void SendChecked(params MiniNative.INPUT[] inputs)
    {
        if (MiniNative.SendInput((uint)inputs.Length, inputs, InputSize) != inputs.Length)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Windows rejected physical input. Match the target's administrator level and try again.");
    }
}
