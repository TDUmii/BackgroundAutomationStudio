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
    [InlineData(PlaybackModes.Automatic, PlaybackModes.Automatic)]
    [InlineData(PlaybackModes.UiAutomation, PlaybackModes.UiAutomation)]
    [InlineData(PlaybackModes.Win32Messages, PlaybackModes.Win32Messages)]
    public void PlaybackMode_NormalizesPersistedSettings(string? value, string expected) =>
        Assert.Equal(expected, PlaybackModes.Normalize(value));
}
