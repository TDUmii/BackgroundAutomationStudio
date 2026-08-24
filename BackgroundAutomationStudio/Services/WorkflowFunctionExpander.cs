using BackgroundAutomationStudio.Models;

namespace BackgroundAutomationStudio.Services;

public static class WorkflowFunctionExpander
{
    public static IReadOnlyList<AutomationAction> Expand(IEnumerable<AutomationAction> actions, IEnumerable<AutomationFunction> functions)
    {
        var catalog = functions.ToList();
        var output = new List<AutomationAction>();
        ExpandInto(actions, catalog, output, [], 0);
        return output;
    }

    private static void ExpandInto(IEnumerable<AutomationAction> actions, IReadOnlyList<AutomationFunction> functions, List<AutomationAction> output, HashSet<Guid> stack, int depth)
    {
        if (depth > 32) throw new InvalidOperationException("Function nesting is deeper than 32 levels.");
        foreach (var action in actions.Where(item => item.Enabled))
        {
            // Keep top-level instances intact so the editor can highlight the
            // exact row currently being executed. Function steps are expanded
            // below and do not have a top-level row to select.
            if (action is not CallFunctionAction call) { output.Add(action); continue; }
            if (call.DelayBefore > 0) output.Add(new WaitAction { Milliseconds = call.DelayBefore });
            var function = functions.FirstOrDefault(item => call.FunctionId != Guid.Empty && item.Id == call.FunctionId)
                ?? functions.FirstOrDefault(item => string.Equals(item.Name, call.FunctionName, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException($"Function \"{call.FunctionName}\" was not found.");
            if (!stack.Add(function.Id)) throw new InvalidOperationException($"Function cycle detected at \"{function.Name}\".");
            ExpandInto(function.Actions, functions, output, stack, depth + 1);
            stack.Remove(function.Id);
        }
    }
}
