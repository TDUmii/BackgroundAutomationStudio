using BackgroundAutomationStudio.Services;

namespace BackgroundAutomationStudio.Tests;

public sealed class FocusSafeSemanticCommandsTests
{
    [Theory]
    [InlineData("num7Button", "7", null)]
    [InlineData("plusButton", "+", null)]
    [InlineData("equalButton", "=", null)]
    [InlineData("clearButton", null, "ESCAPE")]
    [InlineData("backSpaceButton", null, "BACKSPACE")]
    public void CalculatorControl_ReturnsFocusSafeCommand(string automationId, string? text, string? key)
    {
        Assert.True(FocusSafeSemanticCommands.TryGet(automationId, out var command));
        Assert.Equal(text, command.Text);
        Assert.Equal(key, command.Key);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("unknownButton")]
    public void UnknownControl_HasNoSemanticCommand(string? automationId) =>
        Assert.False(FocusSafeSemanticCommands.TryGet(automationId, out _));
}
