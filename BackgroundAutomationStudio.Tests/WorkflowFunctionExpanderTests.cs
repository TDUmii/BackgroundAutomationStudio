using BackgroundAutomationStudio.Models;
using BackgroundAutomationStudio.Services;

namespace BackgroundAutomationStudio.Tests;

public sealed class WorkflowFunctionExpanderTests
{
    [Fact]
    public void Expand_InsertsNestedFunctionStepsInCallOrder()
    {
        var inner = new AutomationFunction { Name = "Confirm", Actions = [new ClickAction { ClientX = 40, ClientY = 50 }] };
        var outer = new AutomationFunction
        {
            Name = "Open and confirm",
            Actions =
            [
                new MovePointerAction { ClientX = 10, ClientY = 20 },
                new CallFunctionAction { FunctionId = inner.Id, FunctionName = inner.Name }
            ]
        };
        var first = new WaitAction { Milliseconds = 100 };
        AutomationAction[] workflow = [first, new CallFunctionAction { FunctionId = outer.Id, FunctionName = outer.Name, DelayBefore = 75 }];

        var expanded = WorkflowFunctionExpander.Expand(workflow, [inner, outer]);

        Assert.Same(first, expanded[0]);
        Assert.Equal(75, Assert.IsType<WaitAction>(expanded[1]).Milliseconds);
        Assert.IsType<MovePointerAction>(expanded[2]);
        Assert.IsType<ClickAction>(expanded[3]);
    }

    [Fact]
    public void Expand_SkipsDisabledSteps()
    {
        var function = new AutomationFunction { Name = "Optional", Actions = [new ClickAction { Enabled = false }, new WaitAction { Milliseconds = 10 }] };
        var expanded = WorkflowFunctionExpander.Expand([new CallFunctionAction { FunctionId = function.Id, FunctionName = function.Name }], [function]);
        Assert.IsType<WaitAction>(Assert.Single(expanded));
    }

    [Fact]
    public void Expand_RejectsCircularCalls()
    {
        var first = new AutomationFunction { Name = "First" };
        var second = new AutomationFunction { Name = "Second" };
        first.Actions.Add(new CallFunctionAction { FunctionId = second.Id, FunctionName = second.Name });
        second.Actions.Add(new CallFunctionAction { FunctionId = first.Id, FunctionName = first.Name });

        var error = Assert.Throws<InvalidOperationException>(() => WorkflowFunctionExpander.Expand([new CallFunctionAction { FunctionId = first.Id, FunctionName = first.Name }], [first, second]));
        Assert.Contains("cycle", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Expand_RejectsMissingFunction()
    {
        var error = Assert.Throws<InvalidOperationException>(() => WorkflowFunctionExpander.Expand([new CallFunctionAction { FunctionName = "Missing" }], []));
        Assert.Contains("was not found", error.Message);
    }
}
