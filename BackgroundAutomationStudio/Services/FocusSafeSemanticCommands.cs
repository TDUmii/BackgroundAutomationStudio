namespace BackgroundAutomationStudio.Services;

public sealed record FocusSafeSemanticCommand(string? Text, string? Key);

public static class FocusSafeSemanticCommands
{
    private static readonly IReadOnlyDictionary<string, FocusSafeSemanticCommand> Commands =
        new Dictionary<string, FocusSafeSemanticCommand>(StringComparer.Ordinal)
        {
            ["num0Button"] = Text("0"), ["num1Button"] = Text("1"), ["num2Button"] = Text("2"),
            ["num3Button"] = Text("3"), ["num4Button"] = Text("4"), ["num5Button"] = Text("5"),
            ["num6Button"] = Text("6"), ["num7Button"] = Text("7"), ["num8Button"] = Text("8"),
            ["num9Button"] = Text("9"), ["plusButton"] = Text("+"), ["minusButton"] = Text("-"),
            ["multiplyButton"] = Text("*"), ["divideButton"] = Text("/"),
            ["decimalSeparatorButton"] = Text("."), ["percentButton"] = Text("%"),
            ["equalButton"] = Text("="), ["invertButton"] = Text("r"), ["squareRootButton"] = Text("@"),
            ["clearButton"] = Key("ESCAPE"), ["clearEntryButton"] = Key("DELETE"),
            ["backSpaceButton"] = Key("BACKSPACE"), ["negateButton"] = Key("F9")
        };

    public static bool TryGet(string? automationId, out FocusSafeSemanticCommand command) =>
        Commands.TryGetValue(automationId ?? string.Empty, out command!);

    private static FocusSafeSemanticCommand Text(string text) => new(text, null);
    private static FocusSafeSemanticCommand Key(string key) => new(null, key);
}
