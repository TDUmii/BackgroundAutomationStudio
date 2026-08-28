using System.Text.Json.Serialization;

namespace BackgroundAutomationStudio.MiniCore;

public sealed record MiniWindowTarget(nint Handle, string Title, string ProcessName)
{
    [JsonIgnore] public string Display => $"{ProcessName} - {Title}";

    public override string ToString() => Display;
}

public sealed record MiniPoint(int X, int Y);

public sealed record RecordedMiniStep(string Type, int DelayMilliseconds, int X = 0, int Y = 0, int Value = 0, string Key = "")
{
    [JsonIgnore] public string Summary => Type switch
    {
        "Click" => $"Click  {X}, {Y}",
        "RightClick" => $"Right click  {X}, {Y}",
        "Scroll" => $"Scroll  {Value} at {X}, {Y}",
        "Key" => $"Key  {Key}",
        _ => Type
    };

    [JsonIgnore] public string SummaryVi => Type switch
    {
        "Click" => $"Nhấp trái  {X}, {Y}",
        "RightClick" => $"Nhấp phải  {X}, {Y}",
        "Scroll" => $"Cuộn  {Value} tại {X}, {Y}",
        "Key" => $"Phím  {Key}",
        _ => Type
    };
}

public sealed record RecorderMiniExport(string Edition, DateTimeOffset CreatedAt, string TargetProcess, string TargetTitle, IReadOnlyList<RecordedMiniStep> Steps);
