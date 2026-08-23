using System.Text.Json;
using BackgroundAutomationStudio.Models;

namespace BackgroundAutomationStudio.Services;

public sealed class WorkflowHistory
{
    private const int MaximumEntries = 100;
    private readonly Stack<string> _undo = new();
    private readonly Stack<string> _redo = new();
    private string _current = "[]";

    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;

    public void Reset(IEnumerable<AutomationAction> actions)
    {
        _undo.Clear();
        _redo.Clear();
        _current = Serialize(actions);
    }

    public bool Capture(IEnumerable<AutomationAction> actions)
    {
        var next = Serialize(actions);
        if (string.Equals(next, _current, StringComparison.Ordinal)) return false;
        PushBounded(_undo, _current);
        _current = next;
        _redo.Clear();
        return true;
    }

    public bool TryUndo(out IReadOnlyList<AutomationAction> actions)
    {
        if (_undo.Count == 0) { actions = []; return false; }
        PushBounded(_redo, _current);
        _current = _undo.Pop();
        actions = Deserialize(_current);
        return true;
    }

    public bool TryRedo(out IReadOnlyList<AutomationAction> actions)
    {
        if (_redo.Count == 0) { actions = []; return false; }
        PushBounded(_undo, _current);
        _current = _redo.Pop();
        actions = Deserialize(_current);
        return true;
    }

    private static string Serialize(IEnumerable<AutomationAction> actions) =>
        JsonSerializer.Serialize(actions.ToList(), ProjectService.JsonOptions);

    private static IReadOnlyList<AutomationAction> Deserialize(string snapshot) =>
        JsonSerializer.Deserialize<List<AutomationAction>>(snapshot, ProjectService.JsonOptions) ?? [];

    private static void PushBounded(Stack<string> stack, string snapshot)
    {
        stack.Push(snapshot);
        if (stack.Count <= MaximumEntries) return;
        var newest = stack.Take(MaximumEntries).Reverse().ToArray();
        stack.Clear();
        foreach (var item in newest) stack.Push(item);
    }
}
