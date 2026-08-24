using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using BackgroundAutomationStudio.Models;

namespace BackgroundAutomationStudio.Services;

public sealed partial class ScriptParser
{
    [GeneratedRegex("^TYPE\\s+\"((?:\\\\.|[^\"\\\\])*)\"$", RegexOptions.IgnoreCase)]
    private static partial Regex TypeRegex();
    [GeneratedRegex("^CALL\\s+\"((?:\\\\.|[^\"\\\\])*)\"$", RegexOptions.IgnoreCase)]
    private static partial Regex CallRegex();
    [GeneratedRegex("^WAIT_IMAGE\\s+(\\d+)\\s+(\\d+)\\s+(\\d+)\\s+(APPEAR|DISAPPEAR)\\s+(\\d+)\\s+(\\d+)\\s+(\\d+)\\s+(\\d+)\\s+\"((?:\\\\.|[^\"\\\\])*)\"\\s+([A-Za-z0-9+/=]+)$", RegexOptions.IgnoreCase)]
    private static partial Regex WaitImageRegex();
    [GeneratedRegex("^CLICK_IMAGE\\s+(\\d+)\\s+(\\d+)\\s+(\\d+)\\s+(LEFT|RIGHT)\\s+(-?\\d+)\\s+(-?\\d+)\\s+(\\d+)\\s+(\\d+)\\s+(\\d+)\\s+(\\d+)\\s+\"((?:\\\\.|[^\"\\\\])*)\"\\s+([A-Za-z0-9+/=]+)$", RegexOptions.IgnoreCase)]
    private static partial Regex ClickImageRegex();

    public ScriptParseResult Parse(string? script)
    {
        var result = new ScriptParseResult();
        var lines = (script ?? string.Empty).Replace("\r\n", "\n").Split('\n');
        var pendingNote = string.Empty;
        for (var i = 0; i < lines.Length; i++)
        {
            var raw = lines[i].Trim();
            if (raw.StartsWith("# NOTE ", StringComparison.OrdinalIgnoreCase)) { pendingNote = raw[7..].Trim(); continue; }
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
                "WAIT_IMAGE" => ParseWaitImage(raw, i + 1, result),
                "CLICK_IMAGE" => ParseClickImage(raw, i + 1, result),
                "WAIT_COLOR" => ParseWaitColor(args, i + 1, result),
                "CLICK_COLOR" => ParseClickColor(args, i + 1, result),
                "KEY" => ParseKey(args, i + 1, result),
                "HOLD" => ParseHold(args, i + 1, result),
                "DRAG" => ParseDrag(args, i + 1, result),
                "SCROLL" => ParseScroll(args, i + 1, result),
                "MOVE" => ParsePoint<MovePointerAction>(args, i + 1, result),
                "CALL" => ParseCall(raw, i + 1, result),
                "TYPE" => ParseType(raw, i + 1, result),
                _ => AddUnknown(command, i + 1, result)
            };
            if (action is not null) { action.Enabled = enabled; action.Note = pendingNote; pendingNote = string.Empty; result.Actions.Add(action); }
        }
        return result;
    }

    public string Serialize(IEnumerable<AutomationAction> actions)
    {
        var lines = new List<string>();
        foreach (var action in actions)
        {
            if (action.DelayBefore > 0) lines.Add($"WAIT {action.DelayBefore}");
            if (!string.IsNullOrWhiteSpace(action.Note)) lines.Add($"# NOTE {action.Note.Replace('\r', ' ').Replace('\n', ' ')}");
            var line = action switch
            {
                ClickAction a => $"CLICK {a.ClientX} {a.ClientY}",
                RightClickAction a => $"RIGHT_CLICK {a.ClientX} {a.ClientY}",
                DoubleClickAction a => $"DOUBLE_CLICK {a.ClientX} {a.ClientY}",
                TypeTextAction a => $"TYPE \"{Escape(a.Text)}\"",
                KeyPressAction a => $"KEY {a.KeyName.ToUpperInvariant()}",
                KeyHoldAction a => $"HOLD {a.KeyName.ToUpperInvariant()} {a.Milliseconds}",
                DragAction a => $"DRAG {a.StartX} {a.StartY} {a.EndX} {a.EndY} {a.Milliseconds}",
                ScrollAction a => $"SCROLL {a.ClientX} {a.ClientY} {a.Delta}",
                MovePointerAction a => $"MOVE {a.ClientX} {a.ClientY}",
                CallFunctionAction a => $"CALL \"{Escape(a.FunctionName)}\"",
                WaitAction a => $"WAIT {a.Milliseconds}",
                WaitForImageAction a => $"WAIT_IMAGE {a.SimilarityPercent} {a.TimeoutMilliseconds} {a.PollIntervalMilliseconds} {(a.WaitForDisappear ? "DISAPPEAR" : "APPEAR")} {a.RegionX} {a.RegionY} {a.RegionWidth} {a.RegionHeight} \"{Escape(a.TemplateName)}\" {Convert.ToBase64String(a.TemplatePng)}",
                ClickImageAction a => $"CLICK_IMAGE {a.SimilarityPercent} {a.TimeoutMilliseconds} {a.PollIntervalMilliseconds} {(a.RightClick ? "RIGHT" : "LEFT")} {a.OffsetX} {a.OffsetY} {a.RegionX} {a.RegionY} {a.RegionWidth} {a.RegionHeight} \"{Escape(a.TemplateName)}\" {Convert.ToBase64String(a.TemplatePng)}",
                WaitForColorAction a => $"WAIT_COLOR {a.ColorHex} {a.Tolerance} {a.MinimumPixels} {a.TimeoutMilliseconds} {a.PollIntervalMilliseconds} {(a.WaitForDisappear ? "DISAPPEAR" : "APPEAR")} {a.RegionX} {a.RegionY} {a.RegionWidth} {a.RegionHeight}",
                ClickColorAction a => $"CLICK_COLOR {a.ColorHex} {a.Tolerance} {a.MinimumPixels} {a.TimeoutMilliseconds} {a.PollIntervalMilliseconds} {(a.RightClick ? "RIGHT" : "LEFT")} {a.OffsetX} {a.OffsetY} {a.RegionX} {a.RegionY} {a.RegionWidth} {a.RegionHeight}",
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
        if (x < 0 || y < 0)
        {
            var label = typeof(T) == typeof(MovePointerAction) ? "Move" : "Pointer";
            result.Errors.Add(new(line, $"{label} coordinates cannot be negative"));
            return null;
        }
        return new T { ClientX = x, ClientY = y };
    }

    private static AutomationAction? ParseWait(string args, int line, ScriptParseResult result)
    {
        if (!int.TryParse(args, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)) { result.Errors.Add(new(line, "Milliseconds must be a number")); return null; }
        if (value < 0) { result.Errors.Add(new(line, "Milliseconds cannot be negative")); return null; }
        return new WaitAction { Milliseconds = value };
    }

    private static AutomationAction? ParseWaitImage(string raw, int line, ScriptParseResult result)
    {
        var match = WaitImageRegex().Match(raw);
        if (!match.Success) { result.Errors.Add(new(line, "Expected similarity, timeout, poll interval, APPEAR or DISAPPEAR, region X/Y/width/height, template name, and PNG data")); return null; }
        if (!TryParseImageCommon(match, 1, line, result, out var values, out var name, out var png)) return null;
        return new WaitForImageAction
        {
            SimilarityPercent = values[0], TimeoutMilliseconds = values[1], PollIntervalMilliseconds = values[2],
            WaitForDisappear = match.Groups[4].Value.Equals("DISAPPEAR", StringComparison.OrdinalIgnoreCase),
            RegionX = values[3], RegionY = values[4], RegionWidth = values[5], RegionHeight = values[6], TemplateName = name, TemplatePng = png
        };
    }

    private static AutomationAction? ParseClickImage(string raw, int line, ScriptParseResult result)
    {
        var match = ClickImageRegex().Match(raw);
        if (!match.Success) { result.Errors.Add(new(line, "Expected similarity, timeout, poll interval, LEFT or RIGHT, offsets, region X/Y/width/height, template name, and PNG data")); return null; }
        var indexes = new[] { 1, 2, 3, 7, 8, 9, 10 };
        var values = new int[7];
        for (var index = 0; index < indexes.Length; index++)
            if (!int.TryParse(match.Groups[indexes[index]].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out values[index])) { result.Errors.Add(new(line, "Image scan values must be whole numbers")); return null; }
        if (!ValidateImageValues(values, line, result)) return null;
        if (!int.TryParse(match.Groups[5].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var offsetX) || !int.TryParse(match.Groups[6].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var offsetY)) { result.Errors.Add(new(line, "Image click offsets must be whole numbers")); return null; }
        try
        {
            var png = Convert.FromBase64String(match.Groups[12].Value);
            if (png.Length == 0) throw new FormatException();
            return new ClickImageAction { SimilarityPercent = values[0], TimeoutMilliseconds = values[1], PollIntervalMilliseconds = values[2], RightClick = match.Groups[4].Value.Equals("RIGHT", StringComparison.OrdinalIgnoreCase), OffsetX = offsetX, OffsetY = offsetY, RegionX = values[3], RegionY = values[4], RegionWidth = values[5], RegionHeight = values[6], TemplateName = Unescape(match.Groups[11].Value), TemplatePng = png };
        }
        catch (FormatException) { result.Errors.Add(new(line, "Image template data must be valid Base64")); return null; }
    }

    private static bool TryParseImageCommon(Match match, int startGroup, int line, ScriptParseResult result, out int[] values, out string name, out byte[] png)
    {
        var indexes = new[] { startGroup, startGroup + 1, startGroup + 2, startGroup + 4, startGroup + 5, startGroup + 6, startGroup + 7 };
        values = new int[7]; name = string.Empty; png = [];
        for (var index = 0; index < indexes.Length; index++)
            if (!int.TryParse(match.Groups[indexes[index]].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out values[index])) { result.Errors.Add(new(line, "Image scan values must be whole numbers")); return false; }
        if (!ValidateImageValues(values, line, result)) return false;
        try { name = Unescape(match.Groups[startGroup + 8].Value); png = Convert.FromBase64String(match.Groups[startGroup + 9].Value); if (png.Length == 0) throw new FormatException(); return true; }
        catch (FormatException) { result.Errors.Add(new(line, "Image template data must be valid Base64")); return false; }
    }

    private static bool ValidateImageValues(int[] values, int line, ScriptParseResult result)
    {
        if (values[0] is < 1 or > 100) { result.Errors.Add(new(line, "Similarity must be from 1 to 100")); return false; }
        if (values[1] < 0 || values[2] is < 50 or > 10000 || values.Skip(3).Any(value => value < 0)) { result.Errors.Add(new(line, "Image scan timeout and region values are invalid")); return false; }
        if (values[5] == 0 ^ values[6] == 0) { result.Errors.Add(new(line, "Image scan region width and height must both be zero or both be positive")); return false; }
        return true;
    }

    private static AutomationAction? ParseWaitColor(string args, int line, ScriptParseResult result)
    {
        var parts = args.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 10) { result.Errors.Add(new(line, "Expected HEX color, tolerance, minimum pixels, timeout, poll interval, APPEAR or DISAPPEAR, and region X/Y/width/height")); return null; }
        if (!TryParseColorCommon(parts, line, result, out var values)) return null;
        var mode = parts[5].ToUpperInvariant();
        if (mode is not ("APPEAR" or "DISAPPEAR")) { result.Errors.Add(new(line, "Color wait mode must be APPEAR or DISAPPEAR")); return null; }
        return new WaitForColorAction { ColorHex = parts[0], Tolerance = values[0], MinimumPixels = values[1], TimeoutMilliseconds = values[2], PollIntervalMilliseconds = values[3], WaitForDisappear = mode == "DISAPPEAR", RegionX = values[4], RegionY = values[5], RegionWidth = values[6], RegionHeight = values[7] };
    }

    private static AutomationAction? ParseClickColor(string args, int line, ScriptParseResult result)
    {
        var parts = args.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 12) { result.Errors.Add(new(line, "Expected HEX color, tolerance, minimum pixels, timeout, poll interval, LEFT or RIGHT, offsets, and region X/Y/width/height")); return null; }
        var commonParts = new[] { parts[0], parts[1], parts[2], parts[3], parts[4], parts[5], parts[8], parts[9], parts[10], parts[11] };
        if (!TryParseColorCommon(commonParts, line, result, out var values)) return null;
        var button = parts[5].ToUpperInvariant();
        if (button is not ("LEFT" or "RIGHT")) { result.Errors.Add(new(line, "Color click button must be LEFT or RIGHT")); return null; }
        if (!int.TryParse(parts[6], NumberStyles.Integer, CultureInfo.InvariantCulture, out var offsetX) || !int.TryParse(parts[7], NumberStyles.Integer, CultureInfo.InvariantCulture, out var offsetY)) { result.Errors.Add(new(line, "Color click offsets must be whole numbers")); return null; }
        return new ClickColorAction { ColorHex = parts[0], Tolerance = values[0], MinimumPixels = values[1], TimeoutMilliseconds = values[2], PollIntervalMilliseconds = values[3], RightClick = button == "RIGHT", OffsetX = offsetX, OffsetY = offsetY, RegionX = values[4], RegionY = values[5], RegionWidth = values[6], RegionHeight = values[7] };
    }

    private static bool TryParseColorCommon(string[] parts, int line, ScriptParseResult result, out int[] values)
    {
        values = new int[8];
        if (!ColorScanAction.TryParseColor(parts[0], out _, out _, out _)) { result.Errors.Add(new(line, "Color must use #RRGGBB format")); return false; }
        var indexes = new[] { 1, 2, 3, 4, 6, 7, 8, 9 };
        for (var index = 0; index < indexes.Length; index++)
            if (!int.TryParse(parts[indexes[index]], NumberStyles.Integer, CultureInfo.InvariantCulture, out values[index])) { result.Errors.Add(new(line, "Color scan values must be whole numbers")); return false; }
        if (values[0] is < 0 or > 255 || values[1] <= 0 || values[2] < 0 || values[3] is < 50 or > 10000 || values.Skip(4).Any(value => value < 0)) { result.Errors.Add(new(line, "Color scan tolerance, timing, minimum pixels, or region is invalid")); return false; }
        if (values[6] == 0 ^ values[7] == 0) { result.Errors.Add(new(line, "Color scan region width and height must both be zero or both be positive")); return false; }
        return true;
    }

    private static AutomationAction? ParseKey(string args, int line, ScriptParseResult result)
    {
        var key = args.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(key)) { result.Errors.Add(new(line, "Key name is required")); return null; }
        if (!KeyNames.IsSupported(key)) { result.Errors.Add(new(line, $"Unsupported key \"{key}\"")); return null; }
        return new KeyPressAction { KeyName = key };
    }

    private static AutomationAction? ParseHold(string args, int line, ScriptParseResult result)
    {
        var parts = args.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2) { result.Errors.Add(new(line, "Expected a key name and hold duration in milliseconds")); return null; }
        var key = parts[0].ToUpperInvariant();
        if (!KeyNames.IsSupported(key)) { result.Errors.Add(new(line, $"Unsupported key \"{key}\"")); return null; }
        if (!int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var milliseconds) || milliseconds <= 0) { result.Errors.Add(new(line, "Hold duration must be a positive number")); return null; }
        return new KeyHoldAction { KeyName = key, Milliseconds = milliseconds };
    }

    private static AutomationAction? ParseDrag(string args, int line, ScriptParseResult result)
    {
        var parts = args.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 5) { result.Errors.Add(new(line, "Expected start X/Y, end X/Y, and duration")); return null; }
        var values = new int[5];
        for (var index = 0; index < values.Length; index++)
            if (!int.TryParse(parts[index], NumberStyles.Integer, CultureInfo.InvariantCulture, out values[index])) { result.Errors.Add(new(line, "Drag values must be numbers")); return null; }
        if (values.Take(4).Any(value => value < 0)) { result.Errors.Add(new(line, "Drag coordinates cannot be negative")); return null; }
        if (values[4] <= 0) { result.Errors.Add(new(line, "Drag duration must be positive")); return null; }
        return new DragAction { StartX = values[0], StartY = values[1], EndX = values[2], EndY = values[3], Milliseconds = values[4] };
    }

    private static AutomationAction? ParseScroll(string args, int line, ScriptParseResult result)
    {
        var parts = args.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3) { result.Errors.Add(new(line, "Expected X, Y, and wheel delta")); return null; }
        var values = new int[3];
        for (var index = 0; index < values.Length; index++)
            if (!int.TryParse(parts[index], NumberStyles.Integer, CultureInfo.InvariantCulture, out values[index])) { result.Errors.Add(new(line, "Scroll values must be numbers")); return null; }
        if (values[0] < 0 || values[1] < 0) { result.Errors.Add(new(line, "Scroll coordinates cannot be negative")); return null; }
        if (values[2] == 0 || values[2] < -12000 || values[2] > 12000) { result.Errors.Add(new(line, "Wheel delta must be between -12000 and 12000, excluding zero")); return null; }
        return new ScrollAction { ClientX = values[0], ClientY = values[1], Delta = values[2] };
    }

    private static AutomationAction? ParseType(string raw, int line, ScriptParseResult result)
    {
        var match = TypeRegex().Match(raw);
        if (!match.Success) { result.Errors.Add(new(line, "Text must be enclosed in double quotes")); return null; }
        try { return new TypeTextAction { Text = Unescape(match.Groups[1].Value) }; }
        catch (FormatException ex) { result.Errors.Add(new(line, ex.Message)); return null; }
    }

    private static AutomationAction? ParseCall(string raw, int line, ScriptParseResult result)
    {
        var match = CallRegex().Match(raw);
        if (!match.Success) { result.Errors.Add(new(line, "Function name must be enclosed in double quotes")); return null; }
        try
        {
            var name = Unescape(match.Groups[1].Value).Trim();
            if (string.IsNullOrWhiteSpace(name)) { result.Errors.Add(new(line, "Function name is required")); return null; }
            return new CallFunctionAction { FunctionName = name };
        }
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
    public static bool IsSupported(string key) => Supported.Contains(key) || (key.Length == 1 && char.IsLetterOrDigit(key[0])) || (key.Length is 2 or 3 && key[0] == 'F' && int.TryParse(key[1..], out var f) && f is >= 1 and <= 12) || IsCombination(key);
    private static bool IsCombination(string key)
    {
        var parts = key.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 2) return false;
        return parts[..^1].All(p => p is "CTRL" or "SHIFT" or "ALT") && (parts[^1].Length == 1 || Supported.Contains(parts[^1]));
    }
}
