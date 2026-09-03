using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Threading;
using BackgroundAutomationStudio.MiniCore;
using Microsoft.Win32;

namespace BackgroundAutomationStudio.RecorderMini;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private readonly MiniWindowService _windowService = new();
    private readonly MiniRecorder _recorder = new();
    private readonly MiniGlobalHotkey _hotkey = new();
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(250) };
    private CancellationTokenSource? _playCts;
    private DateTimeOffset _started;
    private MiniWindowTarget? _selectedWindow;
    private string _language = "en";
    private string _statusEn = "Ready";
    private string _statusVi = "Sẵn sàng";
    public ObservableCollection<MiniWindowTarget> Windows { get; } = [];
    public ObservableCollection<RecordedMiniStep> Steps { get; } = [];
    public MiniWindowTarget? SelectedWindow { get => _selectedWindow; set { _selectedWindow = value; PropertyChanged?.Invoke(this, new(nameof(SelectedWindow))); UpdateButtons(); } }
    public event PropertyChangedEventHandler? PropertyChanged;

    public MainWindow()
    {
        InitializeComponent(); DataContext = this; MiniWindowAppearance.EnableDarkTitleBar(this);
        SourceInitialized += (_, _) => { if (!_hotkey.Register(this)) Status("CTRL+SHIFT+F9 is already used by another app", "CTRL+SHIFT+F9 đang được app khác sử dụng"); ApplyLanguage(); };
        _hotkey.Pressed += (_, _) => Dispatcher.Invoke(() => { if (PlayButton.IsEnabled) PlayButton_Click(PlayButton, new RoutedEventArgs()); });
        _recorder.StepCaptured += (_, step) => Dispatcher.Invoke(() => { Steps.Add(step); EmptyState.Visibility = Visibility.Collapsed; UpdateButtons(); });
        _timer.Tick += (_, _) => TimerText.Text = (DateTimeOffset.Now - _started).ToString(@"hh\:mm\:ss");
        RefreshWindows(); ApplyLanguage(); UpdateButtons();
    }

    private void RefreshWindows() { var previous = SelectedWindow?.Handle; Windows.Clear(); foreach (var window in _windowService.GetWindows()) Windows.Add(window); SelectedWindow = Windows.FirstOrDefault(item => item.Handle == previous) ?? Windows.FirstOrDefault(); Status($"Found {Windows.Count} windows", $"Đã tìm thấy {Windows.Count} cửa sổ"); }
    private void RefreshButton_Click(object sender, RoutedEventArgs e) => RefreshWindows();
    private void RecordButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_recorder.IsRecording) { _recorder.Stop(); _timer.Stop(); RecordingBanner.Visibility = Visibility.Collapsed; Status($"Captured {Steps.Count} actions", $"Đã ghi {Steps.Count} thao tác"); }
            else { if (SelectedWindow is null) return; Steps.Clear(); EmptyState.Visibility = Visibility.Visible; _recorder.Start(SelectedWindow.Handle, PointerPathBox.IsChecked == true); _started = DateTimeOffset.Now; _timer.Start(); RecordingBanner.Visibility = Visibility.Visible; Status(PointerPathBox.IsChecked == true ? "Recording actions and pointer movement inside the focused target" : "Only actions inside the selected window are recorded", PointerPathBox.IsChecked == true ? "Đang ghi thao tác và đường đi chuột trong cửa sổ đích có focus" : "Chỉ thao tác trong cửa sổ đã chọn được ghi"); }
        }
        catch (Exception ex) { ShowOperationError(ex, "Recording could not start. Check the selected window and try again.", "Không thể bắt đầu ghi. Hãy kiểm tra cửa sổ đích rồi thử lại."); }
        ApplyLanguage(); UpdateButtons();
    }
    private void ClearButton_Click(object sender, RoutedEventArgs e) { Steps.Clear(); EmptyState.Visibility = Visibility.Visible; Status("Recording cleared", "Đã xóa bản ghi"); UpdateButtons(); }
    private async void PlayButton_Click(object sender, RoutedEventArgs e)
    {
        if (_playCts is not null) { _playCts.Cancel(); return; }
        if (SelectedWindow is null || Steps.Count == 0) return;
        if (!int.TryParse(PressBox.Text, out var press) || press < 10 || press > 1000)
        {
            PressBox.Focus(); PressBox.SelectAll();
            MessageBox.Show(IsVi ? "Thời gian nhấn phải từ 10 đến 1000 ms." : "Press duration must be from 10 to 1000 ms.", Title, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        _playCts = new(); ApplyLanguage(); UpdateButtons();
        try
        {
            Status("Focusing the target", "Đang focus cửa sổ đích");
            await MiniForegroundInput.ActivateAsync(SelectedWindow.Handle, _playCts.Token);
            for (var index = 0; index < Steps.Count; index++)
            {
                var step = Steps[index];
                var current = index + 1;
                if (step.DelayMilliseconds > 0) await MiniForegroundInput.DelayWithFocusAsync(SelectedWindow.Handle, step.DelayMilliseconds, focused => Dispatcher.Invoke(() =>
                    Status(focused ? $"Playing {current} / {Steps.Count}" : "Paused - focus the selected target", focused ? $"Đang phát {current} / {Steps.Count}" : "Tạm dừng - hãy focus cửa sổ đích")), _playCts.Token);
                Status($"Playing {current} / {Steps.Count}", $"Đang phát {current} / {Steps.Count}");
                await MiniForegroundInput.SendStepAsync(SelectedWindow.Handle, step, press, focused => Dispatcher.Invoke(() =>
                    Status(focused ? $"Playing {current} / {Steps.Count}" : "Paused - focus the selected target", focused ? $"Đang phát {current} / {Steps.Count}" : "Tạm dừng - hãy focus cửa sổ đích")), _playCts.Token);
            }
            Status($"Completed {Steps.Count} actions", $"Đã hoàn tất {Steps.Count} thao tác");
        }
        catch (OperationCanceledException) { Status("Playback stopped", "Đã dừng phát"); }
        catch (Exception ex) { Status("Playback error", "Lỗi phát lại"); ShowOperationError(ex, "Playback stopped because Windows rejected the target or physical input. Check the target and administrator level, then try again.", "Phát lại đã dừng vì Windows từ chối cửa sổ đích hoặc input vật lý. Hãy kiểm tra cửa sổ đích, quyền quản trị rồi thử lại."); }
        finally { _playCts.Dispose(); _playCts = null; ApplyLanguage(); UpdateButtons(); }
    }
    private void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedWindow is null || Steps.Count == 0) return;
        var dialog = new SaveFileDialog { Title = IsVi ? "Xuất bản ghi" : "Export recording", Filter = "JSON recording (*.json)|*.json", FileName = $"recording-{DateTime.Now:yyyyMMdd-HHmmss}.json" };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            var export = new RecorderMiniExport("Background Automation Foreground Recorder Mini", DateTimeOffset.Now, SelectedWindow.ProcessName, SelectedWindow.Title, Steps.ToList());
            File.WriteAllText(dialog.FileName, JsonSerializer.Serialize(export, new JsonSerializerOptions { WriteIndented = true }));
            Status($"Exported {Path.GetFileName(dialog.FileName)}", $"Đã xuất {Path.GetFileName(dialog.FileName)}");
        }
        catch (Exception ex)
        {
            Status("Export failed", "Xuất bản ghi thất bại");
            ShowOperationError(ex, "The recording could not be saved. Choose another folder and try again.", "Không thể lưu bản ghi. Hãy chọn thư mục khác rồi thử lại.");
        }
    }
    private void LanguageBox_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (!IsLoaded || LanguageBox.SelectedItem is not ComboBoxItem item) return; _language = item.Tag?.ToString() == "vi" ? "vi" : "en"; ApplyLanguage(); }
    public bool IsVietnamese => _language == "vi";
    private bool IsVi => IsVietnamese;
    private void ApplyLanguage()
    {
        TitleText.Text = "Foreground Recorder Mini"; SubtitleText.Text = IsVi ? "Ghi một chuỗi ngắn rồi phát bằng input vật lý khi cửa sổ đích có focus." : "Record a short sequence, then replay it with focused physical input.";
        TargetLabel.Text = IsVi ? "CỬA SỔ ĐÍCH" : "TARGET WINDOW"; PressLabel.Text = IsVi ? "THỜI GIAN NHẤN (MS)" : "PRESS (MS)"; PointerPathLabel.Text = IsVi ? "Ghi đường đi chuột" : "Record pointer movement"; PointerPathHelp.Text = IsVi ? "Ghi chuyển động thường trong cửa sổ đích có focus. Khi phát, app sẽ lấy con trỏ thật." : "Adds normal movement inside the focused target. Playback takes control of your pointer."; ForegroundHelp.Text = IsVi ? "Phát lại sẽ focus cửa sổ đích và dùng chuột cùng bàn phím vật lý của bạn." : "Playback focuses the target and uses your physical mouse and keyboard."; RefreshButton.Content = IsVi ? "Làm mới" : "Refresh"; RecordingText.Text = IsVi ? "ĐANG GHI" : "RECORDING"; EmptyTitle.Text = IsVi ? "Chưa có thao tác" : "No actions recorded"; EmptyHelp.Text = IsVi ? "Chọn cửa sổ, bắt đầu ghi, sau đó thao tác trong cửa sổ đó." : "Choose a target, start recording, then work inside that window."; ClearButton.Content = IsVi ? "Xóa" : "Clear"; ExportButton.Content = IsVi ? "Xuất" : "Export"; RecordButton.Content = _recorder.IsRecording ? (IsVi ? "Dừng ghi" : "Stop recording") : (IsVi ? "Bắt đầu ghi" : "Start recording"); PlayButton.Content = _playCts is not null ? (IsVi ? "Dừng" : "Stop") : (IsVi ? "Phát" : "Play");
        StatusText.Text = IsVi ? _statusVi : _statusEn;
        PropertyChanged?.Invoke(this, new(nameof(IsVietnamese)));
        AutomationProperties.SetName(LanguageBox, IsVi ? "Ngôn ngữ giao diện" : "Interface language"); AutomationProperties.SetName(WindowBox, IsVi ? "Cửa sổ đích" : "Target window"); AutomationProperties.SetName(PressBox, IsVi ? "Thời gian giữ input vật lý" : "Physical input press duration"); AutomationProperties.SetName(PointerPathBox, IsVi ? "Ghi đường đi chuột vật lý" : "Record physical pointer movement"); AutomationProperties.SetHelpText(PointerPathBox, PointerPathHelp.Text); AutomationProperties.SetName(StepsList, IsVi ? "Các thao tác đã ghi" : "Recorded actions"); AutomationProperties.SetName(StatusText, IsVi ? "Trạng thái ghi và phát" : "Record and playback status"); AutomationProperties.SetName(RecordingBanner, IsVi ? "Đang ghi thao tác" : "Recording active");
    }
    private void UpdateButtons() { var idle = !_recorder.IsRecording && _playCts is null; WindowBox.IsEnabled = RefreshButton.IsEnabled = PressBox.IsEnabled = PointerPathBox.IsEnabled = idle; ClearButton.IsEnabled = ExportButton.IsEnabled = idle && Steps.Count > 0; RecordButton.IsEnabled = _recorder.IsRecording || (idle && SelectedWindow is not null); PlayButton.IsEnabled = _playCts is not null || (idle && SelectedWindow is not null && Steps.Count > 0); }
    private void Status(string english, string vietnamese) { _statusEn = english; _statusVi = vietnamese; StatusText.Text = IsVi ? vietnamese : english; }
    private void ShowOperationError(Exception exception, string english, string vietnamese)
    {
        var message = IsVi ? vietnamese : english;
        MessageBox.Show($"{message}\n\n{(IsVi ? "Chi tiết kỹ thuật" : "Technical detail")}: {exception.Message}", Title, MessageBoxButton.OK, MessageBoxImage.Error);
    }
    private void Window_Closing(object? sender, CancelEventArgs e) { _timer.Stop(); _playCts?.Cancel(); _hotkey.Dispose(); _recorder.Dispose(); }
}
