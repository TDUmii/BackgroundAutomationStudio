using System.Collections.ObjectModel;

namespace BackgroundAutomationStudio.Models;

public sealed class AutomationProject
{
    public int Version { get; set; } = 1;
    public string Name { get; set; } = "Untitled Project";
    public WindowTarget? Target { get; set; }
    public int RepeatCount { get; set; } = 1;
    public ObservableCollection<AutomationAction> Actions { get; set; } = [];
}

public sealed class WindowTarget
{
    public string ProcessName { get; set; } = string.Empty;
    public int ProcessId { get; set; }
    public string WindowTitle { get; set; } = string.Empty;
    public string WindowTitleContains { get; set; } = string.Empty;
    public string WindowClassName { get; set; } = string.Empty;
    public int RecordedX { get; set; }
    public int RecordedY { get; set; }
    public int RecordedWidth { get; set; }
    public int RecordedHeight { get; set; }
    public long LastKnownHwnd { get; set; }

    public string HwndDisplay => LastKnownHwnd == 0 ? "Not resolved" : $"0x{LastKnownHwnd:X16}";
    public string LayoutDisplay => RecordedWidth <= 0 ? "Not captured" : $"X {RecordedX} - Y {RecordedY} - {RecordedWidth} x {RecordedHeight}";
}
