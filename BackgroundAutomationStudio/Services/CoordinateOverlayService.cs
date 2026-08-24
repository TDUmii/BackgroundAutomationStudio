using System.Windows.Threading;
using BackgroundAutomationStudio.Models;
using BackgroundAutomationStudio.Native;
using BackgroundAutomationStudio.Views;

namespace BackgroundAutomationStudio.Services;

public sealed class CoordinateOverlayService : IDisposable
{
    private readonly IWindowManager _windowManager;
    private readonly DispatcherTimer _timer = new(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(60) };
    private ScreenCoordinateOverlayWindow? _window;
    private WindowTarget? _target;
    private IReadOnlyList<AutomationAction> _actions = [];
    private bool _enabled;
    private bool _showGrid = true;
    private string _markerColor = "#74A7FF";
    private string _markerShape = MarkerShapes.Pin;

    public CoordinateOverlayService(IWindowManager windowManager)
    {
        _windowManager = windowManager;
        _timer.Tick += (_, _) => RefreshPosition(); _timer.Start();
    }

    public void Configure(bool enabled, WindowTarget? target, IReadOnlyList<AutomationAction> actions, bool showGrid, string markerColor, string markerShape)
    {
        _enabled = enabled; _target = target; _actions = actions; _showGrid = showGrid; _markerColor = markerColor; _markerShape = markerShape;
        if (!_enabled) Hide(); else RefreshPosition();
    }

    private void RefreshPosition()
    {
        if (!_enabled || _target is null) { Hide(); return; }
        var hwnd = _windowManager.Resolve(_target);
        if (hwnd == IntPtr.Zero || NativeMethods.IsIconic(hwnd) || !NativeMethods.IsWindowVisible(hwnd)) { Hide(); return; }
        var foreground = NativeMethods.GetAncestor(NativeMethods.GetForegroundWindow(), NativeMethods.GaRoot);
        if (foreground != NativeMethods.GetAncestor(hwnd, NativeMethods.GaRoot)) { Hide(); return; }
        if (!NativeMethods.GetClientRect(hwnd, out var client) || client.Width <= 0 || client.Height <= 0) { Hide(); return; }
        var origin = new POINT(0, 0); if (!NativeMethods.ClientToScreen(hwnd, ref origin)) { Hide(); return; }
        var cursorClient = new POINT();
        var showCursorCoordinate = NativeMethods.GetCursorPos(out cursorClient) && NativeMethods.ScreenToClient(hwnd, ref cursorClient) &&
            cursorClient.X >= 0 && cursorClient.Y >= 0 && cursorClient.X < client.Width && cursorClient.Y < client.Height;
        _window ??= new ScreenCoordinateOverlayWindow();
        if (!_window.IsVisible) _window.Show();
        _window.AttachToTarget(hwnd);
        _window.UpdateOverlay(_actions, client.Width, client.Height, _showGrid, _markerColor, _markerShape, cursorClient, showCursorCoordinate);
        _window.SetPhysicalBounds(origin.X, origin.Y, client.Width, client.Height);
    }
    private void Hide() { if (_window?.IsVisible == true) _window.Hide(); }
    public void Dispose() { _timer.Stop(); _window?.Close(); _window = null; }
}
