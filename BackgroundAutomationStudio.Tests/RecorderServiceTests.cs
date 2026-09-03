using BackgroundAutomationStudio.Native;
using BackgroundAutomationStudio.Services;

namespace BackgroundAutomationStudio.Tests;

public sealed class RecorderServiceTests
{
    [Fact]
    public void PointerPathSampler_RejectsRapidNoise_AndKeepsSpacedMovement()
    {
        var started = DateTime.UtcNow;
        var previous = new POINT(100, 100);

        Assert.True(RecorderService.ShouldCapturePointerMove(null, previous, false, started, previous));
        Assert.False(RecorderService.ShouldCapturePointerMove(started, previous, true, started.AddMilliseconds(20), new POINT(120, 120)));
        Assert.False(RecorderService.ShouldCapturePointerMove(started, previous, true, started.AddMilliseconds(40), new POINT(101, 100)));
        Assert.True(RecorderService.ShouldCapturePointerMove(started, previous, true, started.AddMilliseconds(40), new POINT(102, 100)));
    }
}
