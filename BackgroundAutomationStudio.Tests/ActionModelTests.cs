using BackgroundAutomationStudio.Models;
using BackgroundAutomationStudio.Services;

namespace BackgroundAutomationStudio.Tests;

public sealed class ActionModelTests
{
    [Fact]
    public void ScrollClone_PreservesCoordinatesDeltaAndCommonMetadata()
    {
        var original = new ScrollAction { ClientX = 25, ClientY = 80, Delta = 240, Note = "Zoom out", DelayBefore = 75, Enabled = false };
        var clone = Assert.IsType<ScrollAction>(original.Clone());
        Assert.Equal((25, 80, 240, "Zoom out", 75, false), (clone.ClientX, clone.ClientY, clone.Delta, clone.Note, clone.DelayBefore, clone.Enabled));
        Assert.NotEqual(original.Id, clone.Id);
    }
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

    [Theory]
    [InlineData(PlaybackModes.Automatic, 1, "AutomaticModeName")]
    [InlineData(PlaybackModes.GameForeground, 2, "GameForegroundModeName")]
    [InlineData(PlaybackModes.GameBackground, 3, "GameBackgroundModeName")]
    [InlineData(PlaybackModes.UiAutomation, 4, "UiAutomationModeName")]
    [InlineData(PlaybackModes.Win32Messages, 5, "Win32ModeName")]
    public void PlaybackMode_MapsToStableVisibleEngineIdentity(string mode, int index, string resourceKey)
    {
        Assert.Equal(index, PlaybackModes.GetIndex(mode));
        Assert.Equal(resourceKey, PlaybackModes.GetResourceKey(mode));
    }

    [Theory]
    [InlineData("en")]
    [InlineData("vi")]
    public void PlaybackMode_LabelsUseOnlySingleAsciiHyphenSeparators(string language)
    {
        LocalizationService.Apply(language);
        try
        {
            foreach (var key in new[] { "AutomaticMode", "GameForegroundMode", "GameBackgroundMode", "UiAutomationMode", "Win32Mode" })
            {
                var label = LocalizationService.Get(key);
                Assert.Contains(" - ", label);
                Assert.DoesNotContain("--", label);
                Assert.DoesNotMatch("[‐‑‒–—−]", label);
            }
        }
        finally
        {
            LocalizationService.Apply("en");
        }
    }

    [Fact]
    public void GameInputChord_ResolvesModifiersInPressedOrder()
    {
        Assert.Equal(new ushort[] { 0x11, 0x10, (ushort)'E' }, GameInputDispatcher.ResolveChord("CTRL+SHIFT+E"));
    }

    [Theory]
    [InlineData(10, 8)]
    [InlineData(45, 22)]
    [InlineData(100, 40)]
    [InlineData(1000, 40)]
    public void GameInputTiming_AllowsPointerMoveToSettleBeforePress(int pressDuration, int expectedSettleDuration) =>
        Assert.Equal(expectedSettleDuration, GameInputDispatcher.GetPointerSettleDuration(pressDuration));

    [Theory]
    [InlineData(45, 500, 45)]
    [InlineData(1000, 500, 166)]
    [InlineData(1000, 300, 100)]
    public void GameInputTiming_CapsDoubleClickHoldBelowSystemCadence(int configuredDuration, int systemDoubleClickTime, int expectedPressDuration)
    {
        var pressDuration = GameInputDispatcher.GetDoubleClickPressDuration(configuredDuration, systemDoubleClickTime);
        var gapDuration = GameInputDispatcher.GetDoubleClickGapDuration(systemDoubleClickTime);
        Assert.Equal(expectedPressDuration, pressDuration);
        Assert.True(pressDuration + gapDuration < systemDoubleClickTime);
    }

    [Fact]
    public async Task GameInputTiming_StopsHeldPressImmediatelyWhenTargetLosesFocus() =>
        Assert.False(await GameInputDispatcher.DelayWhileReadyAsync(1000, () => false, CancellationToken.None));

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

    [Fact]
    public void ImageActionClone_DeepCopiesEmbeddedTemplateAndScanSettings()
    {
        var original = new ClickImageAction { TemplateName = "target.png", TemplatePng = [1, 2, 3], SimilarityPercent = 92, TimeoutMilliseconds = 8000, PollIntervalMilliseconds = 175, RegionX = 4, RegionY = 5, RegionWidth = 600, RegionHeight = 400, OffsetX = -2, OffsetY = 7, RightClick = true };
        var clone = Assert.IsType<ClickImageAction>(original.Clone());
        Assert.Equal(("target.png", 92, 8000, 175, 4, 5, 600, 400, -2, 7, true), (clone.TemplateName, clone.SimilarityPercent, clone.TimeoutMilliseconds, clone.PollIntervalMilliseconds, clone.RegionX, clone.RegionY, clone.RegionWidth, clone.RegionHeight, clone.OffsetX, clone.OffsetY, clone.RightClick));
        Assert.Equal(original.TemplatePng, clone.TemplatePng);
        Assert.NotSame(original.TemplatePng, clone.TemplatePng);
        Assert.NotEqual(original.Id, clone.Id);
    }
}
