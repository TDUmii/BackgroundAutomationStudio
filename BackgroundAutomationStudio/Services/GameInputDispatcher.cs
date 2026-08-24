using System.ComponentModel;
using System.Runtime.InteropServices;
using BackgroundAutomationStudio.Models;
using BackgroundAutomationStudio.Native;

namespace BackgroundAutomationStudio.Services;

/// <summary>
/// Foreground-only game input. It deliberately uses the normal Windows input stream,
/// never activates a window, and releases held input whenever playback is paused or
/// the selected target loses foreground focus.
/// </summary>
public static class GameInputDispatcher
{
    private static readonly int InputSize = Marshal.SizeOf<INPUT>();

    public static async Task DispatchAsync(
        IntPtr target,
        AutomationAction action,
        int pressDurationMilliseconds,
        Func<bool> isReady,
        Func<CancellationToken, Task> waitUntilReady,
        CancellationToken token)
    {
        var pressDuration = NormalizePressDuration(pressDurationMilliseconds);
        await waitUntilReady(token);
        switch (action)
        {
            case ClickAction click:
                MoveToClientPoint(target, click.ClientX, click.ClientY);
                await SettlePointerAsync(pressDuration, token);
                await PressMouseAsync(NativeMethods.MouseeventfLeftdown, NativeMethods.MouseeventfLeftup, pressDuration, isReady, waitUntilReady, token);
                break;
            case RightClickAction click:
                MoveToClientPoint(target, click.ClientX, click.ClientY);
                await SettlePointerAsync(pressDuration, token);
                await PressMouseAsync(NativeMethods.MouseeventfRightdown, NativeMethods.MouseeventfRightup, pressDuration, isReady, waitUntilReady, token);
                break;
            case DoubleClickAction click:
                var doubleClickTime = Math.Max(1, (int)NativeMethods.GetDoubleClickTime());
                var doubleClickPressDuration = GetDoubleClickPressDuration(pressDuration, doubleClickTime);
                MoveToClientPoint(target, click.ClientX, click.ClientY);
                await SettlePointerAsync(doubleClickPressDuration, token);
                if (!await PressMouseAsync(NativeMethods.MouseeventfLeftdown, NativeMethods.MouseeventfLeftup, doubleClickPressDuration, isReady, waitUntilReady, token)) break;
                await Task.Delay(GetDoubleClickGapDuration(doubleClickTime), token);
                await waitUntilReady(token);
                await PressMouseAsync(NativeMethods.MouseeventfLeftdown, NativeMethods.MouseeventfLeftup, doubleClickPressDuration, isReady, waitUntilReady, token);
                break;
            case TypeTextAction text:
                foreach (var character in text.Text)
                {
                    await waitUntilReady(token);
                    SendUnicode(character);
                }
                break;
            case KeyPressAction key:
                await PressChordAsync(key.KeyName, pressDuration, isReady, waitUntilReady, token);
                break;
            case KeyHoldAction hold:
                await HoldChordAsync(hold.KeyName, hold.Milliseconds, isReady, waitUntilReady, token);
                break;
            case DragAction drag:
                await DragAsync(target, drag, isReady, waitUntilReady, token);
                break;
            case ScrollAction scroll:
                MoveToClientPoint(target, scroll.ClientX, scroll.ClientY);
                SendChecked(new INPUT { Type = NativeMethods.InputMouse, Data = new INPUTUNION { Mouse = new MOUSEINPUT { MouseData = unchecked((uint)scroll.Delta), Flags = NativeMethods.MouseeventfWheel } } });
                break;
            case MovePointerAction move:
                MoveToClientPoint(target, move.ClientX, move.ClientY);
                break;
        }
    }

    internal static int NormalizePressDuration(int value) => AppSettings.NormalizeGamePressDuration(value);

    internal static int GetPointerSettleDuration(int pressDurationMilliseconds) =>
        Math.Clamp(NormalizePressDuration(pressDurationMilliseconds) / 2, 8, 40);

    internal static int GetDoubleClickPressDuration(int pressDurationMilliseconds, int systemDoubleClickTimeMilliseconds) =>
        Math.Min(NormalizePressDuration(pressDurationMilliseconds), Math.Clamp(Math.Max(1, systemDoubleClickTimeMilliseconds) / 3, 10, 250));

    internal static int GetDoubleClickGapDuration(int systemDoubleClickTimeMilliseconds) =>
        Math.Clamp(Math.Max(1, systemDoubleClickTimeMilliseconds) / 5, 25, 90);

    private static Task SettlePointerAsync(int pressDurationMilliseconds, CancellationToken token) =>
        Task.Delay(GetPointerSettleDuration(pressDurationMilliseconds), token);

    private static async Task<bool> PressMouseAsync(uint down, uint up, int pressDurationMilliseconds, Func<bool> isReady, Func<CancellationToken, Task> waitUntilReady, CancellationToken token)
    {
        await waitUntilReady(token);
        SendMouseFlags(down);
        var completed = false;
        try
        {
            completed = await DelayWhileReadyAsync(pressDurationMilliseconds, isReady, token);
        }
        finally
        {
            SendMouseFlags(up);
        }
        if (!completed) await waitUntilReady(token);
        return completed;
    }

    internal static async Task<bool> DelayWhileReadyAsync(int milliseconds, Func<bool> isReady, CancellationToken token)
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

    public static IReadOnlyList<ushort> ResolveChord(string keyName)
    {
        var keys = new List<ushort>();
        foreach (var part in keyName.ToUpperInvariant().Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            keys.Add(BackgroundAutomationRunner.ToVirtualKey(part));
        if (keys.Count == 0) throw new InvalidOperationException("A key name is required.");
        return keys;
    }

    private static async Task HoldChordAsync(string keyName, int milliseconds, Func<bool> isReady, Func<CancellationToken, Task> waitUntilReady, CancellationToken token)
    {
        var keys = ResolveChord(keyName);
        var remaining = Math.Max(1, milliseconds);
        var held = false;
        try
        {
            while (remaining > 0)
            {
                if (!isReady())
                {
                    if (held) { SendKeysUp(keys); held = false; }
                    await waitUntilReady(token);
                }
                if (!held) { SendKeysDown(keys); held = true; }
                var slice = Math.Min(remaining, 25);
                await Task.Delay(slice, token);
                remaining -= slice;
            }
        }
        finally
        {
            if (held) SendKeysUp(keys);
        }
    }

    private static async Task DragAsync(IntPtr target, DragAction drag, Func<bool> isReady, Func<CancellationToken, Task> waitUntilReady, CancellationToken token)
    {
        var duration = Math.Max(1, drag.Milliseconds);
        var steps = Math.Clamp(duration / 16, 2, 240);
        var held = false;
        try
        {
            MoveToClientPoint(target, drag.StartX, drag.StartY);
            SendMouseFlags(NativeMethods.MouseeventfLeftdown);
            held = true;
            for (var step = 1; step <= steps; step++)
            {
                if (!isReady())
                {
                    if (held) { SendMouseFlags(NativeMethods.MouseeventfLeftup); held = false; }
                    await waitUntilReady(token);
                }
                var progress = step / (double)steps;
                var x = (int)Math.Round(drag.StartX + (drag.EndX - drag.StartX) * progress);
                var y = (int)Math.Round(drag.StartY + (drag.EndY - drag.StartY) * progress);
                MoveToClientPoint(target, x, y);
                if (!held) { SendMouseFlags(NativeMethods.MouseeventfLeftdown); held = true; }
                await Task.Delay(Math.Max(1, duration / steps), token);
            }
        }
        finally
        {
            if (held) SendMouseFlags(NativeMethods.MouseeventfLeftup);
        }
    }

    private static async Task PressChordAsync(string keyName, int pressDurationMilliseconds, Func<bool> isReady, Func<CancellationToken, Task> waitUntilReady, CancellationToken token)
    {
        var keys = ResolveChord(keyName);
        await waitUntilReady(token);
        SendKeysDown(keys);
        var completed = false;
        try
        {
            completed = await DelayWhileReadyAsync(pressDurationMilliseconds, isReady, token);
        }
        finally
        {
            SendKeysUp(keys);
        }
        if (!completed) await waitUntilReady(token);
    }

    private static void SendKeysDown(IReadOnlyList<ushort> keys)
    {
        foreach (var key in keys) SendKeyboard(key, false);
    }

    private static void SendKeysUp(IReadOnlyList<ushort> keys)
    {
        for (var index = keys.Count - 1; index >= 0; index--) SendKeyboard(keys[index], true);
    }

    private static void SendKeyboard(ushort key, bool keyUp) => SendChecked(KeyboardInput(key, keyUp));

    private static INPUT KeyboardInput(ushort key, bool keyUp) => new()
    {
        Type = NativeMethods.InputKeyboard,
        Data = new INPUTUNION { Keyboard = new KEYBDINPUT { VirtualKey = key, Flags = keyUp ? NativeMethods.KeyeventfKeyup : 0 } }
    };

    private static void SendUnicode(char character)
    {
        SendChecked(
            new INPUT { Type = NativeMethods.InputKeyboard, Data = new INPUTUNION { Keyboard = new KEYBDINPUT { ScanCode = character, Flags = NativeMethods.KeyeventfUnicode } } },
            new INPUT { Type = NativeMethods.InputKeyboard, Data = new INPUTUNION { Keyboard = new KEYBDINPUT { ScanCode = character, Flags = NativeMethods.KeyeventfUnicode | NativeMethods.KeyeventfKeyup } } });
    }

    private static void MoveToClientPoint(IntPtr target, int clientX, int clientY)
    {
        var point = new POINT(clientX, clientY);
        if (!NativeMethods.ClientToScreen(target, ref point)) throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not resolve the current game-window point.");
        var left = NativeMethods.GetSystemMetrics(NativeMethods.SmXVirtualScreen);
        var top = NativeMethods.GetSystemMetrics(NativeMethods.SmYVirtualScreen);
        var width = Math.Max(2, NativeMethods.GetSystemMetrics(NativeMethods.SmCxVirtualScreen));
        var height = Math.Max(2, NativeMethods.GetSystemMetrics(NativeMethods.SmCyVirtualScreen));
        var absoluteX = (int)Math.Round((point.X - left) * 65535d / (width - 1));
        var absoluteY = (int)Math.Round((point.Y - top) * 65535d / (height - 1));
        SendChecked(new INPUT
        {
            Type = NativeMethods.InputMouse,
            Data = new INPUTUNION { Mouse = new MOUSEINPUT { X = absoluteX, Y = absoluteY, Flags = NativeMethods.MouseeventfMove | NativeMethods.MouseeventfAbsolute | NativeMethods.MouseeventfVirtualdesk } }
        });
    }

    private static void SendMouseFlags(uint flags) => SendChecked(MouseInput(flags));

    private static INPUT MouseInput(uint flags) => new()
    {
        Type = NativeMethods.InputMouse,
        Data = new INPUTUNION { Mouse = new MOUSEINPUT { Flags = flags } }
    };

    private static void SendChecked(params INPUT[] inputs)
    {
        if (NativeMethods.SendInput((uint)inputs.Length, inputs, InputSize) != inputs.Length)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Windows rejected foreground game input. Match the target's administrator level and try again.");
    }
}
