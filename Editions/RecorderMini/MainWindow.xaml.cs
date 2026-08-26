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
        _hotkey.Pressed += (_, _) => Dispatcher.Invoke(() => { if (RecordButton.IsEnabled) RecordButton_Click(RecordButton, new RoutedEventArgs()); });
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
            else { if (SelectedWindow is null) return; Steps.Clear(); EmptyState.Visibility = Visibility.Visible; _recorder.Start(SelectedWindow.Handle); _started = DateTimeOffset.Now; _timer.Start(); RecordingBanner.Visibility = Visibility.Visible; Status("Only actions inside the selected window are recorded", "Chỉ thao tác trong cửa sổ đã chọn được ghi"); }
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, Title, MessageBoxButton.OK, MessageBoxImage.Error); }
        ApplyLanguage(); UpdateButtons();
    }
    private void ClearButton_Click(object sender, RoutedEventArgs e) { Steps.Clear(); EmptyState.Visibility = Visibility.Visible; Status("Recording cleared", "Đã xóa bản ghi"); UpdateButtons(); }
    private void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedWindow is null || Steps.Count == 0) return;
        var dialog = new SaveFileDialog { Title = IsVi ? "Xuất bản ghi" : "Export recording", Filter = "JSON recording (*.json)|*.json", FileName = $"recording-{DateTime.Now:yyyyMMdd-HHmmss}.json" };
        if (dialog.ShowDialog(this) != true) return;
        var export = new RecorderMiniExport("Background Automation Recorder Mini", DateTimeOffset.Now, SelectedWindow.ProcessName, SelectedWindow.Title, Steps.ToList());
        File.WriteAllText(dialog.FileName, JsonSerializer.Serialize(export, new JsonSerializerOptions { WriteIndented = true }));
        Status($"Exported {Path.GetFileName(dialog.FileName)}", $"Đã xuất {Path.GetFileName(dialog.FileName)}");
    }
    private void LanguageBox_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (!IsLoaded || LanguageBox.SelectedItem is not ComboBoxItem item) return; _language = item.Tag?.ToString() == "vi" ? "vi" : "en"; ApplyLanguage(); }
    private bool IsVi => _language == "vi";
    private void ApplyLanguage()
    {
        TitleText.Text = "Recorder Mini"; SubtitleText.Text = IsVi ? "Ghi thao tác từ một cửa sổ và xuất JSON gọn nhẹ. Chạy hoặc dừng: CTRL+SHIFT+F9." : "Capture one window and export portable JSON. Start or stop: CTRL+SHIFT+F9.";
        TargetLabel.Text = IsVi ? "CỬA SỔ ĐÍCH" : "TARGET WINDOW"; RefreshButton.Content = IsVi ? "Làm mới" : "Refresh"; RecordingText.Text = IsVi ? "ĐANG GHI" : "RECORDING"; EmptyTitle.Text = IsVi ? "Chưa có thao tác" : "No actions recorded"; EmptyHelp.Text = IsVi ? "Chọn cửa sổ, bắt đầu ghi, sau đó thao tác trong cửa sổ đó." : "Choose a target, start recording, then work inside that window."; ClearButton.Content = IsVi ? "Xóa" : "Clear"; ExportButton.Content = IsVi ? "Xuất" : "Export"; RecordButton.Content = _recorder.IsRecording ? (IsVi ? "Dừng ghi" : "Stop recording") : (IsVi ? "Bắt đầu ghi" : "Start recording");
        StatusText.Text = IsVi ? _statusVi : _statusEn;
        AutomationProperties.SetName(LanguageBox, IsVi ? "Ngôn ngữ giao diện" : "Interface language"); AutomationProperties.SetName(WindowBox, IsVi ? "Cửa sổ đích" : "Target window"); AutomationProperties.SetName(StepsList, IsVi ? "Các thao tác đã ghi" : "Recorded actions"); AutomationProperties.SetName(StatusText, IsVi ? "Trạng thái ghi" : "Recorder status"); AutomationProperties.SetName(RecordingBanner, IsVi ? "Đang ghi thao tác" : "Recording active");
    }
    private void UpdateButtons() { WindowBox.IsEnabled = !_recorder.IsRecording; RefreshButton.IsEnabled = !_recorder.IsRecording; ClearButton.IsEnabled = !_recorder.IsRecording && Steps.Count > 0; ExportButton.IsEnabled = !_recorder.IsRecording && Steps.Count > 0; RecordButton.IsEnabled = _recorder.IsRecording || SelectedWindow is not null; }
    private void Status(string english, string vietnamese) { _statusEn = english; _statusVi = vietnamese; StatusText.Text = IsVi ? vietnamese : english; }
    private void Window_Closing(object? sender, CancelEventArgs e) { _timer.Stop(); _hotkey.Dispose(); _recorder.Dispose(); }
}
