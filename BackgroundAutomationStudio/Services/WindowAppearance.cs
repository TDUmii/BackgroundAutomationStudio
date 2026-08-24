using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using BackgroundAutomationStudio.Native;

namespace BackgroundAutomationStudio.Services;

internal static class WindowAppearance
{
    private const int UseImmersiveDarkMode = 20;
    private const int UseImmersiveDarkModeBefore20H1 = 19;

    public static void EnableDarkTitleBar(Window window)
    {
        window.SourceInitialized += (_, _) =>
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            var enabled = 1;
            if (NativeMethods.DwmSetWindowAttribute(hwnd, UseImmersiveDarkMode, ref enabled, Marshal.SizeOf<int>()) != 0)
                NativeMethods.DwmSetWindowAttribute(hwnd, UseImmersiveDarkModeBefore20H1, ref enabled, Marshal.SizeOf<int>());
        };
    }
}
