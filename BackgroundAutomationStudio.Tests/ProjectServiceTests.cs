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
                RepeatCount = 5,
                Target = new WindowTarget { ProcessName = "notepad.exe", ProcessId = 42, WindowTitle = "Test.txt - Notepad", WindowTitleContains = "Notepad", WindowClassName = "Notepad", RecordedX = 200, RecordedY = 100, RecordedWidth = 1200, RecordedHeight = 700, LastKnownHwnd = 12345 },
                Actions = [new ClickAction { ClientX = 400, ClientY = 250 }, new TypeTextAction { Text = "Hello Umi" }, new KeyPressAction { KeyName = "ENTER", Enabled = false }]
            };
            var service = new ProjectService();
            await service.SaveAsync(project, path);
            var loaded = await service.LoadAsync(path);
            Assert.Equal(1, loaded.Version);
            Assert.Equal(5, loaded.RepeatCount);
            Assert.Equal(1200, loaded.Target!.RecordedWidth);
            Assert.IsType<ClickAction>(loaded.Actions[0]);
            Assert.Equal("Hello Umi", Assert.IsType<TypeTextAction>(loaded.Actions[1]).Text);
            Assert.False(loaded.Actions[2].Enabled);
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
}
