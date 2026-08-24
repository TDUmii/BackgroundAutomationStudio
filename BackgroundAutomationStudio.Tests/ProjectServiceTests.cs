using System.Text.Json;
using BackgroundAutomationStudio.Models;
using BackgroundAutomationStudio.Services;

namespace BackgroundAutomationStudio.Tests;

public sealed class ProjectServiceTests
{
    [Fact]
    public async Task SaveAndLoad_RoundTripsTargetAndPolymorphicActions()
    {
        var path = Path.Combine(Path.GetTempPath(), $"bas-{Guid.NewGuid():N}.json");
        try
        {
            var project = new AutomationProject
            {
                Name = "Acceptance",
                ShowCoordinateMap = true,
                ShowCoordinateGrid = false,
                MarkerColor = "#55D6A0",
                MarkerShape = MarkerShapes.Diamond,
                RepeatMode = RepeatModes.Duration,
                RepeatCount = 5,
                RepeatDurationMinutes = 45,
                StopAtTime = "21:30",
                Target = new WindowTarget { ProcessName = "notepad.exe", ProcessId = 42, WindowTitle = "Test.txt - Notepad", WindowTitleContains = "Notepad", WindowClassName = "Notepad", RecordedX = 200, RecordedY = 100, RecordedWidth = 1200, RecordedHeight = 700, LastKnownHwnd = 12345 },
                Actions = [new ClickAction { ClientX = 400, ClientY = 250 }, new TypeTextAction { Text = "Hello Umi" }, new KeyPressAction { KeyName = "ENTER", Enabled = false }, new KeyHoldAction { KeyName = "E", Milliseconds = 2200 }, new DragAction { StartX = 1, StartY = 2, EndX = 3, EndY = 4, Milliseconds = 500 }],
                Functions = [new AutomationFunction { Name = "Confirm", Actions = [new MovePointerAction { ClientX = 40, ClientY = 50 }, new ClickAction { ClientX = 40, ClientY = 50 }] }]
            };
            var service = new ProjectService();
            await service.SaveAsync(project, path);
            var loaded = await service.LoadAsync(path);
            Assert.Equal(1, loaded.Version);
            Assert.Equal(5, loaded.RepeatCount);
            Assert.Equal(RepeatModes.Duration, loaded.RepeatMode);
            Assert.Equal(45, loaded.RepeatDurationMinutes);
            Assert.Equal("21:30", loaded.StopAtTime);
            Assert.Equal(1200, loaded.Target!.RecordedWidth);
            Assert.IsType<ClickAction>(loaded.Actions[0]);
            Assert.Equal("Hello Umi", Assert.IsType<TypeTextAction>(loaded.Actions[1]).Text);
            Assert.False(loaded.Actions[2].Enabled);
            Assert.Equal(2200, Assert.IsType<KeyHoldAction>(loaded.Actions[3]).Milliseconds);
            Assert.Equal(4, Assert.IsType<DragAction>(loaded.Actions[4]).EndY);
            Assert.True(loaded.ShowCoordinateMap);
            Assert.False(loaded.ShowCoordinateGrid);
            Assert.Equal("#55D6A0", loaded.MarkerColor);
            Assert.Equal(MarkerShapes.Diamond, loaded.MarkerShape);
            var function = Assert.Single(loaded.Functions);
            Assert.Equal("Confirm", function.Name);
            Assert.IsType<MovePointerAction>(function.Actions[0]);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public async Task Load_NormalizesInvalidScheduleValues()
    {
        var path = Path.Combine(Path.GetTempPath(), $"bas-{Guid.NewGuid():N}.json");
        try
        {
            await File.WriteAllTextAsync(path, "{\"version\":1,\"repeatMode\":\"Other\",\"repeatDurationMinutes\":0,\"stopAtTime\":\"29:90\",\"actions\":[]}");
            var loaded = await new ProjectService().LoadAsync(path);
            Assert.Equal(RepeatModes.Count, loaded.RepeatMode);
            Assert.Equal(1, loaded.RepeatDurationMinutes);
            Assert.Equal("23:00", loaded.StopAtTime);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public async Task Load_ClampsInvalidRepeatCount()
    {
        var path = Path.Combine(Path.GetTempPath(), $"bas-{Guid.NewGuid():N}.json");
        try
        {
            await File.WriteAllTextAsync(path, "{\"version\":1,\"name\":\"Repeat\",\"repeatCount\":0,\"actions\":[]}");
            var loaded = await new ProjectService().LoadAsync(path);
            Assert.Equal(1, loaded.RepeatCount);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public async Task Load_AllowsLongGameMacroRepeatCount()
    {
        var path = Path.Combine(Path.GetTempPath(), $"bas-{Guid.NewGuid():N}.json");
        try
        {
            await File.WriteAllTextAsync(path, "{\"version\":1,\"name\":\"Long macro\",\"repeatCount\":250000,\"actions\":[]}");
            var loaded = await new ProjectService().LoadAsync(path);
            Assert.Equal(250000, loaded.RepeatCount);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public async Task Load_RejectsFutureProjectVersion()
    {
        var path = Path.Combine(Path.GetTempPath(), $"bas-{Guid.NewGuid():N}.json");
        try { await File.WriteAllTextAsync(path, "{\"version\":2,\"name\":\"Future\",\"actions\":[]}"); await Assert.ThrowsAsync<InvalidDataException>(() => new ProjectService().LoadAsync(path)); }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public void Json_ContainsStableTypeDiscriminator()
    {
        var json = JsonSerializer.Serialize<AutomationAction>(new DoubleClickAction { ClientX = 10, ClientY = 20 }, ProjectService.JsonOptions);
        Assert.Contains("\"$type\": \"doubleClick\"", json);
    }

    [Fact]
    public async Task Load_NormalizesInvalidMarkerAppearance()
    {
        var path = Path.Combine(Path.GetTempPath(), $"bas-{Guid.NewGuid():N}.json");
        try
        {
            await File.WriteAllTextAsync(path, "{\"version\":1,\"markerColor\":\"not-a-color\",\"markerShape\":\"Triangle\",\"actions\":[]}");
            var loaded = await new ProjectService().LoadAsync(path);
            Assert.Equal("#74A7FF", loaded.MarkerColor);
            Assert.Equal(MarkerShapes.Pin, loaded.MarkerShape);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }
}
