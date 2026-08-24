using System.ComponentModel;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
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
        Topmost = settings.Current.AlwaysOnTop;
        PinButton.Tag = Topmost;
        PinButton.ToolTip = LocalizationService.Get(Topmost ? "UnpinWindow" : "PinWindow");
        AutomationProperties.SetName(PinButton, LocalizationService.Get(Topmost ? "UnpinWindow" : "PinWindow"));
        UpdateHotkeyLabel();
        UpdatePlaybackModeIndicator();
        _runHotkey.Pressed += (_, _) => Dispatcher.Invoke(viewModel.ToggleRunFromHotkey);
        _pauseHotkey.Pressed += (_, _) => Dispatcher.Invoke(viewModel.TogglePauseFromHotkey);
    }

    private void MenuButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.ContextMenu is not { } menu) return;
        menu.PlacementTarget = button;
        menu.DataContext = DataContext;
        menu.Closed -= Menu_Closed;
        menu.Closed += Menu_Closed;
        menu.IsOpen = true;
        AnimateTopMenuUnderline(button, true);
    }

    private void Menu_Closed(object? sender, RoutedEventArgs e)
    {
        if (sender is ContextMenu { PlacementTarget: Button button })
            AnimateTopMenuUnderline(button, button.IsMouseOver || button.IsKeyboardFocusWithin);
    }

    private void TopMenuButton_MouseEnter(object sender, MouseEventArgs e) => AnimateTopMenuUnderline((Button)sender, true);
    private void TopMenuButton_MouseLeave(object sender, MouseEventArgs e)
    {
        var button = (Button)sender;
        if (button.ContextMenu?.IsOpen != true && !button.IsKeyboardFocusWithin) AnimateTopMenuUnderline(button, false);
    }
    private void TopMenuButton_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) => AnimateTopMenuUnderline((Button)sender, true);
    private void TopMenuButton_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        var button = (Button)sender;
        if (button.ContextMenu?.IsOpen != true && !button.IsMouseOver) AnimateTopMenuUnderline(button, false);
    }

    private static void AnimateTopMenuUnderline(Button button, bool show)
    {
        button.ApplyTemplate();
        if (button.Template.FindName("Underline", button) is not Border underline || underline.RenderTransform is not ScaleTransform templateScale) return;
        var scale = EnsureMutableScaleTransform(templateScale);
        if (!ReferenceEquals(scale, templateScale)) underline.RenderTransform = scale;
        if (!SystemParameters.ClientAreaAnimation)
        {
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            underline.BeginAnimation(OpacityProperty, null);
            scale.ScaleX = show ? 1 : 0;
            underline.Opacity = show ? 1 : 0;
            return;
        }
        var duration = TimeSpan.FromMilliseconds(show ? 140 : 90);
        var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(show ? 1 : 0, duration) { EasingFunction = easing });
        underline.BeginAnimation(OpacityProperty, new DoubleAnimation(show ? 1 : 0, duration) { EasingFunction = easing });
    }

    internal static ScaleTransform EnsureMutableScaleTransform(ScaleTransform transform) =>
        transform.IsFrozen ? transform.Clone() : transform;
    private void Window_SourceInitialized(object? sender, EventArgs e) => RegisterHotkey(true);

    private void PinButton_Click(object sender, RoutedEventArgs e)
    {
        Topmost = !Topmost;
        _settings.Current.AlwaysOnTop = Topmost;
        _settings.Save(_settings.Current);
        PinButton.Tag = Topmost;
        PinButton.ToolTip = LocalizationService.Get(Topmost ? "UnpinWindow" : "PinWindow");
        AutomationProperties.SetName(PinButton, LocalizationService.Get(Topmost ? "UnpinWindow" : "PinWindow"));
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e) => SystemCommands.MinimizeWindow(this);
    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        if (WindowState == WindowState.Maximized) SystemCommands.RestoreWindow(this);
        else SystemCommands.MaximizeWindow(this);
    }
    private void CloseButton_Click(object sender, RoutedEventArgs e) => SystemCommands.CloseWindow(this);
    private void Window_StateChanged(object? sender, EventArgs e)
    {
        if (MaximizeGlyph is null) return;
        var maximized = WindowState == WindowState.Maximized;
        MaximizeGlyph.Data = Geometry.Parse(maximized
            ? "M4,2 L12,2 L12,10 M2,4 L10,4 L10,12 L2,12 Z"
            : "M2,2 L12,2 L12,12 L2,12 Z");
        MaximizeButton.ToolTip = LocalizationService.Get(maximized ? "RestoreWindow" : "MaximizeWindow");
        AutomationProperties.SetName(MaximizeButton, LocalizationService.Get(maximized ? "RestoreWindow" : "MaximizeWindow"));
    }

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
            UpdatePlaybackModeIndicator();
            UpdateTitleBarTooltips();
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
    private void UpdatePlaybackModeIndicator()
    {
        var mode = PlaybackModes.Normalize(_settings.Current.PlaybackMode);
        PlaybackModeText.Text = $"{LocalizationService.Get("EngineActive")} {PlaybackModes.GetIndex(mode)} · {LocalizationService.Get(PlaybackModes.GetResourceKey(mode))}";
        PlaybackModeText.ToolTip = LocalizationService.Get("PlaybackModeHelp");
    }
    private void UpdateTitleBarTooltips()
    {
        PinButton.ToolTip = LocalizationService.Get(Topmost ? "UnpinWindow" : "PinWindow");
        MaximizeButton.ToolTip = LocalizationService.Get(WindowState == WindowState.Maximized ? "RestoreWindow" : "MaximizeWindow");
        AutomationProperties.SetName(PinButton, LocalizationService.Get(Topmost ? "UnpinWindow" : "PinWindow"));
        AutomationProperties.SetName(MaximizeButton, LocalizationService.Get(WindowState == WindowState.Maximized ? "RestoreWindow" : "MaximizeWindow"));
    }
    private void WorkflowList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) => _dragStart = e.GetPosition(null);
    private void WorkflowList_MouseMove(object sender, MouseEventArgs e) { var point = e.GetPosition(null); if (e.LeftButton != MouseButtonState.Pressed || Math.Abs(point.X - _dragStart.X) < SystemParameters.MinimumHorizontalDragDistance && Math.Abs(point.Y - _dragStart.Y) < SystemParameters.MinimumVerticalDragDistance) return; var item = FindAncestor<ListBoxItem>((DependencyObject)e.OriginalSource); if (item?.DataContext is AutomationAction action) DragDrop.DoDragDrop(item, action, DragDropEffects.Move); }
    private void WorkflowList_Drop(object sender, DragEventArgs e) { if (DataContext is not MainViewModel vm || e.Data.GetData(typeof(AutomationAction)) is not AutomationAction source) return; var targetItem = FindAncestor<ListBoxItem>((DependencyObject)e.OriginalSource); if (targetItem?.DataContext is AutomationAction target) vm.MoveAction(vm.Actions.IndexOf(source), vm.Actions.IndexOf(target)); }
    private void WorkflowList_MouseDoubleClick(object sender, MouseButtonEventArgs e) { if (DataContext is MainViewModel vm && vm.EditCommand.CanExecute(null)) vm.EditCommand.Execute(null); }
    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!WorkflowList.IsKeyboardFocusWithin || DataContext is not MainViewModel vm) return;
        var modifiers = Keyboard.Modifiers;
        ICommand? command = e.Key switch
        {
            Key.C when modifiers == ModifierKeys.Control => vm.CopyCommand,
            Key.X when modifiers == ModifierKeys.Control => vm.CutCommand,
            Key.V when modifiers == ModifierKeys.Control => vm.PasteCommand,
            Key.D when modifiers == ModifierKeys.Control => vm.DuplicateCommand,
            Key.Space when modifiers == ModifierKeys.None => vm.ToggleEnabledCommand,
            Key.Delete when modifiers == ModifierKeys.None => vm.DeleteCommand,
            Key.Enter when modifiers == ModifierKeys.None => vm.EditCommand,
            Key.Up when modifiers == ModifierKeys.Alt => vm.MoveUpCommand,
            Key.Down when modifiers == ModifierKeys.Alt => vm.MoveDownCommand,
            _ => null
        };
        if (command?.CanExecute(null) != true) return;
        command.Execute(null); e.Handled = true;
    }
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
