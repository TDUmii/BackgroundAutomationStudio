using System.Windows;
using BackgroundAutomationStudio.Controls;

namespace BackgroundAutomationStudio.Tests;

public sealed class ScreenCoordinateOverlayControlTests
{
    [Fact]
    public void CursorLabel_StaysAbovePointer_WhenThereIsRoom()
    {
        var label = ScreenCoordinateOverlayControl.CalculateCursorLabelBounds(new Rect(0, 0, 800, 600), new Point(400, 300), 90, 28);
        Assert.True(label.Bottom < 300);
        Assert.Equal(355, label.Left);
    }

    [Fact]
    public void CursorLabel_MovesBelowAndClamps_WhenPointerIsNearEdges()
    {
        var nearTop = ScreenCoordinateOverlayControl.CalculateCursorLabelBounds(new Rect(0, 0, 200, 100), new Point(190, 5), 90, 28);
        Assert.True(nearTop.Top > 5);
        Assert.True(nearTop.Right <= 196);
        Assert.True(nearTop.Bottom <= 96);
    }
}
