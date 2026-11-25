using System;
using System.Windows.Input;

namespace AiEcoSys.Desktop;

/// <summary>
/// Delegate command implementation that can be used to bind UI actions to methods.
/// </summary>
public class DelegateCommand : ICommand
{
    private Action<object?> _execute;
    private Func<object?, bool>? _canExecute;
    private EventHandler? canExecuteChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="DelegateCommand"/> class.
    /// </summary>
    /// <param name="execute">The execution action (takes object? parameter).</param>
    /// <param name="canExecute">The can execute predicate (takes object? parameter).</param>
    public DelegateCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
        if (_canExecute == null) _canExecute = _ => true;
    }

    /// <summary>
    /// Occurs when changes occur that affect whether the command should execute.
    /// </summary>
    public event EventHandler? CanExecuteChanged
    {
        add => canExecuteChanged += value;
        remove => canExecuteChanged -= value;
    }

    /// <inheritdoc />
    public bool CanExecute(object? parameter)
    {
        if (_canExecute == null) return true;
        return _canExecute!(parameter!);
    }

    /// <inheritdoc />
    public void Execute(object? parameter) => _execute!(parameter!);

    /// <summary>
    /// Raises the <see cref="CanExecuteChanged"/> event.
    /// </summary>
    public void RaiseCanExecuteChanged()
    {
        canExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}

/// <summary>
/// Strongly-typed delegate command.
/// </summary>
public class DelegateCommand<T> : ICommand where T : notnull
{
    private Action<T> _execute;
    private Func<T, bool>? _canExecute;
    private EventHandler? canExecuteChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="DelegateCommand{T}"/> class.
    /// </summary>
    /// <param name="execute">The execution action.</param>
    /// <param name="canExecute">The can execute predicate.</param>
    public DelegateCommand(Action<T> execute, Func<T, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
        if (_canExecute == null) _canExecute = _ => true;
    }

    /// <summary>
    /// Occurs when changes occur that affect whether the command should execute.
    /// </summary>
    public event EventHandler? CanExecuteChanged
    {
        add => canExecuteChanged += value;
        remove => canExecuteChanged -= value;
    }

    /// <inheritdoc />
    public bool CanExecute(object? parameter) => parameter is T t && (_canExecute ?? (_canExecute = _ => true))(t);

    /// <inheritdoc />
    public void Execute(object? parameter) => _execute!((T)parameter!);

    /// <summary>
    /// Raises the <see cref="CanExecuteChanged"/> event.
    /// </summary>
    public void RaiseCanExecuteChanged()
    {
        canExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}