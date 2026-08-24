namespace BackgroundAutomationStudio.Models;

public sealed class AppSettings
{
    public string Language { get; set; } = "en";
    public string RunHotkey { get; set; } = "CTRL+SHIFT+F9";
    public string PauseHotkey { get; set; } = "CTRL+SHIFT+F10";
    public string PlaybackMode { get; set; } = PlaybackModes.Automatic;
    public bool AlwaysOnTop { get; set; }
}

public static class PlaybackModes
{
    public const string Automatic = "Automatic";
    public const string UiAutomation = "UiAutomationUnsafe";
    public const string Win32Messages = "Win32Messages";
    public const string GameForeground = "GameForeground";
    public const string GameBackground = "GameBackgroundExperimental";

    public static bool IsGame(string? value) => Normalize(value) is GameForeground or GameBackground;

    public static int GetIndex(string? value) => Normalize(value) switch
    {
        GameForeground => 2,
        GameBackground => 3,
        UiAutomation => 4,
        Win32Messages => 5,
        _ => 1
    };

    public static string GetResourceKey(string? value) => Normalize(value) switch
    {
        GameForeground => "GameForegroundModeName",
        GameBackground => "GameBackgroundModeName",
        UiAutomation => "UiAutomationModeName",
        Win32Messages => "Win32ModeName",
        _ => "AutomaticModeName"
    };

    public static string Normalize(string? value) => value switch
    {
        UiAutomation => UiAutomation,
        Win32Messages => Win32Messages,
        GameForeground => GameForeground,
        GameBackground => GameBackground,
        _ => Automatic
    };
}
