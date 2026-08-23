using BackgroundAutomationStudio.Models;
using BackgroundAutomationStudio.Services;

namespace BackgroundAutomationStudio.Tests;

public sealed class ActionModelTests
{
    [Fact]
    public void Clone_CreatesNewIdentityAndPreservesEditableValues()
    {
        var original = new ClickAction { ClientX = 420, ClientY = 220, DelayBefore = 300, Enabled = false };
        var clone = Assert.IsType<ClickAction>(original.Clone());
        Assert.NotEqual(original.Id, clone.Id);
        Assert.Equal((420, 220, 300, false), (clone.ClientX, clone.ClientY, clone.DelayBefore, clone.Enabled));
    }

    [Fact]
    public void GameActions_CloneDurationsAndCoordinatesWithoutSharingIdentity()
    {
        var hold = new KeyHoldAction { KeyName = "SHIFT+E", Milliseconds = 2750, DelayBefore = 40 };
        var holdClone = Assert.IsType<KeyHoldAction>(hold.Clone());
        Assert.NotEqual(hold.Id, holdClone.Id);
        Assert.Equal(("SHIFT+E", 2750, 40), (holdClone.KeyName, holdClone.Milliseconds, holdClone.DelayBefore));

        var drag = new DragAction { StartX = 10, StartY = 20, EndX = 300, EndY = 400, Milliseconds = 900 };
        var dragClone = Assert.IsType<DragAction>(drag.Clone());
        Assert.NotEqual(drag.Id, dragClone.Id);
        Assert.Equal((10, 20, 300, 400, 900), (dragClone.StartX, dragClone.StartY, dragClone.EndX, dragClone.EndY, dragClone.Milliseconds));
    }

    [Theory]
    [InlineData("ENTER")][InlineData("F12")][InlineData("CTRL+C")][InlineData("CTRL+SHIFT+S")]
    public void KeyNames_AcceptsSupportedKeys(string key) => Assert.True(KeyNames.IsSupported(key));

    [Theory]
    [InlineData("F13")][InlineData("LAUNCH")][InlineData("")]
    public void KeyNames_RejectsUnsupportedKeys(string key) => Assert.False(KeyNames.IsSupported(key));

    [Theory]
    [InlineData("CTRL+SHIFT+F9")][InlineData("ALT+A")][InlineData("WIN+1")]
    public void GlobalHotkey_AcceptsModifierAndSupportedKey(string hotkey) => Assert.True(HotkeyParser.TryParse(hotkey, out _, out _));

    [Theory]
    [InlineData("F9")][InlineData("CTRL")][InlineData("CTRL+LAUNCH")]
    public void GlobalHotkey_RejectsIncompleteOrUnsupportedShortcut(string hotkey) => Assert.False(HotkeyParser.TryParse(hotkey, out _, out _));

    [Theory]
    [InlineData(null, PlaybackModes.Automatic)]
    [InlineData("", PlaybackModes.Automatic)]
    [InlineData("Unexpected", PlaybackModes.Automatic)]
    [InlineData("UiAutomation", PlaybackModes.Automatic)]
    [InlineData(PlaybackModes.Automatic, PlaybackModes.Automatic)]
    [InlineData(PlaybackModes.UiAutomation, PlaybackModes.UiAutomation)]
    [InlineData(PlaybackModes.Win32Messages, PlaybackModes.Win32Messages)]
    [InlineData(PlaybackModes.GameForeground, PlaybackModes.GameForeground)]
    [InlineData(PlaybackModes.GameBackground, PlaybackModes.GameBackground)]
    public void PlaybackMode_NormalizesPersistedSettings(string? value, string expected) =>
        Assert.Equal(expected, PlaybackModes.Normalize(value));

    [Fact]
    public void GameInputChord_ResolvesModifiersInPressedOrder()
    {
        Assert.Equal(new ushort[] { 0x11, 0x10, (ushort)'E' }, GameInputDispatcher.ResolveChord("CTRL+SHIFT+E"));
    }

    [Theory]
    [InlineData(null, RepeatModes.Count)]
    [InlineData("", RepeatModes.Count)]
    [InlineData("Unexpected", RepeatModes.Count)]
    [InlineData(RepeatModes.Count, RepeatModes.Count)]
    [InlineData(RepeatModes.Infinite, RepeatModes.Infinite)]
    [InlineData(RepeatModes.Duration, RepeatModes.Duration)]
    [InlineData(RepeatModes.UntilTime, RepeatModes.UntilTime)]
    public void RepeatMode_NormalizesPersistedProjects(string? value, string expected) =>
        Assert.Equal(expected, RepeatModes.Normalize(value));

    [Fact]
    public void GetNextStopAt_UsesTodayOrTomorrowWithoutDroppingOffset()
    {
        var now = new DateTimeOffset(2026, 8, 23, 20, 0, 0, TimeSpan.FromHours(7));
        Assert.Equal(new DateTimeOffset(2026, 8, 23, 21, 30, 0, now.Offset), PlaybackRunOptions.GetNextStopAt(new TimeSpan(21, 30, 0), now));
        Assert.Equal(new DateTimeOffset(2026, 8, 24, 8, 0, 0, now.Offset), PlaybackRunOptions.GetNextStopAt(new TimeSpan(8, 0, 0), now));
    }

    [Fact]
    public void CountSchedule_AllowsHundredsOfThousandsOfGameIterations()
    {
        Assert.Equal(250_000, PlaybackRunOptions.Count(250_000).RepeatCount);
        Assert.Equal(1_000_000, PlaybackRunOptions.Count(int.MaxValue).RepeatCount);
    }
}
