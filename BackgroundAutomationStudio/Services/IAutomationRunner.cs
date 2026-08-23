using BackgroundAutomationStudio.Models;

namespace BackgroundAutomationStudio.Services;

public interface IAutomationRunner
{
    bool IsRunning { get; }
    bool IsPaused { get; }
    event EventHandler<AutomationAction?>? CurrentActionChanged;
    event EventHandler<string>? StatusChanged;
    Task RunAsync(WindowTarget target, IReadOnlyList<AutomationAction> actions, PlaybackRunOptions options, CancellationToken cancellationToken = default);
    void Pause();
    void Resume();
    void Stop();
}
