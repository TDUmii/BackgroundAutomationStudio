using System.Text.Json;
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
}
