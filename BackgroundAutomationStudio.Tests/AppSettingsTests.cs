using System.Text.Json;
using System.Windows.Media;
using BackgroundAutomationStudio;
using BackgroundAutomationStudio.Models;

namespace BackgroundAutomationStudio.Tests;

public sealed class AppSettingsTests
{
    [Fact]
    public void AlwaysOnTop_DefaultsOff_AndRoundTrips()
    {
        var defaults = new AppSettings();
        Assert.False(defaults.AlwaysOnTop);

        defaults.AlwaysOnTop = true;
        var restored = JsonSerializer.Deserialize<AppSettings>(JsonSerializer.Serialize(defaults));

        Assert.NotNull(restored);
        Assert.True(restored.AlwaysOnTop);
    }

    [Theory]
    [InlineData(-1, 10)]
    [InlineData(45, 45)]
    [InlineData(5000, 1000)]
    public void GamePressDuration_NormalizesToSafeRange(int value, int expected) =>
        Assert.Equal(expected, AppSettings.NormalizeGamePressDuration(value));

    [Fact]
    public void GamePressDuration_DefaultsToReliableMultiFrameValue() =>
        Assert.Equal(45, new AppSettings().GamePressDurationMilliseconds);

    [Fact]
    public void PointerPathRecording_DefaultsOff_AndRoundTrips()
    {
        var defaults = new AppSettings();
        Assert.False(defaults.RecordPointerPath);

        defaults.RecordPointerPath = true;
        var restored = JsonSerializer.Deserialize<AppSettings>(JsonSerializer.Serialize(defaults));

        Assert.NotNull(restored);
        Assert.True(restored.RecordPointerPath);
    }

    [Fact]
    public void FrozenUnderlineTransform_IsClonedBeforeAnimation()
    {
        var frozen = new ScaleTransform(0, 1);
        frozen.Freeze();

        var mutable = MainWindow.EnsureMutableScaleTransform(frozen);

        Assert.NotSame(frozen, mutable);
        Assert.False(mutable.IsFrozen);
        Assert.Equal(0, mutable.ScaleX);
        Assert.Equal(1, mutable.ScaleY);
    }
}
