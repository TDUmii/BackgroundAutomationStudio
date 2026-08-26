using System.Text.Json;
using BackgroundAutomationStudio.MiniCore;

namespace BackgroundAutomationStudio.Tests;

public sealed class MiniEditionTests
{
    [Fact]
    public void RecordedSteps_ExposeReadableSummariesAndPortableJson()
    {
        RecordedMiniStep[] steps =
        [
            new("Click", 250, 12, 34),
            new("Scroll", 500, 20, 40, -120),
            new("Key", 75, Key: "ENTER")
        ];
        var export = new RecorderMiniExport("Recorder Mini", DateTimeOffset.Parse("2026-08-26T10:00:00+07:00"), "sample", "Sample Window", steps);

        var json = JsonSerializer.Serialize(export);

        Assert.Equal("Click  12, 34", steps[0].Summary);
        Assert.Equal("Scroll  -120 at 20, 40", steps[1].Summary);
        Assert.Equal("Key  ENTER", steps[2].Summary);
        Assert.Contains("Sample Window", json);
        Assert.DoesNotContain("Handle", json);
    }

    [Fact]
    public void WindowRelationship_RejectsMissingHandles()
    {
        Assert.False(MiniWindowService.IsTargetOrChild(nint.Zero, nint.Zero));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(32768, 0)]
    [InlineData(0, 32768)]
    public void ClientPointPacking_RejectsInvalidCoordinateRanges(int x, int y)
    {
        Assert.False(MiniWindowService.TryPackClientPoint(nint.Zero, x, y, out _));
    }
}
