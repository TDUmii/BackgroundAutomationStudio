using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using BackgroundAutomationStudio.Models;

namespace BackgroundAutomationStudio.Services;

public sealed class ProjectService
{
    public static JsonSerializerOptions JsonOptions { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task SaveAsync(AutomationProject project, string path, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(path))!;
        Directory.CreateDirectory(directory);
        var temp = Path.Combine(directory, $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
        try
        {
            await using (var stream = File.Create(temp))
                await JsonSerializer.SerializeAsync(stream, project, JsonOptions, cancellationToken);
            File.Move(temp, path, true);
        }
        finally
        {
            if (File.Exists(temp)) File.Delete(temp);
        }
    }

    public async Task<AutomationProject> LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(path);
        var project = await JsonSerializer.DeserializeAsync<AutomationProject>(stream, JsonOptions, cancellationToken)
            ?? throw new InvalidDataException("The project file is empty or invalid.");
        if (project.Version != 1) throw new InvalidDataException($"Project version {project.Version} is not supported by Version 1.");
        project.Actions ??= [];
        project.RepeatCount = Math.Clamp(project.RepeatCount, 1, 999);
        return project;
    }
}
