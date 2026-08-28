using System.Windows;
using System.Windows.Interop;

namespace BackgroundAutomationStudio.MiniCore;

public static class MiniWindowAppearance
{
    public static void EnableDarkTitleBar(Window window) => window.SourceInitialized += (_, _) =>
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        var enabled = 1;
        _ = MiniNative.DwmSetWindowAttribute(hwnd, 20, ref enabled, sizeof(int));
        var rounded = 2;
        _ = MiniNative.DwmSetWindowAttribute(hwnd, 33, ref rounded, sizeof(int));
    };
}
