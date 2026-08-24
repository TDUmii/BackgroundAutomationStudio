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
