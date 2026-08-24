using System.Windows;
using BackgroundAutomationStudio.Services;
using BackgroundAutomationStudio.ViewModels;
using OpenCvSharp;

namespace BackgroundAutomationStudio;

public partial class App : Application
{
    private MainViewModel? _mainViewModel;
    private SettingsService? _settings;
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        try { _ = Cv2.GetVersionString(); }
        catch (Exception ex)
        {
            MessageBox.Show($"The visual matching engine could not start.\n\n{ex.Message}", "Startup failed", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
            return;
        }
        _settings = new SettingsService(); _settings.Load(); LocalizationService.Apply(_settings.Current.Language);
        var windowManager = new WindowManager(); var picker = new WindowPickerService(windowManager); var recorder = new RecorderService(windowManager, () => _settings.Current.PlaybackMode); var runner = new BackgroundAutomationRunner(windowManager, () => _settings.Current.PlaybackMode, () => _settings.Current.GamePressDurationMilliseconds); var dialogs = new DialogService(picker, windowManager); var coordinateOverlay = new CoordinateOverlayService(windowManager);
        _mainViewModel = new MainViewModel(windowManager, picker, recorder, runner, new ScriptParser(), new ProjectService(), dialogs, coordinateOverlay);
        var window = new MainWindow(_mainViewModel, _settings); MainWindow = window; window.Show();
    }
    protected override void OnExit(ExitEventArgs e) { _mainViewModel?.Dispose(); base.OnExit(e); }
}
