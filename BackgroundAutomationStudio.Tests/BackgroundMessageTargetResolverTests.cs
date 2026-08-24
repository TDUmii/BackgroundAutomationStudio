using System.Runtime.InteropServices;
using BackgroundAutomationStudio.Native;
using BackgroundAutomationStudio.Services;

namespace BackgroundAutomationStudio.Tests;

public sealed class BackgroundMessageTargetResolverTests
{
    [Fact]
    public void Resolve_FindsTargetChild_WhenAnotherTopLevelWindowCoversTheDesktopPoint()
    {
        var parent = TestNative.CreateWindowEx(0, "STATIC", "resolver-parent", TestNative.WsPopup | TestNative.WsVisible,
            120, 120, 240, 180, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
        Assert.NotEqual(IntPtr.Zero, parent);
        var child = TestNative.CreateWindowEx(0, "BUTTON", "target", TestNative.WsChild | TestNative.WsVisible,
            20, 24, 100, 42, parent, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
        Assert.NotEqual(IntPtr.Zero, child);
        var cover = TestNative.CreateWindowEx(TestNative.WsExTopmost, "BUTTON", "resolver-cover", TestNative.WsPopup | TestNative.WsVisible,
            120, 120, 240, 180, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
        Assert.NotEqual(IntPtr.Zero, cover);
        Assert.True(NativeMethods.SetWindowPos(cover, new IntPtr(-1), 0, 0, 0, 0,
            NativeMethods.SwpNoMove | NativeMethods.SwpNoSize | NativeMethods.SwpNoActivate));

        try
        {
            var desktopWindow = NativeMethods.WindowFromPoint(new POINT(150, 154));
            Assert.NotEqual(child, desktopWindow);

            var resolved = BackgroundMessageTargetResolver.Resolve(parent, 30, 34);

            Assert.Equal(child, resolved.Hwnd);
            Assert.Equal(10, resolved.ClientPoint.X);
            Assert.Equal(10, resolved.ClientPoint.Y);
        }
        finally
        {
            TestNative.DestroyWindow(cover);
            TestNative.DestroyWindow(parent);
        }
    }

    private static class TestNative
    {
        public const uint WsPopup = 0x80000000;
        public const uint WsChild = 0x40000000;
        public const uint WsVisible = 0x10000000;
        public const uint WsExTopmost = 0x00000008;

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr CreateWindowEx(uint extendedStyle, string className, string windowName, uint style,
            int x, int y, int width, int height, IntPtr parent, IntPtr menu, IntPtr instance, IntPtr parameter);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DestroyWindow(IntPtr hwnd);
    }
}
