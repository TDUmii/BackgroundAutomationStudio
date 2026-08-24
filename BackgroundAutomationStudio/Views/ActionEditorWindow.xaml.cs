using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using BackgroundAutomationStudio.Models;
using BackgroundAutomationStudio.Native;
using BackgroundAutomationStudio.Services;
using Microsoft.Win32;
using System.IO;

namespace BackgroundAutomationStudio.Views;

public partial class ActionEditorWindow : Window
{
    private readonly WindowTarget? _target;
    private readonly WindowPickerService _picker;
    private readonly IWindowManager _windowManager;
    public AutomationAction Action { get; }
    public ActionEditorWindow(AutomationAction action, WindowTarget? target, WindowPickerService picker, IWindowManager windowManager)
    {
        InitializeComponent(); WindowAppearance.EnableDarkTitleBar(this); Action = action; _target = target; _picker = picker; _windowManager = windowManager; DataContext = Action;
    }
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var invalidField = ValidateNumericFields();
        if (invalidField is not null)
        {
            invalidField.Focus(); invalidField.SelectAll();
            var fieldName = AutomationProperties.GetName(invalidField);
            if (string.IsNullOrWhiteSpace(fieldName)) fieldName = "Numeric field";
            var requirement = Equals(invalidField.Tag, "PositiveInteger") ? "a whole number greater than zero" : Equals(invalidField.Tag, "NonZeroInteger") ? "a non-zero wheel delta between -12000 and 12000" : "a whole number greater than or equal to zero";
            MessageBox.Show($"{fieldName} must be {requirement}. Enter a valid value, then save again.", "Check numeric value", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        foreach (var combo in FindVisualChildren<ComboBox>(this)) combo.GetBindingExpression(ComboBox.TextProperty)?.UpdateSource();
        if (Action is ImageScanAction imageAction)
        {
            if (!imageAction.HasTemplate) { MessageBox.Show("Choose a PNG image template before saving this action.", "Image template required", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (imageAction.RegionWidth == 0 ^ imageAction.RegionHeight == 0) { MessageBox.Show("Set both search region width and height, or leave both at zero to scan the full target client area.", "Check search region", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        }
        var keyName = Action switch { KeyPressAction key => key.KeyName, KeyHoldAction hold => hold.KeyName, _ => null };
        if (keyName is not null && !KeyNames.IsSupported(keyName)) { MessageBox.Show($"Unsupported key \"{keyName}\". Use a listed key or a shortcut such as CTRL+C.", "Invalid key", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        DialogResult = true;
    }
    private void ChooseTemplate_Click(object sender, RoutedEventArgs e)
    {
        if (Action is not ImageScanAction imageAction) return;
        var dialog = new OpenFileDialog { Title = LocalizationService.Get("ChoosePng"), Filter = "PNG image (*.png)|*.png", CheckFileExists = true };
        if (dialog.ShowDialog(this) != true) return;
        var bytes = File.ReadAllBytes(dialog.FileName);
        if (bytes.Length > 10 * 1024 * 1024) { MessageBox.Show("Choose a PNG smaller than 10 MB.", "Image is too large", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        try
        {
            using var stream = new MemoryStream(bytes);
            _ = new System.Windows.Media.Imaging.PngBitmapDecoder(stream, System.Windows.Media.Imaging.BitmapCreateOptions.PreservePixelFormat, System.Windows.Media.Imaging.BitmapCacheOption.OnLoad).Frames[0];
        }
        catch { MessageBox.Show("The selected file is not a readable PNG image.", "Invalid image", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        imageAction.TemplateName = Path.GetFileName(dialog.FileName);
        imageAction.TemplatePng = bytes;
    }
    private void ClearTemplate_Click(object sender, RoutedEventArgs e)
    {
        if (Action is not ImageScanAction imageAction) return;
        imageAction.TemplateName = string.Empty;
        imageAction.TemplatePng = [];
    }

    private TextBox? ValidateNumericFields()
    {
        TextBox? firstInvalid = null;
        foreach (var field in FindVisualChildren<TextBox>(this).Where(box => Equals(box.Tag, "NonNegativeInteger") || Equals(box.Tag, "PositiveInteger") || Equals(box.Tag, "NonZeroInteger")))
        {
            var valid = int.TryParse(field.Text, out var value) && (Equals(field.Tag, "PositiveInteger") ? value > 0 : Equals(field.Tag, "NonZeroInteger") ? value is >= -12000 and <= 12000 and not 0 : value >= 0);
            field.ClearValue(Control.BorderBrushProperty); field.ClearValue(FrameworkElement.ToolTipProperty);
            if (!valid)
            {
                field.BorderBrush = (Brush)FindResource("DangerBrush");
                field.ToolTip = Equals(field.Tag, "PositiveInteger") ? "Enter a whole number greater than zero." : Equals(field.Tag, "NonZeroInteger") ? "Enter a non-zero wheel delta between -12000 and 12000." : "Enter a whole number greater than or equal to zero.";
                firstInvalid ??= field;
            }
            else field.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
        }
        return firstInvalid;
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject root) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match) yield return match;
            foreach (var descendant in FindVisualChildren<T>(child)) yield return descendant;
        }
    }
    private async void PickPoint_Click(object sender, RoutedEventArgs e)
    {
        var hwnd = ResolveTarget(); if (hwnd == IntPtr.Zero || Action is not PointerAction pointer) return;
        try { Hide(); var point = await _picker.PickClientPointAsync(hwnd); pointer.ClientX = point.X; pointer.ClientY = point.Y; Show(); Activate(); }
        catch (Exception ex) { Show(); MessageBox.Show(ex.Message, "Could not pick point", MessageBoxButton.OK, MessageBoxImage.Error); }
    }
    private async void TestPoint_Click(object sender, RoutedEventArgs e)
    {
        var hwnd = ResolveTarget(); if (hwnd == IntPtr.Zero || Action is not PointerAction pointer) return;
        var point = new POINT(pointer.ClientX, pointer.ClientY); if (!NativeMethods.ClientToScreen(hwnd, ref point)) return;
        var marker = new PointMarkerWindow { Left = point.X - 24, Top = point.Y - 24, Owner = Owner }; marker.Show(); await Task.Delay(1400); marker.Close();
    }
    private IntPtr ResolveTarget()
    {
        if (_target is null) { MessageBox.Show("Select a target window before picking or testing a point.", "Target required", MessageBoxButton.OK, MessageBoxImage.Information); return IntPtr.Zero; }
        var hwnd = _windowManager.Resolve(_target); if (hwnd == IntPtr.Zero) MessageBox.Show("The target window is not currently open.", "Target not found", MessageBoxButton.OK, MessageBoxImage.Warning); return hwnd;
    }
}
