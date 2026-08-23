namespace BackgroundAutomationStudio.Models;

public sealed record ScriptError(int Line, string Message)
{
    public override string ToString() => $"Line {Line}: {Message}";
}

public sealed class ScriptParseResult
{
    public List<AutomationAction> Actions { get; } = [];
    public List<ScriptError> Errors { get; } = [];
    public bool IsValid => Errors.Count == 0;
}
