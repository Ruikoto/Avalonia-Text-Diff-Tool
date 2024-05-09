using System;
using System.Windows.Input;

namespace TextDiff_Demo.Utils;

public class RelayCommand : ICommand
{
    private readonly Predicate<object> _canExecute;
    private readonly Action<object> _execute;

    /// <summary>
    ///     Constructer takes Execute events to register in CommandManager.
    /// </summary>
    /// <param name="execute">Execute method as action.</param>
    public RelayCommand(Action<object> execute)
        : this(execute, null)
    {
        if (null == execute) throw new NotImplementedException("Not implemented");
        _execute = execute;
    }

    /// <summary>
    ///     Constructer takes Execute and CanExcecute events to register in CommandManager.
    /// </summary>
    /// <param name="execute">Execute method as action.</param>
    /// <param name="canExecute">CanExecute method as return bool type.</param>
    public RelayCommand(Action<object> execute, Predicate<object> canExecute)
    {
        try
        {
            if (null == execute)
            {
                _execute = null;
                throw new NotImplementedException("Not implemented");
            }

            _execute = execute;
            _canExecute = canExecute;
        }
        catch (Exception)
        {
        }
    }

    public event EventHandler? CanExecuteChanged;

    /// <summary>
    ///     Execute method.
    /// </summary>
    /// <param name="parameter">Method parameter.</param>
    public void Execute(object parameter)
    {
        _execute(parameter);
    }

    /// <summary>
    ///     CanExecute method.
    /// </summary>
    /// <param name="parameter">Method parameter.</param>
    /// <returns>Return true if can execute.</returns>
    public bool CanExecute(object parameter)
    {
        return _canExecute == null || _canExecute(parameter);
    }
}