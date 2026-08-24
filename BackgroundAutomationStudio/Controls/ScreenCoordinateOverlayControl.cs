using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using BackgroundAutomationStudio.Models;
using WorkflowDragAction = BackgroundAutomationStudio.Models.DragAction;

namespace BackgroundAutomationStudio.Controls;

public sealed class ScreenCoordinateOverlayControl : FrameworkElement
{
    private INotifyCollectionChanged? _observedCollection;
    public static readonly DependencyProperty ActionsProperty = DependencyProperty.Register(nameof(Actions), typeof(IEnumerable<AutomationAction>), typeof(ScreenCoordinateOverlayControl), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, ActionsChanged));
    public static readonly DependencyProperty ClientWidthProperty = DependencyProperty.Register(nameof(ClientWidth), typeof(int), typeof(ScreenCoordinateOverlayControl), new FrameworkPropertyMetadata(1, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty ClientHeightProperty = DependencyProperty.Register(nameof(ClientHeight), typeof(int), typeof(ScreenCoordinateOverlayControl), new FrameworkPropertyMetadata(1, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty ShowGridProperty = DependencyProperty.Register(nameof(ShowGrid), typeof(bool), typeof(ScreenCoordinateOverlayControl), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty MarkerColorProperty = DependencyProperty.Register(nameof(MarkerColor), typeof(string), typeof(ScreenCoordinateOverlayControl), new FrameworkPropertyMetadata("#74A7FF", FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty MarkerShapeProperty = DependencyProperty.Register(nameof(MarkerShape), typeof(string), typeof(ScreenCoordinateOverlayControl), new FrameworkPropertyMetadata(MarkerShapes.Pin, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty CursorClientXProperty = DependencyProperty.Register(nameof(CursorClientX), typeof(int), typeof(ScreenCoordinateOverlayControl), new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty CursorClientYProperty = DependencyProperty.Register(nameof(CursorClientY), typeof(int), typeof(ScreenCoordinateOverlayControl), new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty ShowCursorCoordinateProperty = DependencyProperty.Register(nameof(ShowCursorCoordinate), typeof(bool), typeof(ScreenCoordinateOverlayControl), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

    public IEnumerable<AutomationAction>? Actions { get => (IEnumerable<AutomationAction>?)GetValue(ActionsProperty); set => SetValue(ActionsProperty, value); }
    public int ClientWidth { get => (int)GetValue(ClientWidthProperty); set => SetValue(ClientWidthProperty, value); }
    public int ClientHeight { get => (int)GetValue(ClientHeightProperty); set => SetValue(ClientHeightProperty, value); }
    public bool ShowGrid { get => (bool)GetValue(ShowGridProperty); set => SetValue(ShowGridProperty, value); }
    public string MarkerColor { get => (string)GetValue(MarkerColorProperty); set => SetValue(MarkerColorProperty, value); }
    public string MarkerShape { get => (string)GetValue(MarkerShapeProperty); set => SetValue(MarkerShapeProperty, value); }
    public int CursorClientX { get => (int)GetValue(CursorClientXProperty); set => SetValue(CursorClientXProperty, value); }
    public int CursorClientY { get => (int)GetValue(CursorClientYProperty); set => SetValue(CursorClientYProperty, value); }
    public bool ShowCursorCoordinate { get => (bool)GetValue(ShowCursorCoordinateProperty); set => SetValue(ShowCursorCoordinateProperty, value); }

    private static void ActionsChanged(DependencyObject source, DependencyPropertyChangedEventArgs e) => ((ScreenCoordinateOverlayControl)source).ObserveActions(e.OldValue as IEnumerable<AutomationAction>, e.NewValue as IEnumerable<AutomationAction>);
    private void ObserveActions(IEnumerable<AutomationAction>? oldActions, IEnumerable<AutomationAction>? newActions)
    {
        if (_observedCollection is not null) _observedCollection.CollectionChanged -= CollectionChanged;
        if (oldActions is not null) foreach (var action in oldActions) action.PropertyChanged -= ActionChanged;
        if (newActions is not null) foreach (var action in newActions) action.PropertyChanged += ActionChanged;
        _observedCollection = newActions as INotifyCollectionChanged;
        if (_observedCollection is not null) _observedCollection.CollectionChanged += CollectionChanged;
        InvalidateVisual();
    }
    private void CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null) foreach (AutomationAction action in e.OldItems) action.PropertyChanged -= ActionChanged;
        if (e.NewItems is not null) foreach (AutomationAction action in e.NewItems) action.PropertyChanged += ActionChanged;
        InvalidateVisual();
    }
    private void ActionChanged(object? sender, PropertyChangedEventArgs e) => InvalidateVisual();

    protected override void OnRender(DrawingContext drawing)
    {
        base.OnRender(drawing);
        if (ActualWidth <= 1 || ActualHeight <= 1) return;
        var bounds = new Rect(0, 0, ActualWidth, ActualHeight);
        if (ShowGrid) DrawGrid(drawing, bounds);
        drawing.DrawRectangle(null, new Pen(new SolidColorBrush(Color.FromArgb(190, 116, 167, 255)), 1.5), new Rect(.75, .75, Math.Max(0, ActualWidth - 1.5), Math.Max(0, ActualHeight - 1.5)));
        DrawMarkers(drawing, bounds);
        if (ShowCursorCoordinate) DrawCursorCoordinate(drawing, bounds);
    }

    private void DrawGrid(DrawingContext drawing, Rect bounds)
    {
        var minor = new Pen(new SolidColorBrush(Color.FromArgb(55, 158, 192, 255)), 1);
        var major = new Pen(new SolidColorBrush(Color.FromArgb(120, 158, 192, 255)), 1);
        for (var x = 0; x <= ClientWidth; x += 50)
        {
            var screenX = x / (double)Math.Max(1, ClientWidth) * bounds.Width;
            drawing.DrawLine(x % 100 == 0 ? major : minor, new Point(screenX, 0), new Point(screenX, bounds.Height));
            if (x % 100 == 0) DrawCoordinateLabel(drawing, x.ToString(CultureInfo.InvariantCulture), new Point(screenX + 3, 3), bounds);
        }
        for (var y = 0; y <= ClientHeight; y += 50)
        {
            var screenY = y / (double)Math.Max(1, ClientHeight) * bounds.Height;
            drawing.DrawLine(y % 100 == 0 ? major : minor, new Point(0, screenY), new Point(bounds.Width, screenY));
            if (y > 0 && y % 100 == 0) DrawCoordinateLabel(drawing, y.ToString(CultureInfo.InvariantCulture), new Point(3, screenY + 3), bounds);
        }
    }

    private void DrawCoordinateLabel(DrawingContext drawing, string text, Point origin, Rect bounds)
    {
        var formatted = MakeText(text, Brushes.White, 10);
        var labelX = Math.Clamp(origin.X, 2, Math.Max(2, bounds.Width - formatted.Width - 5));
        var labelY = Math.Clamp(origin.Y, 1, Math.Max(1, bounds.Height - formatted.Height - 3));
        var box = new Rect(labelX - 2, labelY - 1, formatted.Width + 6, formatted.Height + 3);
        drawing.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb(205, 12, 14, 17)), null, box, 3, 3);
        drawing.DrawText(formatted, new Point(labelX + 1, labelY));
    }

    private void DrawMarkers(DrawingContext drawing, Rect bounds)
    {
        var color = ParseColor(MarkerColor);
        var index = 1;
        foreach (var action in Actions ?? [])
        {
            var alpha = action.Enabled ? (byte)235 : (byte)105;
            var fill = new SolidColorBrush(Color.FromArgb(alpha, color.R, color.G, color.B));
            var outline = new Pen(new SolidColorBrush(Color.FromArgb(alpha, 255, 255, 255)), 1.2);
            switch (action)
            {
                case PointerAction point:
                    DrawMarker(drawing, Map(bounds, point.ClientX, point.ClientY), index++, fill, outline);
                    break;
                case WorkflowDragAction drag:
                    var start = Map(bounds, drag.StartX, drag.StartY); var end = Map(bounds, drag.EndX, drag.EndY);
                    drawing.DrawLine(new Pen(fill, 2.5), start, end);
                    DrawMarker(drawing, start, index, fill, outline); DrawMarker(drawing, end, index++, fill, outline);
                    break;
            }
        }
    }

    private void DrawCursorCoordinate(DrawingContext drawing, Rect bounds)
    {
        var point = Map(bounds, CursorClientX, CursorClientY);
        var text = MakeText($"{CursorClientX} | {CursorClientY}", Brushes.White, 11);
        const double gap = 14;
        const double paddingX = 7;
        const double paddingY = 4;
        var width = text.Width + paddingX * 2;
        var height = text.Height + paddingY * 2;
        var left = point.X + gap;
        var top = point.Y + gap;
        if (left + width > bounds.Right - 4) left = point.X - gap - width;
        if (top + height > bounds.Bottom - 4) top = point.Y - gap - height;
        left = Math.Clamp(left, bounds.Left + 4, Math.Max(bounds.Left + 4, bounds.Right - width - 4));
        top = Math.Clamp(top, bounds.Top + 4, Math.Max(bounds.Top + 4, bounds.Bottom - height - 4));
        var backplate = new Rect(left, top, width, height);
        drawing.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb(232, 12, 14, 17)), new Pen(new SolidColorBrush(Color.FromArgb(220, 116, 167, 255)), 1), backplate, 5, 5);
        drawing.DrawText(text, new Point(left + paddingX, top + paddingY));
    }

    private void DrawMarker(DrawingContext drawing, Point point, int index, Brush fill, Pen outline)
    {
        switch (MarkerShapes.Normalize(MarkerShape))
        {
            case MarkerShapes.Diamond:
                var diamond = new StreamGeometry();
                using (var context = diamond.Open()) { context.BeginFigure(new Point(point.X, point.Y - 10), true, true); context.LineTo(new Point(point.X + 10, point.Y), true, false); context.LineTo(new Point(point.X, point.Y + 10), true, false); context.LineTo(new Point(point.X - 10, point.Y), true, false); }
                diamond.Freeze(); drawing.DrawGeometry(fill, outline, diamond); break;
            case MarkerShapes.Crosshair:
                drawing.DrawEllipse(new SolidColorBrush(Color.FromArgb(45, 255, 255, 255)), new Pen(fill, 2.5), point, 10, 10);
                drawing.DrawLine(new Pen(fill, 2), new Point(point.X - 14, point.Y), new Point(point.X + 14, point.Y));
                drawing.DrawLine(new Pen(fill, 2), new Point(point.X, point.Y - 14), new Point(point.X, point.Y + 14)); break;
            default:
                var pin = new StreamGeometry();
                using (var context = pin.Open()) { context.BeginFigure(point, true, true); context.BezierTo(new Point(point.X - 3, point.Y - 4), new Point(point.X - 9, point.Y - 9), new Point(point.X - 9, point.Y - 14), true, false); context.ArcTo(new Point(point.X + 9, point.Y - 14), new Size(9, 9), 0, false, SweepDirection.Clockwise, true, false); context.BezierTo(new Point(point.X + 9, point.Y - 9), new Point(point.X + 3, point.Y - 4), point, true, false); }
                pin.Freeze(); drawing.DrawGeometry(fill, outline, pin); break;
        }
        var label = MakeText(index.ToString(CultureInfo.InvariantCulture), Brushes.White, 9);
        var labelY = MarkerShapes.Normalize(MarkerShape) == MarkerShapes.Pin ? point.Y - 19 : point.Y - label.Height / 2;
        drawing.DrawText(label, new Point(point.X - label.Width / 2, labelY));
    }

    private Point Map(Rect bounds, int x, int y) => new(Math.Clamp(x, 0, Math.Max(1, ClientWidth)) / (double)Math.Max(1, ClientWidth) * bounds.Width, Math.Clamp(y, 0, Math.Max(1, ClientHeight)) / (double)Math.Max(1, ClientHeight) * bounds.Height);
    private static Color ParseColor(string? value) { try { return (Color)ColorConverter.ConvertFromString(value ?? "#74A7FF"); } catch { return Color.FromRgb(116, 167, 255); } }
    private FormattedText MakeText(string text, Brush brush, double size) => new(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), size, brush, VisualTreeHelper.GetDpi(this).PixelsPerDip);
}
