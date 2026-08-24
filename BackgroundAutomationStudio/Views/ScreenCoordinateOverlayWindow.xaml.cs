using System.Windows;
using System.Windows.Interop;
using BackgroundAutomationStudio.Models;
using BackgroundAutomationStudio.Native;

namespace BackgroundAutomationStudio.Views;

public partial class ScreenCoordinateOverlayWindow : Window
{
    private IntPtr _targetOwner;

    public ScreenCoordinateOverlayWindow() => InitializeComponent();
    private void Window_SourceInitialized(object? sender, EventArgs e)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        var style = NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GwlExStyle).ToInt64();
        NativeMethods.SetWindowLongPtr(hwnd, NativeMethods.GwlExStyle, new IntPtr(style | NativeMethods.WsExTransparent | NativeMethods.WsExNoActivate | NativeMethods.WsExToolWindow));
    }
    public void UpdateOverlay(IReadOnlyList<AutomationAction> actions, int clientWidth, int clientHeight, bool showGrid, string markerColor, string markerShape, POINT cursorClient, bool showCursorCoordinate)
    {
        Overlay.Actions = actions; Overlay.ClientWidth = Math.Max(1, clientWidth); Overlay.ClientHeight = Math.Max(1, clientHeight);
        Overlay.ShowGrid = showGrid; Overlay.MarkerColor = markerColor; Overlay.MarkerShape = markerShape;
        Overlay.CursorClientX = cursorClient.X; Overlay.CursorClientY = cursorClient.Y; Overlay.ShowCursorCoordinate = showCursorCoordinate;
    }

    public void AttachToTarget(IntPtr target)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero || target == IntPtr.Zero || _targetOwner == target) return;
        NativeMethods.SetWindowLongPtr(hwnd, NativeMethods.GwlHwndParent, target);
        _targetOwner = target;
    }

    public void SetPhysicalBounds(int x, int y, int width, int height)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        NativeMethods.SetWindowPos(hwnd, IntPtr.Zero, x, y, width, height, NativeMethods.SwpNoActivate);
    }
}
