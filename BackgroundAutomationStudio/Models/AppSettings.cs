namespace BackgroundAutomationStudio.Models;

public sealed class AppSettings
{
    public string Language { get; set; } = "en";
    public string RunHotkey { get; set; } = "CTRL+SHIFT+F9";
    public string PlaybackMode { get; set; } = PlaybackModes.Automatic;
}

public static class PlaybackModes
{
    public const string Automatic = "Automatic";
    public const string UiAutomation = "UiAutomation";
    public const string Win32Messages = "Win32Messages";

    public static string Normalize(string? value) => value switch
    {
        UiAutomation => UiAutomation,
        Win32Messages => Win32Messages,
        _ => Automatic
    };
}
