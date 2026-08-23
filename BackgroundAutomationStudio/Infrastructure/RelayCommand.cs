using System.Windows.Input;

namespace BackgroundAutomationStudio.Infrastructure;

public sealed class RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null) : ICommand
{
    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? parameter) => canExecute?.Invoke(parameter) ?? true;
    public void Execute(object? parameter) => execute(parameter);
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

public sealed class AsyncRelayCommand(Func<object?, Task> execute, Predicate<object?>? canExecute = null) : ICommand
{
    private bool _isRunning;
    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? parameter) => !_isRunning && (canExecute?.Invoke(parameter) ?? true);
    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter)) return;
        _isRunning = true;
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        try { await execute(parameter); }
        finally { _isRunning = false; CanExecuteChanged?.Invoke(this, EventArgs.Empty); }
    }
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
