using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using BackgroundAutomationStudio.Models;

namespace BackgroundAutomationStudio.Services;

public sealed partial class ScriptParser
{
    [GeneratedRegex("^TYPE\\s+\"((?:\\\\.|[^\"\\\\])*)\"$", RegexOptions.IgnoreCase)]
    private static partial Regex TypeRegex();

    public ScriptParseResult Parse(string? script)
    {
        var result = new ScriptParseResult();
        var lines = (script ?? string.Empty).Replace("\r\n", "\n").Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var raw = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(raw) || raw.StartsWith('#') && !raw.StartsWith("# DISABLED ", StringComparison.OrdinalIgnoreCase)) continue;
            var enabled = true;
            if (raw.StartsWith("# DISABLED ", StringComparison.OrdinalIgnoreCase))
            {
                enabled = false;
                raw = raw[11..].TrimStart();
            }

            var firstSpace = raw.IndexOfAny([' ', '\t']);
            var command = (firstSpace < 0 ? raw : raw[..firstSpace]).ToUpperInvariant();
            var args = firstSpace < 0 ? string.Empty : raw[(firstSpace + 1)..].Trim();
            AutomationAction? action = command switch
            {
                "CLICK" => ParsePoint<ClickAction>(args, i + 1, result),
                "RIGHT_CLICK" => ParsePoint<RightClickAction>(args, i + 1, result),
                "DOUBLE_CLICK" => ParsePoint<DoubleClickAction>(args, i + 1, result),
                "WAIT" => ParseWait(args, i + 1, result),
                "KEY" => ParseKey(args, i + 1, result),
                "TYPE" => ParseType(raw, i + 1, result),
                _ => AddUnknown(command, i + 1, result)
            };
            if (action is not null) { action.Enabled = enabled; result.Actions.Add(action); }
        }
        return result;
    }

    public string Serialize(IEnumerable<AutomationAction> actions)
    {
        var lines = new List<string>();
        foreach (var action in actions)
        {
            if (action.DelayBefore > 0) lines.Add($"WAIT {action.DelayBefore}");
            var line = action switch
            {
                ClickAction a => $"CLICK {a.ClientX} {a.ClientY}",
                RightClickAction a => $"RIGHT_CLICK {a.ClientX} {a.ClientY}",
                DoubleClickAction a => $"DOUBLE_CLICK {a.ClientX} {a.ClientY}",
                TypeTextAction a => $"TYPE \"{Escape(a.Text)}\"",
                KeyPressAction a => $"KEY {a.KeyName.ToUpperInvariant()}",
                WaitAction a => $"WAIT {a.Milliseconds}",
                _ => throw new InvalidOperationException($"Unsupported action {action.GetType().Name}.")
            };
            lines.Add(action.Enabled ? line : $"# DISABLED {line}");
        }
        return string.Join(Environment.NewLine, lines);
    }

    private static AutomationAction? ParsePoint<T>(string args, int line, ScriptParseResult result) where T : PointerAction, new()
    {
        var parts = args.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2) { result.Errors.Add(new(line, "Expected X and Y coordinates")); return null; }
        if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var x)) { result.Errors.Add(new(line, "X must be a number")); return null; }
        if (!int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var y)) { result.Errors.Add(new(line, "Y must be a number")); return null; }
        return new T { ClientX = x, ClientY = y };
    }

    private static AutomationAction? ParseWait(string args, int line, ScriptParseResult result)
    {
        if (!int.TryParse(args, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)) { result.Errors.Add(new(line, "Milliseconds must be a number")); return null; }
        if (value < 0) { result.Errors.Add(new(line, "Milliseconds cannot be negative")); return null; }
        return new WaitAction { Milliseconds = value };
    }

    private static AutomationAction? ParseKey(string args, int line, ScriptParseResult result)
    {
        var key = args.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(key)) { result.Errors.Add(new(line, "Key name is required")); return null; }
        if (!KeyNames.IsSupported(key)) { result.Errors.Add(new(line, $"Unsupported key \"{key}\"")); return null; }
        return new KeyPressAction { KeyName = key };
    }

    private static AutomationAction? ParseType(string raw, int line, ScriptParseResult result)
    {
        var match = TypeRegex().Match(raw);
        if (!match.Success) { result.Errors.Add(new(line, "Text must be enclosed in double quotes")); return null; }
        try { return new TypeTextAction { Text = Unescape(match.Groups[1].Value) }; }
        catch (FormatException ex) { result.Errors.Add(new(line, ex.Message)); return null; }
    }

    private static AutomationAction? AddUnknown(string command, int line, ScriptParseResult result)
    {
        result.Errors.Add(new(line, $"Unknown command \"{command}\""));
        return null;
    }

    private static string Escape(string text) => text.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
    private static string Unescape(string text)
    {
        var output = new StringBuilder();
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] != '\\') { output.Append(text[i]); continue; }
            if (++i >= text.Length) throw new FormatException("Text ends with an incomplete escape sequence");
            output.Append(text[i] switch { '\\' => '\\', '"' => '"', 'n' => '\n', 'r' => '\r', 't' => '\t', _ => throw new FormatException($"Unsupported escape sequence \\{text[i]}") });
        }
        return output.ToString();
    }
}

public static class KeyNames
{
    private static readonly HashSet<string> Supported = new(StringComparer.OrdinalIgnoreCase)
    {
        "ENTER", "TAB", "ESCAPE", "BACKSPACE", "DELETE", "UP", "DOWN", "LEFT", "RIGHT", "CTRL", "SHIFT", "ALT",
        "HOME", "END", "PAGEUP", "PAGEDOWN", "SPACE"
    };
    public static bool IsSupported(string key) => Supported.Contains(key) || (key.Length is 2 or 3 && key[0] == 'F' && int.TryParse(key[1..], out var f) && f is >= 1 and <= 12) || IsCombination(key);
    private static bool IsCombination(string key)
    {
        var parts = key.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 2) return false;
        return parts[..^1].All(p => p is "CTRL" or "SHIFT" or "ALT") && (parts[^1].Length == 1 || Supported.Contains(parts[^1]));
    }
}
