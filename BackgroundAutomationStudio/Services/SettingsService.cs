using System.IO;
using System.Text.Json;
using BackgroundAutomationStudio.Models;

namespace BackgroundAutomationStudio.Services;

public sealed class SettingsService
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    private readonly string _path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BackgroundAutomationStudio", "settings.json");

    public AppSettings Current { get; private set; } = new();

    public void Load()
    {
        try
        {
            if (File.Exists(_path)) Current = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_path), Options) ?? new();
        }
        catch { Current = new(); }
        Current.Language = Current.Language.Equals("vi", StringComparison.OrdinalIgnoreCase) ? "vi" : "en";
        if (!HotkeyParser.TryParse(Current.RunHotkey, out _, out _)) Current.RunHotkey = "CTRL+SHIFT+F9";
        if (!HotkeyParser.TryParse(Current.PauseHotkey, out _, out _) || string.Equals(Current.RunHotkey, Current.PauseHotkey, StringComparison.OrdinalIgnoreCase)) Current.PauseHotkey = "CTRL+SHIFT+F10";
        Current.PlaybackMode = PlaybackModes.Normalize(Current.PlaybackMode);
    }

    public void Save(AppSettings settings)
    {
        settings.PlaybackMode = PlaybackModes.Normalize(settings.PlaybackMode);
        if (!HotkeyParser.TryParse(settings.PauseHotkey, out _, out _) || string.Equals(settings.RunHotkey, settings.PauseHotkey, StringComparison.OrdinalIgnoreCase)) settings.PauseHotkey = "CTRL+SHIFT+F10";
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.WriteAllText(_path, JsonSerializer.Serialize(settings, Options));
        Current = settings;
    }
}
