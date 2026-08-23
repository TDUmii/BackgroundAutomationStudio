using BackgroundAutomationStudio.Models;
using BackgroundAutomationStudio.Services;

namespace BackgroundAutomationStudio.Tests;

public sealed class WorkflowHistoryTests
{
    [Fact]
    public void UndoAndRedo_RestoreCompleteWorkflowAndStableIds()
    {
        var first = new ClickAction { ClientX = 10, ClientY = 20 };
        var history = new WorkflowHistory();
        history.Reset([first]);

        var second = new TypeTextAction { Text = "continuous background test" };
        Assert.True(history.Capture([first, second]));
        Assert.True(history.CanUndo);

        Assert.True(history.TryUndo(out var undone));
        Assert.Single(undone);
        Assert.Equal(first.Id, undone[0].Id);
        Assert.True(history.CanRedo);

        Assert.True(history.TryRedo(out var redone));
        Assert.Collection(redone,
            action => Assert.Equal(first.Id, Assert.IsType<ClickAction>(action).Id),
            action => Assert.Equal("continuous background test", Assert.IsType<TypeTextAction>(action).Text));
    }

    [Fact]
    public void NewEditAfterUndo_ClearsRedoBranch()
    {
        var history = new WorkflowHistory();
        history.Reset([]);
        history.Capture([new WaitAction { Milliseconds = 100 }]);
        history.Capture([new WaitAction { Milliseconds = 200 }]);
        Assert.True(history.TryUndo(out _));
        Assert.True(history.CanRedo);

        history.Capture([new WaitAction { Milliseconds = 300 }]);
        Assert.False(history.CanRedo);
    }

    [Fact]
    public void IdenticalSnapshot_DoesNotCreateUndoEntry()
    {
        var action = new KeyPressAction { KeyName = "ENTER" };
        var history = new WorkflowHistory();
        history.Reset([action]);
        Assert.False(history.Capture([action]));
        Assert.False(history.CanUndo);
    }
}
