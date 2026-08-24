using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using BackgroundAutomationStudio.Infrastructure;
using BackgroundAutomationStudio.Models;
using BackgroundAutomationStudio.Services;

namespace BackgroundAutomationStudio.ViewModels;

public sealed class MainViewModel : ObservableObject, IDisposable
{
    private readonly IWindowManager _windowManager;
    private readonly WindowPickerService _windowPicker;
    private readonly RecorderService _recorder;
    private readonly IAutomationRunner _runner;
    private readonly ScriptParser _scriptParser;
    private readonly ProjectService _projectService;
    private readonly IDialogService _dialogs;
    private readonly CoordinateOverlayService _coordinateOverlay;
    private readonly WorkflowHistory _workflowHistory = new();
    private readonly DispatcherTimer _recordTimer;
    private AutomationProject _project = new();
    private AutomationAction? _selectedAction;
    private string _scriptText = string.Empty;
    private string _scriptErrors = string.Empty;
    private string _statusText = LocalizationService.Get("Ready");
    private string _recordingTime = "00:00:00";
    private bool _isModified;
    private bool _isPicking;
    private bool _isRecording;
    private bool _isRunning;
    private bool _isPaused;
    private bool _syncingScript;
    private bool _suspendWorkflowHistory;
    private string? _projectPath;
    private RecordChoice _recordChoice;
    private AutomationAction? _actionClipboard;

    public MainViewModel(IWindowManager windowManager, WindowPickerService windowPicker, RecorderService recorder, IAutomationRunner runner, ScriptParser scriptParser, ProjectService projectService, IDialogService dialogs, CoordinateOverlayService coordinateOverlay)
    {
        _windowManager = windowManager; _windowPicker = windowPicker; _recorder = recorder; _runner = runner; _scriptParser = scriptParser; _projectService = projectService; _dialogs = dialogs; _coordinateOverlay = coordinateOverlay;
        _recordTimer = new(DispatcherPriority.Normal) { Interval = TimeSpan.FromMilliseconds(250) };
        _recordTimer.Tick += (_, _) => RecordingTime = _recorder.Elapsed.ToString(@"hh\:mm\:ss");
        _runner.CurrentActionChanged += RunnerOnCurrentActionChanged;
        _runner.StatusChanged += (_, status) => Application.Current.Dispatcher.Invoke(() => StatusText = status);
        AttachCollection(_project.Actions);

        NewCommand = new AsyncRelayCommand(_ => NewAsync(), _ => !IsRecording && !IsRunning);
        OpenCommand = new AsyncRelayCommand(_ => OpenAsync(), _ => !IsRecording && !IsRunning);
        SaveCommand = new AsyncRelayCommand(_ => SaveAsync(false), _ => !IsRecording);
        SaveAsCommand = new AsyncRelayCommand(_ => SaveAsync(true), _ => !IsRecording);
        SelectWindowCommand = new AsyncRelayCommand(_ => SelectWindowAsync(), _ => !IsRecording && !IsRunning && !IsPicking);
        StartRecordCommand = new RelayCommand(_ => StartRecording(), _ => Project.Target is not null && !IsRecording && !IsRunning);
        StopRecordCommand = new RelayCommand(_ => StopRecording(), _ => IsRecording);
        CancelRecordCommand = new RelayCommand(_ => CancelRecording(), _ => IsRecording);
        RunCommand = new AsyncRelayCommand(_ => RunAsync(), _ => Project.Target is not null && Actions.Count > 0 && string.IsNullOrEmpty(ScriptErrors) && !IsRecording && !IsRunning);
        PauseCommand = new RelayCommand(_ => TogglePause(), _ => IsRunning);
        StopRunCommand = new RelayCommand(_ => _runner.Stop(), _ => IsRunning);
        EditCommand = new RelayCommand(_ => EditSelected(), _ => SelectedAction is not null && !IsRecording && !IsRunning);
        DeleteCommand = new RelayCommand(_ => DeleteSelected(), _ => SelectedAction is not null && !IsRecording && !IsRunning);
        ClearAllCommand = new RelayCommand(_ => ClearAll(), _ => Actions.Count > 0 && !IsRecording && !IsRunning);
        UndoCommand = new RelayCommand(_ => UndoWorkflow(), _ => _workflowHistory.CanUndo && !IsRecording && !IsRunning);
        RedoCommand = new RelayCommand(_ => RedoWorkflow(), _ => _workflowHistory.CanRedo && !IsRecording && !IsRunning);
        DuplicateCommand = new RelayCommand(_ => DuplicateSelected(), _ => SelectedAction is not null && !IsRecording && !IsRunning);
        CopyCommand = new RelayCommand(_ => CopySelected(), _ => SelectedAction is not null && !IsRecording && !IsRunning);
        CutCommand = new RelayCommand(_ => CutSelected(), _ => SelectedAction is not null && !IsRecording && !IsRunning);
        PasteCommand = new RelayCommand(_ => PasteAfterSelected(), _ => _actionClipboard is not null && !IsRecording && !IsRunning);
        ToggleEnabledCommand = new RelayCommand(_ => ToggleSelectedEnabled(), _ => SelectedAction is not null && !IsRecording && !IsRunning);
        MoveUpCommand = new RelayCommand(_ => MoveSelected(-1), _ => CanMove(-1));
        MoveDownCommand = new RelayCommand(_ => MoveSelected(1), _ => CanMove(1));
        InsertAboveCommand = new RelayCommand(p => AddAction((string?)p ?? "Wait", true), _ => SelectedAction is not null && !IsRecording && !IsRunning);
        InsertBelowCommand = new RelayCommand(p => AddAction((string?)p ?? "Wait", false), _ => SelectedAction is not null && !IsRecording && !IsRunning);
        AddActionCommand = new RelayCommand(p => AddAction((string?)p ?? "Wait", null), _ => !IsRecording && !IsRunning);
        ManageFunctionsCommand = new RelayCommand(_ => ManageFunctions(), _ => !IsRecording && !IsRunning);
        SyncScriptFromVisual();
        _workflowHistory.Reset(Actions);
    }

    public AutomationProject Project { get => _project; private set { if (SetProperty(ref _project, value)) { OnPropertyChanged(nameof(Actions)); OnPropertyChanged(nameof(ProjectTitle)); OnPropertyChanged(nameof(TargetSummary)); } } }
    public ObservableCollection<AutomationAction> Actions => Project.Actions;
    public ObservableCollection<AutomationFunction> Functions => Project.Functions;
    public AutomationAction? SelectedAction { get => _selectedAction; set { if (SetProperty(ref _selectedAction, value)) RaiseCommandStates(); } }
    public string ProjectTitle => Project.Name + (IsModified ? " *" : string.Empty);
    public string TargetSummary => Project.Target is null ? LocalizationService.Get("NoTarget") : $"{Project.Target.ProcessName} - {Project.Target.WindowTitle}";
    public int RepeatCount { get => Project.RepeatCount; set { var safe = Math.Clamp(value, 1, 1_000_000); if (Project.RepeatCount == safe) return; Project.RepeatCount = safe; OnPropertyChanged(); MarkModified(); } }
    public string RepeatMode { get => RepeatModes.Normalize(Project.RepeatMode); set { var safe = RepeatModes.Normalize(value); if (Project.RepeatMode == safe) return; Project.RepeatMode = safe; OnPropertyChanged(); MarkModified(); } }
    public int RepeatDurationMinutes { get => Project.RepeatDurationMinutes; set { var safe = Math.Clamp(value, 1, 10080); if (Project.RepeatDurationMinutes == safe) return; Project.RepeatDurationMinutes = safe; OnPropertyChanged(); MarkModified(); } }
    public string StopAtTime { get => Project.StopAtTime; set { var safe = value?.Trim() ?? string.Empty; if (Project.StopAtTime == safe) return; Project.StopAtTime = safe; OnPropertyChanged(); MarkModified(); } }
    public string ScriptText { get => _scriptText; set { if (!SetProperty(ref _scriptText, value) || _syncingScript) return; ApplyScript(value); } }
    public string ScriptErrors { get => _scriptErrors; private set => SetProperty(ref _scriptErrors, value); }
    public string StatusText { get => _statusText; private set => SetProperty(ref _statusText, value); }
    public string RecordingTime { get => _recordingTime; private set => SetProperty(ref _recordingTime, value); }
    public bool IsModified { get => _isModified; private set { if (SetProperty(ref _isModified, value)) OnPropertyChanged(nameof(ProjectTitle)); } }
    public bool IsPicking { get => _isPicking; private set { if (SetProperty(ref _isPicking, value)) RaiseCommandStates(); } }
    public bool IsRecording { get => _isRecording; private set { if (SetProperty(ref _isRecording, value)) { RaiseCommandStates(); SyncCoordinateOverlay(); } } }
    public bool IsRunning { get => _isRunning; private set { if (SetProperty(ref _isRunning, value)) { RaiseCommandStates(); SyncCoordinateOverlay(); } } }
    public bool IsPaused { get => _isPaused; private set { if (SetProperty(ref _isPaused, value)) OnPropertyChanged(nameof(PauseButtonText)); } }
    public string PauseButtonText => IsPaused ? LocalizationService.Get("Resume") : LocalizationService.Get("Pause");
    public bool HasTarget => Project.Target is not null;
    public bool ShowCoordinateMap { get => Project.ShowCoordinateMap; set { if (Project.ShowCoordinateMap == value) return; Project.ShowCoordinateMap = value; OnPropertyChanged(); MarkModified(); SyncCoordinateOverlay(); } }
    public bool ShowCoordinateGrid { get => Project.ShowCoordinateGrid; set { if (Project.ShowCoordinateGrid == value) return; Project.ShowCoordinateGrid = value; OnPropertyChanged(); MarkModified(); SyncCoordinateOverlay(); } }
    public string MarkerColor { get => Project.MarkerColor; set { var safe = value is "#FF727E" or "#55D6A0" or "#F4B860" or "#B69CFF" ? value : "#74A7FF"; if (Project.MarkerColor == safe) return; Project.MarkerColor = safe; OnPropertyChanged(); MarkModified(); SyncCoordinateOverlay(); } }
    public string MarkerShape { get => MarkerShapes.Normalize(Project.MarkerShape); set { var safe = MarkerShapes.Normalize(value); if (Project.MarkerShape == safe) return; Project.MarkerShape = safe; OnPropertyChanged(); MarkModified(); SyncCoordinateOverlay(); } }

    public ICommand NewCommand { get; }
    public ICommand OpenCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand SaveAsCommand { get; }
    public ICommand SelectWindowCommand { get; }
    public ICommand StartRecordCommand { get; }
    public ICommand StopRecordCommand { get; }
    public ICommand CancelRecordCommand { get; }
    public ICommand RunCommand { get; }
    public ICommand PauseCommand { get; }
    public ICommand StopRunCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand ClearAllCommand { get; }
    public ICommand UndoCommand { get; }
    public ICommand RedoCommand { get; }
    public ICommand DuplicateCommand { get; }
    public ICommand CopyCommand { get; }
    public ICommand CutCommand { get; }
    public ICommand PasteCommand { get; }
    public ICommand ToggleEnabledCommand { get; }
    public ICommand MoveUpCommand { get; }
    public ICommand MoveDownCommand { get; }
    public ICommand InsertAboveCommand { get; }
    public ICommand InsertBelowCommand { get; }
    public ICommand AddActionCommand { get; }
    public ICommand ManageFunctionsCommand { get; }

    public void MoveAction(int oldIndex, int newIndex)
    {
        if (oldIndex < 0 || newIndex < 0 || oldIndex >= Actions.Count || newIndex >= Actions.Count || oldIndex == newIndex) return;
        Actions.Move(oldIndex, newIndex); SelectedAction = Actions[newIndex];
    }

    public void ToggleRunFromHotkey()
    {
        if (IsRunning) { _runner.Stop(); return; }
        if (RunCommand.CanExecute(null)) RunCommand.Execute(null);
        else StatusText = LocalizationService.Language == "vi" ? "Không thể chạy - hãy chọn cửa sổ đích và thêm ít nhất một thao tác" : "Cannot run - select a target window and add at least one action";
    }

    public void TogglePauseFromHotkey()
    {
        if (IsRunning) { TogglePause(); return; }
        StatusText = LocalizationService.Language == "vi" ? "Không có quy trình đang chạy để tạm dừng" : "No running workflow to pause";
    }

    public void RefreshLanguage()
    {
        OnPropertyChanged(nameof(TargetSummary)); OnPropertyChanged(nameof(PauseButtonText));
        StatusText = LocalizationService.Get("Ready");
    }

    public async Task<bool> TryCloseAsync()
    {
        if (!IsModified) return true;
        var result = MessageBox.Show($"Save changes to {Project.Name} before closing?", "Unsaved changes", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
        if (result == MessageBoxResult.Cancel) return false;
        return result != MessageBoxResult.Yes || await SaveAsync(false);
    }

    private void ApplyScript(string value)
    {
        var result = _scriptParser.Parse(value);
        ScriptErrors = string.Join(Environment.NewLine, result.Errors);
        if (!result.IsValid) { RaiseCommandStates(); return; }
        _syncingScript = true;
        try { ReplaceActions(result.Actions); IsModified = true; StatusText = "Script applied to visual workflow"; }
        finally { _syncingScript = false; }
    }

    private async Task NewAsync()
    {
        if (!await ConfirmContinueAsync()) return;
        DetachCollection(Actions); Project = new AutomationProject(); AttachCollection(Actions);
        _projectPath = null; SelectedAction = null; IsModified = false; ScriptErrors = string.Empty; SyncScriptFromVisual();
        _workflowHistory.Reset(Actions);
        NotifyScheduleChanged(); OnPropertyChanged(nameof(HasTarget)); StatusText = LocalizationService.Language == "vi" ? "Đã tạo dự án mới - Chọn cửa sổ đích" : "New project created - Select a target window";
    }

    private async Task OpenAsync()
    {
        if (!await ConfirmContinueAsync()) return;
        var path = _dialogs.OpenProject(); if (path is null) return;
        try
        {
            var loaded = await _projectService.LoadAsync(path); if (loaded.Target is not null) _windowManager.Resolve(loaded.Target);
            DetachCollection(Actions); Project = loaded; AttachCollection(Actions);
            _projectPath = path; IsModified = false; SelectedAction = null; SyncScriptFromVisual(); OnPropertyChanged(nameof(HasTarget)); OnPropertyChanged(nameof(TargetSummary)); NotifyScheduleChanged();
            _workflowHistory.Reset(Actions); RaiseCommandStates();
            StatusText = $"Opened {Path.GetFileName(path)}";
        }
        catch (Exception ex) { _dialogs.Error("Could not open project", ex.Message); }
    }

    private async Task<bool> SaveAsync(bool saveAs)
    {
        var path = saveAs || string.IsNullOrWhiteSpace(_projectPath) ? _dialogs.SaveProject(Project.Name) : _projectPath;
        if (path is null) return false;
        try { Project.Name = Path.GetFileNameWithoutExtension(path); await _projectService.SaveAsync(Project, path); _projectPath = path; IsModified = false; OnPropertyChanged(nameof(ProjectTitle)); StatusText = $"Saved {Path.GetFileName(path)}"; return true; }
        catch (Exception ex) { _dialogs.Error("Could not save project", ex.Message); return false; }
    }

    private async Task<bool> ConfirmContinueAsync()
    {
        if (!IsModified) return true;
        var result = MessageBox.Show($"Save changes to {Project.Name} before continuing?", "Unsaved changes", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
        if (result == MessageBoxResult.Cancel) return false;
        return result != MessageBoxResult.Yes || await SaveAsync(false);
    }

    private async Task SelectWindowAsync()
    {
        IsPicking = true; StatusText = LocalizationService.Language == "vi" ? "Nhấp vào một cửa sổ ứng dụng đang hiển thị để chọn..." : "Click any visible application window to select it...";
        try
        {
            var owner = Application.Current.MainWindow; var hwnd = new WindowInteropHelper(owner).Handle; owner.Opacity = .25;
            var target = await _windowPicker.PickWindowAsync(hwnd); owner.Opacity = 1; if (target is null) return;
            if (Project.Target is { RecordedWidth: > 0 } existing && string.Equals(existing.ProcessName, target.ProcessName, StringComparison.OrdinalIgnoreCase) && string.Equals(existing.WindowClassName, target.WindowClassName, StringComparison.Ordinal))
            { target.RecordedX = existing.RecordedX; target.RecordedY = existing.RecordedY; target.RecordedWidth = existing.RecordedWidth; target.RecordedHeight = existing.RecordedHeight; _windowManager.RestoreLayout(target, new IntPtr(target.LastKnownHwnd)); }
            Project.Target = target; OnPropertyChanged(nameof(Project)); OnPropertyChanged(nameof(TargetSummary)); OnPropertyChanged(nameof(HasTarget)); MarkModified(); SyncCoordinateOverlay(); StatusText = LocalizationService.Language == "vi" ? "Đã chọn cửa sổ đích và ghi nhận bố cục" : "Target selected and layout captured";
        }
        catch (Exception ex) { Application.Current.MainWindow.Opacity = 1; _dialogs.Error("Window selection failed", ex.Message); StatusText = "Window selection cancelled"; }
        finally { IsPicking = false; }
    }

    private void StartRecording()
    {
        if (Project.Target is null) return;
        _recordChoice = Actions.Count == 0 ? RecordChoice.Replace : _dialogs.ChooseRecordMode(); if (_recordChoice == RecordChoice.Cancel) return;
        var hwnd = _windowManager.Resolve(Project.Target); if (hwnd == IntPtr.Zero) { _dialogs.Error("Target not found", "Open the target application or select its window again."); return; }
        try
        {
            if (Actions.Count == 0 && _recordChoice == RecordChoice.Replace)
            {
                var current = _windowManager.GetTarget(hwnd);
                Project.Target.RecordedX = current.RecordedX; Project.Target.RecordedY = current.RecordedY;
                Project.Target.RecordedWidth = current.RecordedWidth; Project.Target.RecordedHeight = current.RecordedHeight;
                OnPropertyChanged(nameof(Project)); MarkModified();
            }
            _windowManager.RestoreLayout(Project.Target, hwnd); _windowManager.Activate(hwnd); _recorder.Start(hwnd);
            IsRecording = true; RecordingTime = "00:00:00"; _recordTimer.Start(); StatusText = LocalizationService.Language == "vi" ? "Đang ghi - thao tác trên cửa sổ khác sẽ bị bỏ qua" : "Recording - actions on other windows are ignored";
        }
        catch (Exception ex) { _dialogs.Error("Could not start recording", ex.Message); }
    }

    private void StopRecording()
    {
        var recorded = _recorder.Stop(); _recordTimer.Stop(); IsRecording = false;
        if (_recordChoice == RecordChoice.Replace) ReplaceActions(recorded);
        else
        {
            _suspendWorkflowHistory = true;
            try { foreach (var action in recorded) Actions.Add(action); }
            finally { _suspendWorkflowHistory = false; }
            CaptureWorkflowHistory();
        }
        MarkModified(); StatusText = LocalizationService.Language == "vi" ? $"Đã dừng ghi - nhận {recorded.Count} thao tác" : $"Recording stopped - {recorded.Count} actions captured";
    }

    private void CancelRecording() { _recorder.Cancel(); _recordTimer.Stop(); IsRecording = false; RecordingTime = "00:00:00"; StatusText = LocalizationService.Language == "vi" ? "Đã hủy ghi - quy trình trước đó được giữ nguyên" : "Recording cancelled - previous workflow preserved"; }
    private async Task RunAsync()
    {
        if (Project.Target is null) return;
        IsRunning = true;
        IsPaused = false;
        try { await _runner.RunAsync(Project.Target, WorkflowFunctionExpander.Expand(Actions, Functions), CreateRunOptions()); }
        catch (Exception ex) { _dialogs.Error(LocalizationService.Language == "vi" ? "Đã dừng quy trình" : "Workflow stopped", ex.Message); StatusText = LocalizationService.Language == "vi" ? "Quy trình thất bại" : "Workflow failed"; }
        finally { IsRunning = false; IsPaused = false; }
    }

    private PlaybackRunOptions CreateRunOptions()
    {
        var mode = RepeatModes.Normalize(RepeatMode);
        if (mode == RepeatModes.Count) return PlaybackRunOptions.Count(RepeatCount);
        if (mode == RepeatModes.Infinite) return new(mode, RepeatCount, TimeSpan.Zero, null);
        if (mode == RepeatModes.Duration) return new(mode, RepeatCount, TimeSpan.FromMinutes(RepeatDurationMinutes), null);
        if (!TimeSpan.TryParseExact(StopAtTime, [@"h\:mm", @"hh\:mm"], CultureInfo.InvariantCulture, out var clock) || clock >= TimeSpan.FromDays(1))
            throw new InvalidOperationException(LocalizationService.Language == "vi" ? "Giờ dừng không hợp lệ. Hãy nhập theo dạng HH:mm, ví dụ 23:30." : "Invalid stop time. Use HH:mm, for example 23:30.");
        return new(mode, RepeatCount, TimeSpan.Zero, PlaybackRunOptions.GetNextStopAt(clock, DateTimeOffset.Now));
    }
    private void TogglePause() { if (IsPaused) { _runner.Resume(); IsPaused = false; } else { _runner.Pause(); IsPaused = true; } }

    private void AddAction(string type, bool? above)
    {
        AutomationAction action = type switch { "Click" => new ClickAction(), "RightClick" => new RightClickAction(), "DoubleClick" => new DoubleClickAction(), "Drag" => new BackgroundAutomationStudio.Models.DragAction(), "Scroll" => new ScrollAction(), "MovePointer" => new MovePointerAction(), "WaitForImage" => new WaitForImageAction(), "ClickImage" => new ClickImageAction(), "CallFunction" => new CallFunctionAction(), "TypeText" => new TypeTextAction(), "KeyPress" => new KeyPressAction(), "KeyHold" => new KeyHoldAction(), _ => new WaitAction() };
        if (action is CallFunctionAction && Functions.Count == 0)
        {
            StatusText = LocalizationService.Language == "vi" ? "Hãy tạo ít nhất một hàm dùng lại trước khi thêm bước CALL" : "Create at least one reusable function before adding a CALL step";
            if (!ManageFunctions() || Functions.Count == 0) return;
        }
        var edited = _dialogs.EditAction(action, Project.Target, Functions, true); if (edited is null) return;
        var index = above is null || SelectedAction is null ? Actions.Count : Actions.IndexOf(SelectedAction) + (above.Value ? 0 : 1); Actions.Insert(index, edited); SelectedAction = edited;
    }

    private void EditSelected() { if (SelectedAction is null) return; var edited = _dialogs.EditAction(SelectedAction, Project.Target, Functions); if (edited is null) return; var index = Actions.IndexOf(SelectedAction); Actions[index] = edited; SelectedAction = edited; }
    private bool ManageFunctions()
    {
        var edited = _dialogs.ManageFunctions(Functions, Actions); if (edited is null) return false;
        Project.Functions = new(edited.Select(item => item.Clone())); OnPropertyChanged(nameof(Functions));
        foreach (var call in Actions.OfType<CallFunctionAction>())
        {
            var match = Functions.FirstOrDefault(item => item.Id == call.FunctionId) ?? Functions.FirstOrDefault(item => item.Name.Equals(call.FunctionName, StringComparison.OrdinalIgnoreCase));
            if (match is not null) { call.FunctionId = match.Id; call.FunctionName = match.Name; }
        }
        MarkModified(); SyncScriptFromVisual(); CaptureWorkflowHistory(); RaiseCommandStates();
        SyncCoordinateOverlay();
        StatusText = LocalizationService.Language == "vi" ? $"Đã cập nhật {Functions.Count} hàm dùng lại" : $"Updated {Functions.Count} reusable function(s)";
        return true;
    }
    private void DeleteSelected() { if (SelectedAction is null) return; var index = Actions.IndexOf(SelectedAction); Actions.Remove(SelectedAction); SelectedAction = Actions.Count == 0 ? null : Actions[Math.Min(index, Actions.Count - 1)]; }
    private void ClearAll()
    {
        var message = LocalizationService.Language == "vi" ? "Xóa toàn bộ thao tác trong quy trình? Hành động này không thể hoàn tác." : "Delete every action in this workflow? This cannot be undone.";
        var title = LocalizationService.Language == "vi" ? "Xóa tất cả thao tác" : "Clear all actions";
        if (MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        Actions.Clear();
        SelectedAction = null;
        StatusText = LocalizationService.Language == "vi" ? "Đã xóa toàn bộ thao tác" : "All workflow actions were cleared";
    }
    private void UndoWorkflow() { if (_workflowHistory.TryUndo(out var actions)) RestoreWorkflowSnapshot(actions, LocalizationService.Language == "vi" ? "Đã quay lại thay đổi trước" : "Undid workflow change"); }
    private void RedoWorkflow() { if (_workflowHistory.TryRedo(out var actions)) RestoreWorkflowSnapshot(actions, LocalizationService.Language == "vi" ? "Đã áp dụng lại thay đổi" : "Redid workflow change"); }
    private void RestoreWorkflowSnapshot(IEnumerable<AutomationAction> actions, string status)
    {
        _suspendWorkflowHistory = true;
        try
        {
            DetachCollection(Actions);
            Project.Actions = new(actions);
            OnPropertyChanged(nameof(Actions));
            AttachCollection(Actions);
            SelectedAction = null;
            SyncScriptFromVisual();
            IsModified = true;
            StatusText = status;
        }
        finally { _suspendWorkflowHistory = false; }
        RaiseCommandStates();
    }
    private void DuplicateSelected() { if (SelectedAction is null) return; var clone = SelectedAction.Clone(); var index = Actions.IndexOf(SelectedAction) + 1; Actions.Insert(index, clone); SelectedAction = clone; }
    private void CopySelected() { if (SelectedAction is null) return; _actionClipboard = SelectedAction.Clone(); StatusText = LocalizationService.Language == "vi" ? "Đã sao chép thao tác" : "Action copied"; RaiseCommandStates(); }
    private void CutSelected() { if (SelectedAction is null) return; CopySelected(); DeleteSelected(); StatusText = LocalizationService.Language == "vi" ? "Đã cắt thao tác - nhấn Ctrl+V để dán" : "Action cut - press Ctrl+V to paste"; }
    private void PasteAfterSelected()
    {
        if (_actionClipboard is null) return;
        var pasted = _actionClipboard.Clone();
        var index = SelectedAction is null ? Actions.Count : Actions.IndexOf(SelectedAction) + 1;
        Actions.Insert(index, pasted); SelectedAction = pasted;
        StatusText = LocalizationService.Language == "vi" ? "Đã dán thao tác" : "Action pasted";
    }
    private void ToggleSelectedEnabled()
    {
        if (SelectedAction is null) return;
        SelectedAction.Enabled = !SelectedAction.Enabled;
        StatusText = LocalizationService.Language == "vi" ? (SelectedAction.Enabled ? "Đã bật thao tác" : "Đã bỏ qua thao tác") : (SelectedAction.Enabled ? "Action enabled" : "Action skipped");
    }
    private bool CanMove(int delta) => SelectedAction is not null && !IsRecording && !IsRunning && Actions.IndexOf(SelectedAction) + delta >= 0 && Actions.IndexOf(SelectedAction) + delta < Actions.Count;
    private void MoveSelected(int delta) { if (SelectedAction is not null) MoveAction(Actions.IndexOf(SelectedAction), Actions.IndexOf(SelectedAction) + delta); }

    private void ReplaceActions(IEnumerable<AutomationAction> actions) { DetachCollection(Actions); Project.Actions = new(actions); OnPropertyChanged(nameof(Actions)); AttachCollection(Actions); SelectedAction = null; SyncScriptFromVisual(); CaptureWorkflowHistory(); RaiseCommandStates(); }
    private void AttachCollection(ObservableCollection<AutomationAction> actions) { actions.CollectionChanged += ActionsOnCollectionChanged; foreach (var action in actions) action.PropertyChanged += ActionOnPropertyChanged; }
    private void DetachCollection(ObservableCollection<AutomationAction> actions) { actions.CollectionChanged -= ActionsOnCollectionChanged; foreach (var action in actions) action.PropertyChanged -= ActionOnPropertyChanged; }
    private void ActionsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) { if (e.OldItems is not null) foreach (AutomationAction action in e.OldItems) action.PropertyChanged -= ActionOnPropertyChanged; if (e.NewItems is not null) foreach (AutomationAction action in e.NewItems) action.PropertyChanged += ActionOnPropertyChanged; MarkModified(); SyncScriptFromVisual(); CaptureWorkflowHistory(); RaiseCommandStates(); SyncCoordinateOverlay(); }
    private void ActionOnPropertyChanged(object? sender, PropertyChangedEventArgs e) { if (e.PropertyName == nameof(AutomationAction.IsCurrent)) return; MarkModified(); SyncScriptFromVisual(); CaptureWorkflowHistory(); SyncCoordinateOverlay(); }
    private void SyncScriptFromVisual() { if (_syncingScript) return; _syncingScript = true; try { _scriptText = _scriptParser.Serialize(Actions); OnPropertyChanged(nameof(ScriptText)); ScriptErrors = string.Empty; } finally { _syncingScript = false; } }
    private void MarkModified() { if (!_syncingScript) IsModified = true; }
    private void CaptureWorkflowHistory() { if (!_suspendWorkflowHistory && _workflowHistory.Capture(Actions)) RaiseCommandStates(); }
    private void RunnerOnCurrentActionChanged(object? sender, AutomationAction? current) => Application.Current.Dispatcher.Invoke(() => { foreach (var action in Actions) action.IsCurrent = action.Id == current?.Id; });
    private void NotifyScheduleChanged() { OnPropertyChanged(nameof(RepeatMode)); OnPropertyChanged(nameof(RepeatCount)); OnPropertyChanged(nameof(RepeatDurationMinutes)); OnPropertyChanged(nameof(StopAtTime)); OnPropertyChanged(nameof(ShowCoordinateMap)); OnPropertyChanged(nameof(ShowCoordinateGrid)); OnPropertyChanged(nameof(MarkerColor)); OnPropertyChanged(nameof(MarkerShape)); OnPropertyChanged(nameof(Functions)); SyncCoordinateOverlay(); }
    private void SyncCoordinateOverlay()
    {
        IReadOnlyList<AutomationAction> visibleActions;
        try { visibleActions = WorkflowFunctionExpander.Expand(Actions, Functions); }
        catch { visibleActions = Actions.ToList(); }
        _coordinateOverlay.Configure(ShowCoordinateMap && !IsRecording && !IsRunning, Project.Target, visibleActions, ShowCoordinateGrid, MarkerColor, MarkerShape);
    }
    private void RaiseCommandStates() { foreach (var command in new ICommand[] { NewCommand, OpenCommand, SaveCommand, SaveAsCommand, SelectWindowCommand, StartRecordCommand, StopRecordCommand, CancelRecordCommand, RunCommand, PauseCommand, StopRunCommand, EditCommand, DeleteCommand, ClearAllCommand, UndoCommand, RedoCommand, DuplicateCommand, CopyCommand, CutCommand, PasteCommand, ToggleEnabledCommand, MoveUpCommand, MoveDownCommand, InsertAboveCommand, InsertBelowCommand, AddActionCommand, ManageFunctionsCommand }) if (command is RelayCommand relay) relay.RaiseCanExecuteChanged(); else if (command is AsyncRelayCommand asyncRelay) asyncRelay.RaiseCanExecuteChanged(); }
    public void Dispose() { _recordTimer.Stop(); _coordinateOverlay.Dispose(); _recorder.Dispose(); _runner.Stop(); if (_runner is IDisposable disposable) disposable.Dispose(); _windowPicker.Dispose(); }
}
