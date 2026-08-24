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
        project.Functions ??= [];
        var functionIds = new HashSet<Guid>();
        for (var index = 0; index < project.Functions.Count; index++)
        {
            var function = project.Functions[index];
            if (function.Id == Guid.Empty || !functionIds.Add(function.Id)) { function.Id = Guid.NewGuid(); functionIds.Add(function.Id); }
            if (string.IsNullOrWhiteSpace(function.Name)) function.Name = $"Function {index + 1}";
            function.Actions ??= [];
        }
        project.MarkerShape = MarkerShapes.Normalize(project.MarkerShape);
        if (!System.Text.RegularExpressions.Regex.IsMatch(project.MarkerColor ?? string.Empty, "^#[0-9A-Fa-f]{6}$")) project.MarkerColor = "#74A7FF";
        project.RepeatMode = RepeatModes.Normalize(project.RepeatMode);
        project.RepeatCount = Math.Clamp(project.RepeatCount, 1, 1_000_000);
        project.RepeatDurationMinutes = Math.Clamp(project.RepeatDurationMinutes, 1, 10080);
        if (!TimeSpan.TryParse(project.StopAtTime, out var stopAt) || stopAt < TimeSpan.Zero || stopAt >= TimeSpan.FromDays(1)) project.StopAtTime = "23:00";
        return project;
    }
}
