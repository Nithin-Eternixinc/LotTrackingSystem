using System.Windows.Input;

namespace LTS.UI.ViewModels;

// ── Standard RelayCommand (sync) ─────────────────────────
public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute; //field Stores method to run.
    private readonly Func<object?, bool>? _canExecute; //Method takes object, returns bool

    // parameterless usage: new RelayCommand(() => DoSomething())
    public RelayCommand(Action execute, Func<bool>? canExecute = null)
        : this(_ => execute(), canExecute == null ? null : _ => canExecute()) { }

    // parameterized usage: new RelayCommand(obj => DoSomething(obj))
    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter)
        => _canExecute?.Invoke(parameter) ?? true;

    public void Execute(object? parameter)
        => _execute(parameter);

    public event EventHandler? CanExecuteChanged;

    public void RaiseCanExecuteChanged()
        => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

// ── Generic RelayCommand (for CommandParameter binding) ───
public class RelayCommand<T> : ICommand
{
    private readonly Action<T?> _execute;
    private readonly Func<T?, bool>? _canExecute;

    public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter)
        => _canExecute?.Invoke((T?)parameter) ?? true;  

    public void Execute(object? parameter)
        => _execute((T?)parameter);

    public event EventHandler? CanExecuteChanged;

    public void RaiseCanExecuteChanged()
        => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

// ── AsyncRelayCommand (for async methods) ─────────────────
public class AsyncRelayCommand : ICommand
{
    private readonly Func<Task> _execute;
    private readonly Func<bool>? _canExecute;
    private bool _isExecuting;

    public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter)
        => !_isExecuting && (_canExecute?.Invoke() ?? true); //if command already running → button disabled

    public async void Execute(object? parameter) //runs when button clicked 
    {
        _isExecuting = true;  //button disable
        RaiseCanExecuteChanged();
        try
        {
            await _execute();
        }
        finally
        {
            _isExecuting = false;
            RaiseCanExecuteChanged();
        }
    }

    public event EventHandler? CanExecuteChanged;

    public void RaiseCanExecuteChanged()
        => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}