using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using BackgroundAutomationStudio.MiniCore;

namespace BackgroundAutomationStudio.ClickRepeaterMini;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private readonly MiniWindowService _windowService = new();
    private readonly MiniPointPicker _picker = new();
    private readonly MiniGlobalHotkey _hotkey = new();
    private CancellationTokenSource? _runCts;
    private CancellationTokenSource? _pickCts;
    private MiniWindowTarget? _selectedWindow;
    private string _language = "en";
    private bool _hotkeyAvailable = true;
    private string _stateEn = "Ready";
    private string _stateVi = "Sẵn sàng";
    private string _progressEn = "Choose a target and point";
    private string _progressVi = "Chọn cửa sổ đích và tọa độ";
    public ObservableCollection<MiniWindowTarget> Windows { get; } = [];
    public MiniWindowTarget? SelectedWindow { get => _selectedWindow; set { _selectedWindow = value; PropertyChanged?.Invoke(this, new(nameof(SelectedWindow))); UpdateControls(); } }
    public event PropertyChangedEventHandler? PropertyChanged;
    private bool IsRunning => _runCts is not null;
    private bool IsPicking => _pickCts is not null;
    private bool IsVi => _language == "vi";

    public MainWindow() { InitializeComponent(); DataContext = this; MiniWindowAppearance.EnableDarkTitleBar(this); SourceInitialized += (_, _) => { _hotkeyAvailable = _hotkey.Register(this); if (!_hotkeyAvailable) SetRunStatus("Ready", "Sẵn sàng", "CTRL+SHIFT+F9 is already used by another app", "CTRL+SHIFT+F9 đang được app khác sử dụng"); }; PreviewKeyDown += (_, e) => { if (e.Key == Key.Escape && IsPicking) { _pickCts!.Cancel(); e.Handled = true; } }; _hotkey.Pressed += (_, _) => Dispatcher.Invoke(() => { if (RunButton.IsEnabled) RunButton_Click(RunButton, new RoutedEventArgs()); }); RefreshWindows(); ApplyLanguage(); UpdateControls(); }
    private void RefreshWindows() { var previous = SelectedWindow?.Handle; Windows.Clear(); foreach (var window in _windowService.GetWindows()) Windows.Add(window); SelectedWindow = Windows.FirstOrDefault(item => item.Handle == previous) ?? Windows.FirstOrDefault(); }
    private void RefreshButton_Click(object sender, RoutedEventArgs e) => RefreshWindows();
    private async void PickButton_Click(object sender, RoutedEventArgs e)
    {
        if (IsPicking) { _pickCts!.Cancel(); return; }
        if (SelectedWindow is null) return;
        var operation = new CancellationTokenSource();
        _pickCts = operation;
        try
        {
            SetRunStatus("Pick a point", "Chọn một điểm", "Click inside the target or press Escape", "Nhấp trong cửa sổ đích hoặc nhấn Escape");
            Opacity = 0.72; ApplyLanguage(); UpdateControls();
            var point = await _picker.PickAsync(SelectedWindow.Handle, operation.Token);
            if (point is not null) { XBox.Text = point.X.ToString(); YBox.Text = point.Y.ToString(); SetRunStatus("Ready", "Sẵn sàng", $"Selected {point.X} | {point.Y}", $"Đã chọn {point.X} | {point.Y}"); }
            else SetRunStatus("Point selection cancelled", "Đã hủy chọn điểm", "Choose a target and point", "Chọn cửa sổ đích và tọa độ");
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, Title, MessageBoxButton.OK, MessageBoxImage.Error); }
        finally { if (ReferenceEquals(_pickCts, operation)) { _pickCts.Dispose(); _pickCts = null; Opacity = 1; ApplyLanguage(); UpdateControls(); } }
    }
    private async void RunButton_Click(object sender, RoutedEventArgs e)
    {
        if (IsRunning) { _runCts!.Cancel(); return; }
        if (SelectedWindow is null || !TryReadSettings(out var x, out var y, out var interval, out var count, out var press)) return;
        _runCts = new(); SetRunStatus("Running", "Đang chạy", InfiniteBox.IsChecked == true ? "0 clicks" : $"0 / {count:N0}", InfiniteBox.IsChecked == true ? "0 lần nhấp" : $"0 / {count:N0}"); ApplyLanguage(); UpdateControls();
        try
        {
            await MiniForegroundInput.ActivateAsync(SelectedWindow.Handle, _runCts.Token);
            long completed = 0;
            var nextDue = Stopwatch.GetTimestamp();
            var resumedAfterPause = false;
            while (InfiniteBox.IsChecked == true || completed < count)
            {
                _runCts.Token.ThrowIfCancellationRequested();
                await MiniForegroundInput.SendClickAsync(SelectedWindow.Handle, x, y, false, press, focused => Dispatcher.Invoke(() =>
                {
                    if (focused) resumedAfterPause = true;
                    RunStateText.Text = focused ? (IsVi ? "Đang chạy" : "Running") : (IsVi ? "Tạm dừng" : "Paused");
                    ProgressText.Text = focused ? (IsVi ? _progressVi : _progressEn) : (IsVi ? "Đưa focus về cửa sổ đích để tiếp tục" : "Focus the selected target to continue");
                }), _runCts.Token);
                completed++;
                _progressEn = InfiniteBox.IsChecked == true ? $"{completed:N0} clicks" : $"{completed:N0} / {count:N0}";
                _progressVi = InfiniteBox.IsChecked == true ? $"{completed:N0} lần nhấp" : $"{completed:N0} / {count:N0}";
                ProgressText.Text = IsVi ? _progressVi : _progressEn;
                nextDue = resumedAfterPause ? Stopwatch.GetTimestamp() : nextDue;
                resumedAfterPause = false;
                nextDue += (long)(interval / 1000d * Stopwatch.Frequency);
                if (InfiniteBox.IsChecked == true || completed < count)
                {
                    var remaining = nextDue - Stopwatch.GetTimestamp();
                    if (remaining > 0) await MiniForegroundInput.DelayWithFocusAsync(SelectedWindow.Handle, (int)Math.Ceiling(remaining * 1000d / Stopwatch.Frequency), focused => Dispatcher.Invoke(() =>
                    {
                        if (focused) resumedAfterPause = true;
                        RunStateText.Text = focused ? (IsVi ? "Đang chạy" : "Running") : (IsVi ? "Tạm dừng" : "Paused");
                        ProgressText.Text = focused ? (IsVi ? _progressVi : _progressEn) : (IsVi ? "Đưa focus về cửa sổ đích để tiếp tục" : "Focus the selected target to continue");
                    }), _runCts.Token);
                }
            }
            SetRunStatus("Completed", "Hoàn tất", _progressEn, _progressVi);
        }
        catch (OperationCanceledException) { SetRunStatus("Stopped", "Đã dừng", _progressEn, _progressVi); }
        catch (Exception ex)
        {
            var english = "The run stopped because Windows rejected the target or physical mouse input. Check the target and administrator level, then try again.";
            var vietnamese = "Lượt chạy đã dừng vì Windows từ chối cửa sổ đích hoặc chuột vật lý. Hãy kiểm tra cửa sổ đích, quyền quản trị rồi thử lại.";
            SetRunStatus("Error", "Lỗi", english, vietnamese);
            MessageBox.Show($"{(IsVi ? vietnamese : english)}\n\n{(IsVi ? "Chi tiết kỹ thuật" : "Technical detail")}: {ex.Message}", Title, MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally { _runCts.Dispose(); _runCts = null; ApplyLanguage(); UpdateControls(); }
    }
    private bool TryReadSettings(out int x, out int y, out int interval, out long count, out int press)
    {
        x = y = interval = press = 0; count = 0;
        if (!int.TryParse(XBox.Text, out x) || x < 0) return Invalid(XBox, IsVi ? "X phải là số nguyên không âm." : "X must be a non-negative whole number.");
        if (!int.TryParse(YBox.Text, out y) || y < 0) return Invalid(YBox, IsVi ? "Y phải là số nguyên không âm." : "Y must be a non-negative whole number.");
        if (SelectedWindow is null || !MiniWindowService.TryPackClientPoint(SelectedWindow.Handle, x, y, out _)) return Invalid(XBox, IsVi ? "Tọa độ phải nằm trong vùng nội dung hiện tại của cửa sổ đích." : "The point must be inside the target's current client area.");
        if (!int.TryParse(IntervalBox.Text, out interval) || interval < 25 || interval > 3_600_000) return Invalid(IntervalBox, IsVi ? "Khoảng nghỉ phải từ 25 đến 3600000 ms." : "Interval must be from 25 to 3600000 ms.");
        if (!int.TryParse(PressBox.Text, out press) || press < 10 || press > 1000) return Invalid(PressBox, IsVi ? "Thời gian nhấn phải từ 10 đến 1000 ms." : "Press duration must be from 10 to 1000 ms.");
        if (InfiniteBox.IsChecked != true && (!long.TryParse(CountBox.Text, out count) || count < 1 || count > 100_000_000)) return Invalid(CountBox, IsVi ? "Số lần phải từ 1 đến 100000000." : "Repeat count must be from 1 to 100000000.");
        return true;
    }
    private bool Invalid(TextBox field, string message) { field.Focus(); field.SelectAll(); MessageBox.Show(message, Title, MessageBoxButton.OK, MessageBoxImage.Warning); return false; }
    private void InfiniteBox_Changed(object sender, RoutedEventArgs e) { if (CountBox is not null) CountBox.IsEnabled = InfiniteBox.IsChecked != true && !IsRunning; }
    private void LanguageBox_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (!IsLoaded || LanguageBox.SelectedItem is not ComboBoxItem item) return; _language = item.Tag?.ToString() == "vi" ? "vi" : "en"; ApplyLanguage(); }
    private void ApplyLanguage()
    {
        TitleText.Text = "One Click Mini"; SubtitleText.Text = IsVi ? "Focus một cửa sổ và lặp một điểm bằng chuột vật lý. Chạy hoặc dừng: CTRL+SHIFT+F9." : "Focus one target and repeat one point with the physical mouse. Start or stop: CTRL+SHIFT+F9."; TargetLabel.Text = IsVi ? "CỬA SỔ ĐÍCH" : "TARGET WINDOW"; RefreshButton.Content = IsVi ? "Làm mới" : "Refresh"; PickButton.Content = IsPicking ? (IsVi ? "Hủy" : "Cancel") : (IsVi ? "Chọn điểm" : "Pick point"); PointHelp.Text = IsVi ? "Tọa độ đi theo vùng nội dung của cửa sổ khi cửa sổ được di chuyển." : "Coordinates stay relative to the target client area when it moves."; IntervalLabel.Text = IsVi ? "KHOẢNG LẶP (MS)" : "INTERVAL (MS)"; CountLabel.Text = IsVi ? "SỐ LẦN LẶP" : "REPEAT COUNT"; PressLabel.Text = IsVi ? "THỜI GIAN NHẤN (MS)" : "PRESS (MS)"; InfiniteBox.Content = IsVi ? "Chạy đến khi dừng" : "Run until stopped"; CompatibilityText.Text = IsVi ? "Chỉ chạy phía trước. Công cụ sẽ focus cửa sổ đích và di chuyển con trỏ vật lý." : "Foreground only. This tool focuses the target and moves your physical pointer."; RunButton.Content = IsRunning ? (IsVi ? "Dừng" : "Stop") : (IsVi ? "Bắt đầu" : "Start");
        RunStateText.Text = IsVi ? _stateVi : _stateEn; ProgressText.Text = IsVi ? _progressVi : _progressEn;
        AutomationProperties.SetName(LanguageBox, IsVi ? "Ngôn ngữ giao diện" : "Interface language"); AutomationProperties.SetName(WindowBox, IsVi ? "Cửa sổ đích" : "Target window"); AutomationProperties.SetName(XBox, IsVi ? "Tọa độ X trong vùng nội dung" : "Client X coordinate"); AutomationProperties.SetName(YBox, IsVi ? "Tọa độ Y trong vùng nội dung" : "Client Y coordinate"); AutomationProperties.SetName(IntervalBox, IsVi ? "Khoảng lặp theo mili giây" : "Click interval in milliseconds"); AutomationProperties.SetName(CountBox, IsVi ? "Số lần lặp" : "Repeat count"); AutomationProperties.SetName(PressBox, IsVi ? "Thời gian giữ chuột vật lý" : "Physical mouse press duration"); AutomationProperties.SetName(RunStateText, IsVi ? "Trạng thái chạy" : "Run status"); AutomationProperties.SetName(ProgressText, IsVi ? "Tiến độ chạy" : "Run progress");
    }
    private void SetRunStatus(string stateEn, string stateVi, string progressEn, string progressVi) { _stateEn = stateEn; _stateVi = stateVi; _progressEn = progressEn; _progressVi = progressVi; RunStateText.Text = IsVi ? stateVi : stateEn; ProgressText.Text = IsVi ? progressVi : progressEn; }
    private void UpdateControls() { var enabled = !IsRunning && !IsPicking; WindowBox.IsEnabled = enabled; RefreshButton.IsEnabled = enabled; PickButton.IsEnabled = IsPicking || (enabled && SelectedWindow is not null); XBox.IsEnabled = YBox.IsEnabled = IntervalBox.IsEnabled = PressBox.IsEnabled = InfiniteBox.IsEnabled = enabled; CountBox.IsEnabled = enabled && InfiniteBox.IsChecked != true; RunButton.IsEnabled = IsRunning || (enabled && SelectedWindow is not null); }
    private void Window_Closing(object? sender, CancelEventArgs e) { _runCts?.Cancel(); _pickCts?.Cancel(); _hotkey.Dispose(); _picker.Dispose(); }
}
