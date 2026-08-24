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

    [Fact]
    public void Parse_GameMacroActions_PreservesHoldAndDrag()
    {
        var result = _parser.Parse("HOLD SHIFT+E 1250\nDRAG 10 20 300 400 900");
        Assert.True(result.IsValid);
        var hold = Assert.IsType<KeyHoldAction>(result.Actions[0]);
        Assert.Equal(("SHIFT+E", 1250), (hold.KeyName, hold.Milliseconds));
        var drag = Assert.IsType<DragAction>(result.Actions[1]);
        Assert.Equal((10, 20, 300, 400, 900), (drag.StartX, drag.StartY, drag.EndX, drag.EndY, drag.Milliseconds));
        Assert.Equal("HOLD SHIFT+E 1250\r\nDRAG 10 20 300 400 900", _parser.Serialize(result.Actions));
    }

    [Fact]
    public void Parse_RepeatedRightClickGameWorkflow_PreservesEveryStep()
    {
        const string script = "RIGHT_CLICK 557 983\nWAIT 731\nRIGHT_CLICK 597 980\nWAIT 1123\nRIGHT_CLICK 615 990\nWAIT 804\nRIGHT_CLICK 557 982\nWAIT 607\nRIGHT_CLICK 608 984\nWAIT 550\nRIGHT_CLICK 569 985\nWAIT 671\nRIGHT_CLICK 620 979\nWAIT 515\nRIGHT_CLICK 546 981\nWAIT 536\nRIGHT_CLICK 600 980";
        var result = _parser.Parse(script);
        Assert.True(result.IsValid);
        Assert.Equal(17, result.Actions.Count);
        Assert.Equal(9, result.Actions.OfType<RightClickAction>().Count());
        Assert.Equal(8, result.Actions.OfType<WaitAction>().Count());
        var last = Assert.IsType<RightClickAction>(result.Actions[^1]);
        Assert.Equal((600, 980), (last.ClientX, last.ClientY));
    }

    [Fact]
    public void Parse_RepeatedClickDstWorkflow_PreservesEveryStep()
    {
        const string script = "CLICK 245 720\nWAIT 937\nCLICK 263 526\nWAIT 398\nCLICK 242 584\nWAIT 331\nCLICK 228 633\nWAIT 602\nCLICK 226 673\nWAIT 1023\nCLICK 212 923";
        var result = _parser.Parse(script);
        Assert.True(result.IsValid);
        Assert.Equal(11, result.Actions.Count);
        Assert.Equal(6, result.Actions.OfType<ClickAction>().Count());
        Assert.Equal(5, result.Actions.OfType<WaitAction>().Count());
        var last = Assert.IsType<ClickAction>(result.Actions[^1]);
        Assert.Equal((212, 923), (last.ClientX, last.ClientY));
    }

    [Theory]
    [InlineData("HOLD E 0", "Line 1: Hold duration must be a positive number")]
    [InlineData("DRAG 0 0 10 10 0", "Line 1: Drag duration must be positive")]
    [InlineData("DRAG -1 0 10 10 100", "Line 1: Drag coordinates cannot be negative")]
    public void Parse_InvalidGameMacroAction_ReturnsSpecificError(string script, string expected)
    {
        var result = _parser.Parse(script);
        Assert.False(result.IsValid);
        Assert.Equal(expected, result.Errors.Single().ToString());
    }

    [Fact]
    public void ScrollAndNote_RoundTripThroughDsl()
    {
        var original = new ScrollAction { ClientX = 320, ClientY = 240, Delta = -360, Note = "Scroll the inventory list" };
        var script = _parser.Serialize([original]);
        Assert.Equal("# NOTE Scroll the inventory list\r\nSCROLL 320 240 -360", script);
        var parsed = _parser.Parse(script);
        Assert.True(parsed.IsValid);
        var scroll = Assert.IsType<ScrollAction>(Assert.Single(parsed.Actions));
        Assert.Equal((320, 240, -360, "Scroll the inventory list"), (scroll.ClientX, scroll.ClientY, scroll.Delta, scroll.Note));
    }

    [Theory]
    [InlineData("SCROLL 1 2 0", "Line 1: Wheel delta must be between -12000 and 12000, excluding zero")]
    [InlineData("SCROLL -1 2 120", "Line 1: Scroll coordinates cannot be negative")]
    public void Parse_InvalidScroll_ReturnsSpecificError(string script, string expected)
    {
        var result = _parser.Parse(script);
        Assert.False(result.IsValid);
        Assert.Equal(expected, result.Errors.Single().ToString());
    }

    [Fact]
    public void MoveAndCall_RoundTripThroughDsl()
    {
        AutomationAction[] actions =
        [
            new MovePointerAction { ClientX = 420, ClientY = 260, Note = "Position before the next step" },
            new CallFunctionAction { FunctionName = "Open panel" }
        ];

        var script = _parser.Serialize(actions);
        Assert.Equal("# NOTE Position before the next step\r\nMOVE 420 260\r\nCALL \"Open panel\"", script);
        var parsed = _parser.Parse(script);
        Assert.True(parsed.IsValid);
        var move = Assert.IsType<MovePointerAction>(parsed.Actions[0]);
        Assert.Equal((420, 260), (move.ClientX, move.ClientY));
        Assert.Equal("Open panel", Assert.IsType<CallFunctionAction>(parsed.Actions[1]).FunctionName);
    }

    [Theory]
    [InlineData("MOVE -1 20", "Line 1: Move coordinates cannot be negative")]
    [InlineData("CALL panel", "Line 1: Function name must be enclosed in double quotes")]
    public void Parse_InvalidWorkspaceCommands_ReturnsSpecificError(string script, string expected)
    {
        var result = _parser.Parse(script);
        Assert.False(result.IsValid);
        Assert.Equal(expected, result.Errors.Single().ToString());
    }

    [Fact]
    public void ImageActions_RoundTripEmbeddedTemplateAndSettingsThroughDsl()
    {
        var template = new byte[] { 137, 80, 78, 71, 1, 2, 3, 4 };
        AutomationAction[] actions =
        [
            new WaitForImageAction { TemplateName = "Play button.png", TemplatePng = template, SimilarityPercent = 91, TimeoutMilliseconds = 7000, PollIntervalMilliseconds = 125, RegionX = 10, RegionY = 20, RegionWidth = 300, RegionHeight = 200, WaitForDisappear = true },
            new ClickImageAction { TemplateName = "Confirm.png", TemplatePng = template, SimilarityPercent = 87, TimeoutMilliseconds = 4000, PollIntervalMilliseconds = 200, RightClick = true, OffsetX = -3, OffsetY = 6 }
        ];

        var parsed = _parser.Parse(_parser.Serialize(actions));

        Assert.True(parsed.IsValid, string.Join(Environment.NewLine, parsed.Errors));
        var wait = Assert.IsType<WaitForImageAction>(parsed.Actions[0]);
        Assert.Equal(("Play button.png", 91, 7000, 125, 10, 20, 300, 200, true), (wait.TemplateName, wait.SimilarityPercent, wait.TimeoutMilliseconds, wait.PollIntervalMilliseconds, wait.RegionX, wait.RegionY, wait.RegionWidth, wait.RegionHeight, wait.WaitForDisappear));
        Assert.Equal(template, wait.TemplatePng);
        var click = Assert.IsType<ClickImageAction>(parsed.Actions[1]);
        Assert.Equal((true, -3, 6, 87), (click.RightClick, click.OffsetX, click.OffsetY, click.SimilarityPercent));
        Assert.Equal(template, click.TemplatePng);
    }

    [Fact]
    public void ColorActions_RoundTripHexRgbAndScanSettingsThroughDsl()
    {
        AutomationAction[] actions =
        [
            new WaitForColorAction { ColorHex = "#12ABEF", Tolerance = 24, MinimumPixels = 12, TimeoutMilliseconds = 6000, PollIntervalMilliseconds = 90, WaitForDisappear = true, RegionX = 4, RegionY = 5, RegionWidth = 300, RegionHeight = 200 },
            new ClickColorAction { ColorHex = "#C81E42", Tolerance = 10, MinimumPixels = 20, TimeoutMilliseconds = 4500, PollIntervalMilliseconds = 110, RightClick = true, OffsetX = -2, OffsetY = 8 }
        ];

        var parsed = _parser.Parse(_parser.Serialize(actions));

        Assert.True(parsed.IsValid, string.Join(Environment.NewLine, parsed.Errors));
        var wait = Assert.IsType<WaitForColorAction>(parsed.Actions[0]);
        Assert.Equal(("#12ABEF", 24, 12, true, 4, 5, 300, 200), (wait.ColorHex, wait.Tolerance, wait.MinimumPixels, wait.WaitForDisappear, wait.RegionX, wait.RegionY, wait.RegionWidth, wait.RegionHeight));
        var click = Assert.IsType<ClickColorAction>(parsed.Actions[1]);
        Assert.Equal(("#C81E42", true, -2, 8), (click.ColorHex, click.RightClick, click.OffsetX, click.OffsetY));
    }
}
