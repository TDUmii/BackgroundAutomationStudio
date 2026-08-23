using BackgroundAutomationStudio.Models;
using BackgroundAutomationStudio.Services;

namespace BackgroundAutomationStudio.Tests;

public sealed class ScriptParserTests
{
    private readonly ScriptParser _parser = new();

    [Fact]
    public void Parse_AcceptanceScript_CreatesExpectedWorkflow()
    {
        const string script = "CLICK 500 350\nWAIT 600\nTYPE \"Automation Test\"\nKEY TAB\nKEY ENTER";
        var result = _parser.Parse(script);
        Assert.True(result.IsValid);
        Assert.Collection(result.Actions,
            a => { var click = Assert.IsType<ClickAction>(a); Assert.Equal(500, click.ClientX); Assert.Equal(350, click.ClientY); },
            a => Assert.Equal(600, Assert.IsType<WaitAction>(a).Milliseconds),
            a => Assert.Equal("Automation Test", Assert.IsType<TypeTextAction>(a).Text),
            a => Assert.Equal("TAB", Assert.IsType<KeyPressAction>(a).KeyName),
            a => Assert.Equal("ENTER", Assert.IsType<KeyPressAction>(a).KeyName));
    }

    [Theory]
    [InlineData("CLCK 200 300", "Line 1: Unknown command \"CLCK\"")]
    [InlineData("CLICK abc 200", "Line 1: X must be a number")]
    [InlineData("WAIT -1", "Line 1: Milliseconds cannot be negative")]
    [InlineData("TYPE hello", "Line 1: Text must be enclosed in double quotes")]
    public void Parse_InvalidScript_ReturnsLineSpecificError(string script, string expected)
    {
        var result = _parser.Parse(script);
        Assert.False(result.IsValid);
        Assert.Equal(expected, result.Errors.Single().ToString());
    }

    [Fact]
    public void SerializeAndParse_PreservesAllV1ActionKindsAndEscapedText()
    {
        AutomationAction[] actions =
        [
            new ClickAction { ClientX = 1, ClientY = 2 }, new RightClickAction { ClientX = 3, ClientY = 4 },
            new DoubleClickAction { ClientX = 5, ClientY = 6 }, new TypeTextAction { Text = "Hello \"Umi\"\nNext" },
            new KeyPressAction { KeyName = "CTRL+C" }, new WaitAction { Milliseconds = 750, Enabled = false }
        ];
        var script = _parser.Serialize(actions);
        var parsed = _parser.Parse(script);
        Assert.True(parsed.IsValid);
        Assert.Equal(actions.Select(a => a.GetType()), parsed.Actions.Select(a => a.GetType()));
        Assert.Equal("Hello \"Umi\"\nNext", Assert.IsType<TypeTextAction>(parsed.Actions[3]).Text);
        Assert.False(parsed.Actions[5].Enabled);
    }

    [Fact]
    public void Parser_IgnoresBlankLinesAndComments()
    {
        var result = _parser.Parse("# workflow\n\nCLICK 10 20\n   # note\nWAIT 5");
        Assert.True(result.IsValid);
        Assert.Equal(2, result.Actions.Count);
    }
}
