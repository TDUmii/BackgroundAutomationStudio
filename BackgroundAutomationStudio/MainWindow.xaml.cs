using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BackgroundAutomationStudio.Models;
using BackgroundAutomationStudio.Services;
using BackgroundAutomationStudio.ViewModels;
using BackgroundAutomationStudio.Views;

namespace BackgroundAutomationStudio;

public partial class MainWindow : Window
{
    private readonly SettingsService _settings;
    private readonly GlobalHotkeyService _runHotkey = new(0xB451);
    private readonly GlobalHotkeyService _pauseHotkey = new(0xB452);
    private Point _dragStart; private bool _allowClose;

    public MainWindow(MainViewModel viewModel, SettingsService settings)
    {
        _settings = settings;
        InitializeComponent();
        DataContext = viewModel;
        UpdateHotkeyLabel();
        _runHotkey.Pressed += (_, _) => Dispatcher.Invoke(viewModel.ToggleRunFromHotkey);
        _pauseHotkey.Pressed += (_, _) => Dispatcher.Invoke(viewModel.TogglePauseFromHotkey);
    }

    private void MenuButton_Click(object sender, RoutedEventArgs e) { if (sender is Button button && button.ContextMenu is { } menu) { menu.PlacementTarget = button; menu.DataContext = DataContext; menu.IsOpen = true; } }
    private void Window_SourceInitialized(object? sender, EventArgs e) => RegisterHotkey(true);

    private void SettingsButton_Click(object sender, RoutedEventArgs e) => ShowSettings();
    private void HotkeyButton_Click(object sender, RoutedEventArgs e) => ShowSettings();

    private void ShowSettings()
    {
        var dialog = new SettingsWindow(_settings.Current) { Owner = this };
        _runHotkey.Unregister();
        _pauseHotkey.Unregister();
        try
        {
            if (dialog.ShowDialog() != true || dialog.Result is null) return;
            _settings.Save(dialog.Result);
            LocalizationService.Apply(dialog.Result.Language);
            if (DataContext is MainViewModel vm) vm.RefreshLanguage();
            UpdateHotkeyLabel();
        }
        finally
        {
            RegisterHotkey(false);
        }
    }

    private void RegisterHotkey(bool silent)
    {
        var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
        var runRegistered = _runHotkey.Register(hwnd, _settings.Current.RunHotkey);
        var pauseRegistered = _pauseHotkey.Register(hwnd, _settings.Current.PauseHotkey);
        if (runRegistered && pauseRegistered) return;
        if (!silent) MessageBox.Show(LocalizationService.Language == "vi" ? "Một phím tắt đang được ứng dụng khác sử dụng. Hãy chọn tổ hợp khác." : "One shortcut is already used by another application. Choose a different shortcut.", LocalizationService.Get("SettingsTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void UpdateHotkeyLabel() => HotkeyText.Text = $"{LocalizationService.Get("RunStopShort")}: {_settings.Current.RunHotkey}  ·  {LocalizationService.Get("PauseShort")}: {_settings.Current.PauseHotkey}";
    private void WorkflowList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) => _dragStart = e.GetPosition(null);
    private void WorkflowList_MouseMove(object sender, MouseEventArgs e) { var point = e.GetPosition(null); if (e.LeftButton != MouseButtonState.Pressed || Math.Abs(point.X - _dragStart.X) < SystemParameters.MinimumHorizontalDragDistance && Math.Abs(point.Y - _dragStart.Y) < SystemParameters.MinimumVerticalDragDistance) return; var item = FindAncestor<ListBoxItem>((DependencyObject)e.OriginalSource); if (item?.DataContext is AutomationAction action) DragDrop.DoDragDrop(item, action, DragDropEffects.Move); }
    private void WorkflowList_Drop(object sender, DragEventArgs e) { if (DataContext is not MainViewModel vm || e.Data.GetData(typeof(AutomationAction)) is not AutomationAction source) return; var targetItem = FindAncestor<ListBoxItem>((DependencyObject)e.OriginalSource); if (targetItem?.DataContext is AutomationAction target) vm.MoveAction(vm.Actions.IndexOf(source), vm.Actions.IndexOf(target)); }
    private void WorkflowList_MouseDoubleClick(object sender, MouseButtonEventArgs e) { if (DataContext is MainViewModel vm && vm.EditCommand.CanExecute(null)) vm.EditCommand.Execute(null); }
    private async void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (_allowClose || DataContext is not MainViewModel vm) return;
        if (!vm.IsModified)
        {
            _allowClose = true;
            DisposeHotkeys();
            return;
        }

        e.Cancel = true;
        if (!await vm.TryCloseAsync()) return;
        _allowClose = true;
        DisposeHotkeys();
        Close();
    }

    private void DisposeHotkeys()
    {
        _runHotkey.Dispose();
        _pauseHotkey.Dispose();
    }
    private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject { while (current is not null) { if (current is T found) return found; current = VisualTreeHelper.GetParent(current); } return null; }
}
