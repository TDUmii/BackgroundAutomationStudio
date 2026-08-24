using BackgroundAutomationStudio.Services;
using OpenCvSharp;

namespace BackgroundAutomationStudio.Tests;

public sealed class VisualMatchingServiceTests
{
    [Fact]
    public void FindInPng_FindsTemplateCenterInsideRestrictedRegion()
    {
        using var frame = new Mat(new Size(220, 160), MatType.CV_8UC3, Scalar.Black);
        Cv2.Rectangle(frame, new Rect(120, 70, 32, 24), new Scalar(20, 220, 70), -1);
        Cv2.Line(frame, new Point(120, 70), new Point(151, 93), Scalar.White, 3);
        using var template = new Mat(frame, new Rect(120, 70, 32, 24));
        Cv2.ImEncode(".png", frame, out var framePng);
        Cv2.ImEncode(".png", template, out var templatePng);

        var match = VisualMatchingService.FindInPng(framePng, templatePng, 80, 40, 110, 90, 0.9);

        Assert.True(match.Found);
        Assert.InRange(match.Similarity, 0.99, 1.0);
        Assert.Equal((136, 82), (match.CenterX, match.CenterY));
    }

    [Fact]
    public void FindInPng_ReturnsNotFoundWhenThresholdIsNotMet()
    {
        using var frame = new Mat(new Size(100, 100), MatType.CV_8UC3, Scalar.Black);
        using var template = new Mat(new Size(20, 20), MatType.CV_8UC3, Scalar.White);
        Cv2.ImEncode(".png", frame, out var framePng);
        Cv2.ImEncode(".png", template, out var templatePng);

        var match = VisualMatchingService.FindInPng(framePng, templatePng, 0, 0, 0, 0, 0.99);

        Assert.False(match.Found);
    }
}
