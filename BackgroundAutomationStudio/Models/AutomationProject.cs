using System.Collections.ObjectModel;

namespace BackgroundAutomationStudio.Models;

public sealed class AutomationProject
{
    public int Version { get; set; } = 1;
    public string Name { get; set; } = "Untitled Project";
    public WindowTarget? Target { get; set; }
    public string RepeatMode { get; set; } = RepeatModes.Count;
    public int RepeatCount { get; set; } = 1;
    public int RepeatDurationMinutes { get; set; } = 30;
    public string StopAtTime { get; set; } = "23:00";
    public bool ShowCoordinateMap { get; set; }
    public bool ShowCoordinateGrid { get; set; } = true;
    public string MarkerColor { get; set; } = "#74A7FF";
    public string MarkerShape { get; set; } = MarkerShapes.Pin;
    public ObservableCollection<AutomationAction> Actions { get; set; } = [];
    public ObservableCollection<AutomationFunction> Functions { get; set; } = [];
}

public sealed class AutomationFunction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "New function";
    public ObservableCollection<AutomationAction> Actions { get; set; } = [];
    public AutomationFunction Clone() => new() { Id = Id, Name = Name, Actions = new(Actions.Select(action => action.Clone())) };
}

public static class MarkerShapes
{
    public const string Pin = "Pin";
    public const string Diamond = "Diamond";
    public const string Crosshair = "Crosshair";
    public static string Normalize(string? value) => value is Diamond or Crosshair ? value : Pin;
}

public static class RepeatModes
{
    public const string Count = "Count";
    public const string Infinite = "Infinite";
    public const string Duration = "Duration";
    public const string UntilTime = "UntilTime";

    public static string Normalize(string? value) => value switch
    {
        Infinite => Infinite,
        Duration => Duration,
        UntilTime => UntilTime,
        _ => Count
    };
}

public sealed record PlaybackRunOptions(
    string Mode,
    int RepeatCount,
    TimeSpan Duration,
    DateTimeOffset? StopAt)
{
    public static PlaybackRunOptions Count(int repeatCount) =>
        new(RepeatModes.Count, Math.Clamp(repeatCount, 1, 1_000_000), TimeSpan.Zero, null);

    public static DateTimeOffset GetNextStopAt(TimeSpan clockTime, DateTimeOffset now)
    {
        var candidate = new DateTimeOffset(now.Year, now.Month, now.Day, clockTime.Hours, clockTime.Minutes, 0, now.Offset);
        return candidate <= now ? candidate.AddDays(1) : candidate;
    }
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
