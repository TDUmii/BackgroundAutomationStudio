using System.Windows;
using System.Windows.Controls;
using System.Windows.Automation;
using System.Windows.Input;
using System.Windows.Threading;
using BackgroundAutomationStudio.Models;
using BackgroundAutomationStudio.Services;

namespace BackgroundAutomationStudio.Views;

public partial class SettingsWindow : Window
{
    private readonly AppSettings _original;
    public AppSettings? Result { get; private set; }

    public SettingsWindow(AppSettings settings)
    {
        InitializeComponent();
        WindowAppearance.EnableDarkTitleBar(this);
        _original = settings;
        HotkeyBox.Text = settings.RunHotkey;
        PauseHotkeyBox.Text = settings.PauseHotkey;
        GamePressDurationBox.Text = AppSettings.NormalizeGamePressDuration(settings.GamePressDurationMilliseconds).ToString();
        RecordPointerPathBox.IsChecked = settings.RecordPointerPath;
        if (settings.Language == "vi") VietnameseRadio.IsChecked = true; else EnglishRadio.IsChecked = true;
        switch (PlaybackModes.Normalize(settings.PlaybackMode))
        {
            case PlaybackModes.UiAutomation: UiAutomationRadio.IsChecked = true; break;
            case PlaybackModes.Win32Messages: Win32Radio.IsChecked = true; break;
            case PlaybackModes.GameForeground: GameForegroundRadio.IsChecked = true; break;
            case PlaybackModes.GameBackground: GameBackgroundRadio.IsChecked = true; break;
            default: AutomaticRadio.IsChecked = true; break;
        }
        UpdateLanguageHelp();
    }

    private void LanguageChoice_Checked(object sender, RoutedEventArgs e) => UpdateLanguageHelp();

    private void SettingsWindow_Loaded(object sender, RoutedEventArgs e) => BringSelectedModeIntoView();

    private void BringSelectedModeIntoView()
    {
        var selected = new[] { AutomaticRadio, GameForegroundRadio, GameBackgroundRadio, UiAutomationRadio, Win32Radio }.FirstOrDefault(choice => choice.IsChecked == true);
        if (selected is not null)
        {
            Dispatcher.BeginInvoke(new Action(selected.BringIntoView), DispatcherPriority.ContextIdle);
        }
    }

    private void SettingsWindow_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (SettingsScrollViewer.ScrollableHeight <= 0) return;
        SettingsScrollViewer.ScrollToVerticalOffset(SettingsScrollViewer.VerticalOffset - e.Delta);
        e.Handled = true;
    }

    private void UpdateLanguageHelp()
    {
        if (LanguageHelp is null) return;
        LanguageHelp.Text = VietnameseRadio?.IsChecked == true ? "Tiếng Anh là ngôn ngữ mặc định. Thay đổi có hiệu lực ngay sau khi lưu." : "English is used by default. Changes apply immediately after saving.";
    }

    private void HotkeyBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key == Key.Tab)
        {
            e.Handled = true;
            var direction = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? FocusNavigationDirection.Previous : FocusNavigationDirection.Next;
            (sender as UIElement)?.MoveFocus(new TraversalRequest(direction));
            return;
        }
        if (key == Key.Escape) return;
        e.Handled = true;
        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftShift or Key.RightShift or Key.LeftAlt or Key.RightAlt or Key.LWin or Key.RWin) return;
        var text = HotkeyParser.FromKeyEvent(key, Keyboard.Modifiers);
        if (!string.IsNullOrEmpty(text) && sender is TextBox box)
        {
            box.Text = text;
            if (box == PauseHotkeyBox) PauseHotkeyError.Visibility = Visibility.Collapsed; else HotkeyError.Visibility = Visibility.Collapsed;
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!HotkeyParser.TryParse(HotkeyBox.Text, out _, out _))
        {
            HotkeyError.Text = VietnameseRadio.IsChecked == true ? "Hãy dùng ít nhất một phím bổ trợ (Ctrl/Shift/Alt/Win) và một phím F, chữ hoặc số." : "Use at least one modifier (Ctrl/Shift/Alt/Win) plus an F-key, letter, or number.";
            HotkeyError.Visibility = Visibility.Visible;
            return;
        }
        if (!HotkeyParser.TryParse(PauseHotkeyBox.Text, out _, out _))
        {
            PauseHotkeyError.Text = VietnameseRadio.IsChecked == true ? "Hãy chọn một tổ hợp phím tạm dừng hợp lệ." : "Choose a valid pause shortcut.";
            PauseHotkeyError.Visibility = Visibility.Visible;
            return;
        }
        if (string.Equals(HotkeyBox.Text, PauseHotkeyBox.Text, StringComparison.OrdinalIgnoreCase))
        {
            PauseHotkeyError.Text = VietnameseRadio.IsChecked == true ? "Hai chức năng cần hai phím tắt khác nhau." : "Run/Stop and Pause/Resume need different shortcuts.";
            PauseHotkeyError.Visibility = Visibility.Visible;
            return;
        }
        if (!int.TryParse(GamePressDurationBox.Text, out var gamePressDuration) ||
            gamePressDuration < AppSettings.MinimumGamePressDurationMilliseconds ||
            gamePressDuration > AppSettings.MaximumGamePressDurationMilliseconds)
        {
            GamePressDurationError.Text = VietnameseRadio.IsChecked == true ? "Nhập từ 10 đến 1000 mili giây." : "Enter a value from 10 to 1000 milliseconds.";
            AutomationProperties.SetHelpText(GamePressDurationBox, GamePressDurationError.Text);
            GamePressDurationError.Visibility = Visibility.Visible;
            GamePressDurationBox.Focus();
            GamePressDurationBox.SelectAll();
            return;
        }
        GamePressDurationError.Visibility = Visibility.Collapsed;
        AutomationProperties.SetHelpText(GamePressDurationBox, LocalizationService.Get("GamePressDurationHelp"));
        Result = new AppSettings
        {
            Language = VietnameseRadio.IsChecked == true ? "vi" : "en",
            RunHotkey = HotkeyBox.Text,
            PauseHotkey = PauseHotkeyBox.Text,
            AlwaysOnTop = _original.AlwaysOnTop,
            RecordPointerPath = RecordPointerPathBox.IsChecked == true,
            GamePressDurationMilliseconds = gamePressDuration,
            PlaybackMode = UiAutomationRadio.IsChecked == true ? PlaybackModes.UiAutomation
                : Win32Radio.IsChecked == true ? PlaybackModes.Win32Messages
                : GameForegroundRadio.IsChecked == true ? PlaybackModes.GameForeground
                : GameBackgroundRadio.IsChecked == true ? PlaybackModes.GameBackground
                : PlaybackModes.Automatic
        };
        DialogResult = true;
    }
}
