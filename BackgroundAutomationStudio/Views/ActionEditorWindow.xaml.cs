using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using BackgroundAutomationStudio.Models;
using BackgroundAutomationStudio.Native;
using BackgroundAutomationStudio.Services;

namespace BackgroundAutomationStudio.Views;

public partial class ActionEditorWindow : Window
{
    private readonly WindowTarget? _target;
    private readonly WindowPickerService _picker;
    private readonly IWindowManager _windowManager;
    public AutomationAction Action { get; }
    public ActionEditorWindow(AutomationAction action, WindowTarget? target, WindowPickerService picker, IWindowManager windowManager)
    {
        InitializeComponent(); Action = action; _target = target; _picker = picker; _windowManager = windowManager; DataContext = Action;
    }
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var invalidField = ValidateNumericFields();
        if (invalidField is not null)
        {
            invalidField.Focus(); invalidField.SelectAll();
            MessageBox.Show($"{AutomationProperties.GetName(invalidField)} must be a whole number greater than or equal to zero. Enter a valid value, then save again.", "Check numeric value", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        foreach (var combo in FindVisualChildren<ComboBox>(this)) combo.GetBindingExpression(ComboBox.TextProperty)?.UpdateSource();
        if (Action is KeyPressAction key && !KeyNames.IsSupported(key.KeyName)) { MessageBox.Show($"Unsupported key \"{key.KeyName}\". Use a listed key or a shortcut such as CTRL+C.", "Invalid key", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        DialogResult = true;
    }

    private TextBox? ValidateNumericFields()
    {
        TextBox? firstInvalid = null;
        foreach (var field in FindVisualChildren<TextBox>(this).Where(box => Equals(box.Tag, "NonNegativeInteger")))
        {
            var valid = int.TryParse(field.Text, out var value) && value >= 0;
            field.ClearValue(Control.BorderBrushProperty); field.ClearValue(FrameworkElement.ToolTipProperty);
            if (!valid)
            {
                field.BorderBrush = (Brush)FindResource("DangerBrush");
                field.ToolTip = "Enter a whole number greater than or equal to zero.";
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
