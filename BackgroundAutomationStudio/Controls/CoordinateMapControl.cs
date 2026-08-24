using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using BackgroundAutomationStudio.Models;
using WorkflowDragAction = BackgroundAutomationStudio.Models.DragAction;

namespace BackgroundAutomationStudio.Controls;

public sealed class CoordinateMapControl : FrameworkElement
{
    private INotifyCollectionChanged? _observedCollection;
    private Point? _hover;

    public static readonly DependencyProperty ActionsProperty = DependencyProperty.Register(nameof(Actions), typeof(IEnumerable<AutomationAction>), typeof(CoordinateMapControl), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, ActionsChanged));
    public static readonly DependencyProperty ClientWidthProperty = DependencyProperty.Register(nameof(ClientWidth), typeof(int), typeof(CoordinateMapControl), new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty ClientHeightProperty = DependencyProperty.Register(nameof(ClientHeight), typeof(int), typeof(CoordinateMapControl), new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty ShowGridProperty = DependencyProperty.Register(nameof(ShowGrid), typeof(bool), typeof(CoordinateMapControl), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty MarkerColorProperty = DependencyProperty.Register(nameof(MarkerColor), typeof(string), typeof(CoordinateMapControl), new FrameworkPropertyMetadata("#74A7FF", FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty MarkerShapeProperty = DependencyProperty.Register(nameof(MarkerShape), typeof(string), typeof(CoordinateMapControl), new FrameworkPropertyMetadata(MarkerShapes.Pin, FrameworkPropertyMetadataOptions.AffectsRender));

    public IEnumerable<AutomationAction>? Actions { get => (IEnumerable<AutomationAction>?)GetValue(ActionsProperty); set => SetValue(ActionsProperty, value); }
    public int ClientWidth { get => (int)GetValue(ClientWidthProperty); set => SetValue(ClientWidthProperty, value); }
    public int ClientHeight { get => (int)GetValue(ClientHeightProperty); set => SetValue(ClientHeightProperty, value); }
    public bool ShowGrid { get => (bool)GetValue(ShowGridProperty); set => SetValue(ShowGridProperty, value); }
    public string MarkerColor { get => (string)GetValue(MarkerColorProperty); set => SetValue(MarkerColorProperty, value); }
    public string MarkerShape { get => (string)GetValue(MarkerShapeProperty); set => SetValue(MarkerShapeProperty, value); }

    public CoordinateMapControl()
    {
        MinHeight = 210;
        Cursor = Cursors.Cross;
        MouseMove += (_, e) => { _hover = e.GetPosition(this); InvalidateVisual(); };
        MouseLeave += (_, _) => { _hover = null; InvalidateVisual(); };
    }

    private static void ActionsChanged(DependencyObject source, DependencyPropertyChangedEventArgs e) => ((CoordinateMapControl)source).ObserveActions(e.OldValue as IEnumerable<AutomationAction>, e.NewValue as IEnumerable<AutomationAction>);

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
        var bounds = new Rect(0, 0, ActualWidth, ActualHeight);
        drawing.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(18, 21, 26)), new Pen(new SolidColorBrush(Color.FromRgb(48, 54, 65)), 1), bounds, 10, 10);
        var plot = new Rect(34, 20, Math.Max(1, ActualWidth - 54), Math.Max(1, ActualHeight - 44));
        var logicalWidth = ClientWidth > 0 ? ClientWidth : Math.Max(800, MaxX() + 100);
        var logicalHeight = ClientHeight > 0 ? ClientHeight : Math.Max(500, MaxY() + 100);
        if (ShowGrid) DrawGrid(drawing, plot, logicalWidth, logicalHeight);
        DrawMarkers(drawing, plot, logicalWidth, logicalHeight);
        DrawFrameLabels(drawing, plot, logicalWidth, logicalHeight);
        if (_hover is { } hover && plot.Contains(hover)) DrawHover(drawing, plot, logicalWidth, logicalHeight, hover);
    }

    private void DrawGrid(DrawingContext drawing, Rect plot, int logicalWidth, int logicalHeight)
    {
        var minor = new Pen(new SolidColorBrush(Color.FromArgb(90, 48, 54, 65)), 1);
        var major = new Pen(new SolidColorBrush(Color.FromArgb(150, 67, 76, 91)), 1);
        for (var index = 0; index <= 10; index++)
        {
            var x = plot.Left + plot.Width * index / 10d;
            var y = plot.Top + plot.Height * index / 10d;
            drawing.DrawLine(index % 5 == 0 ? major : minor, new Point(x, plot.Top), new Point(x, plot.Bottom));
            drawing.DrawLine(index % 5 == 0 ? major : minor, new Point(plot.Left, y), new Point(plot.Right, y));
        }
    }

    private void DrawFrameLabels(DrawingContext drawing, Rect plot, int logicalWidth, int logicalHeight)
    {
        var brush = new SolidColorBrush(Color.FromRgb(135, 146, 163));
        DrawText(drawing, "0,0", new Point(8, 6), brush, 10);
        DrawText(drawing, logicalWidth.ToString(CultureInfo.InvariantCulture), new Point(plot.Right - 22, plot.Bottom + 4), brush, 10);
        DrawText(drawing, logicalHeight.ToString(CultureInfo.InvariantCulture), new Point(5, plot.Bottom - 7), brush, 10);
    }

    private void DrawMarkers(DrawingContext drawing, Rect plot, int logicalWidth, int logicalHeight)
    {
        var color = ParseColor(MarkerColor);
        var fill = new SolidColorBrush(Color.FromArgb(220, color.R, color.G, color.B));
        var outline = new Pen(new SolidColorBrush(Color.FromRgb(243, 245, 248)), 1.2);
        var index = 1;
        foreach (var action in Actions ?? [])
        {
            switch (action)
            {
                case PointerAction point:
                    DrawMarker(drawing, ToPlot(plot, logicalWidth, logicalHeight, point.ClientX, point.ClientY), index++, fill, outline);
                    break;
                case WorkflowDragAction drag:
                    var start = ToPlot(plot, logicalWidth, logicalHeight, drag.StartX, drag.StartY);
                    var end = ToPlot(plot, logicalWidth, logicalHeight, drag.EndX, drag.EndY);
                    drawing.DrawLine(new Pen(fill, 2), start, end);
                    DrawMarker(drawing, start, index, fill, outline);
                    DrawMarker(drawing, end, index++, fill, outline);
                    break;
            }
        }
    }

    private void DrawMarker(DrawingContext drawing, Point point, int index, Brush fill, Pen outline)
    {
        switch (MarkerShapes.Normalize(MarkerShape))
        {
            case MarkerShapes.Diamond:
                var diamond = new StreamGeometry();
                using (var context = diamond.Open()) { context.BeginFigure(new Point(point.X, point.Y - 9), true, true); context.LineTo(new Point(point.X + 9, point.Y), true, false); context.LineTo(new Point(point.X, point.Y + 9), true, false); context.LineTo(new Point(point.X - 9, point.Y), true, false); }
                diamond.Freeze(); drawing.DrawGeometry(fill, outline, diamond); break;
            case MarkerShapes.Crosshair:
                drawing.DrawEllipse(new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), new Pen(fill, 2), point, 9, 9);
                drawing.DrawLine(new Pen(fill, 2), new Point(point.X - 12, point.Y), new Point(point.X + 12, point.Y));
                drawing.DrawLine(new Pen(fill, 2), new Point(point.X, point.Y - 12), new Point(point.X, point.Y + 12)); break;
            default:
                var pin = new StreamGeometry();
                using (var context = pin.Open())
                {
                    context.BeginFigure(point, true, true);
                    context.BezierTo(new Point(point.X - 3, point.Y - 4), new Point(point.X - 8, point.Y - 8), new Point(point.X - 8, point.Y - 13), true, false);
                    context.ArcTo(new Point(point.X + 8, point.Y - 13), new Size(8, 8), 0, false, SweepDirection.Clockwise, true, false);
                    context.BezierTo(new Point(point.X + 8, point.Y - 8), new Point(point.X + 3, point.Y - 4), point, true, false);
                }
                pin.Freeze(); drawing.DrawGeometry(fill, outline, pin); break;
        }
        var label = new FormattedText(index.ToString(CultureInfo.InvariantCulture), CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), 9, Brushes.White, VisualTreeHelper.GetDpi(this).PixelsPerDip);
        var labelY = MarkerShapes.Normalize(MarkerShape) == MarkerShapes.Pin ? point.Y - 18 : point.Y - label.Height / 2;
        drawing.DrawText(label, new Point(point.X - label.Width / 2, labelY));
    }

    private void DrawHover(DrawingContext drawing, Rect plot, int logicalWidth, int logicalHeight, Point hover)
    {
        var accent = new SolidColorBrush(Color.FromArgb(160, 158, 192, 255));
        var pen = new Pen(accent, 1) { DashStyle = DashStyles.Dash };
        drawing.DrawLine(pen, new Point(hover.X, plot.Top), new Point(hover.X, plot.Bottom));
        drawing.DrawLine(pen, new Point(plot.Left, hover.Y), new Point(plot.Right, hover.Y));
        var x = (int)Math.Round((hover.X - plot.Left) / plot.Width * logicalWidth);
        var y = (int)Math.Round((hover.Y - plot.Top) / plot.Height * logicalHeight);
        var text = $"X {Math.Clamp(x, 0, logicalWidth)}  Y {Math.Clamp(y, 0, logicalHeight)}";
        var formatted = MakeText(text, Brushes.White, 11);
        var left = Math.Min(plot.Right - formatted.Width - 16, hover.X + 10);
        var top = Math.Max(plot.Top + 4, hover.Y - 30);
        drawing.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb(245, 29, 33, 40)), new Pen(new SolidColorBrush(Color.FromRgb(90, 143, 228)), 1), new Rect(left, top, formatted.Width + 12, formatted.Height + 8), 5, 5);
        drawing.DrawText(formatted, new Point(left + 6, top + 4));
    }

    private Point ToPlot(Rect plot, int logicalWidth, int logicalHeight, int x, int y) => new(plot.Left + Math.Clamp(x, 0, logicalWidth) / (double)logicalWidth * plot.Width, plot.Top + Math.Clamp(y, 0, logicalHeight) / (double)logicalHeight * plot.Height);
    private int MaxX() => (Actions ?? []).SelectMany(Points).Select(point => point.X).DefaultIfEmpty(0).Max();
    private int MaxY() => (Actions ?? []).SelectMany(Points).Select(point => point.Y).DefaultIfEmpty(0).Max();
    private static IEnumerable<(int X, int Y)> Points(AutomationAction action) => action switch { PointerAction p => [(p.ClientX, p.ClientY)], WorkflowDragAction d => [(d.StartX, d.StartY), (d.EndX, d.EndY)], _ => [] };
    private static Color ParseColor(string? value) { try { return (Color)ColorConverter.ConvertFromString(value ?? "#74A7FF"); } catch { return Color.FromRgb(116, 167, 255); } }
    private FormattedText MakeText(string text, Brush brush, double size) => new(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), size, brush, VisualTreeHelper.GetDpi(this).PixelsPerDip);
    private void DrawText(DrawingContext drawing, string text, Point point, Brush brush, double size) => drawing.DrawText(MakeText(text, brush, size), point);
}
