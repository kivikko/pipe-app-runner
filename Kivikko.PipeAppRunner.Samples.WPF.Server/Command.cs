using System;
using System.Diagnostics;
using System.Windows.Input;

namespace Kivikko.PipeAppRunner.Samples.WPF.Server;

public class Command : ICommand
{
    public Command() { }
    public Command(Action execute, Func<bool>? canExecute = null)
    {
        CommandAction = execute;
        CanExecuteFunc = canExecute ?? (() => true);
    }

    public Action? CommandAction { set; get; }
    public Func<bool>? CanExecuteFunc { set; get; }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => CanExecuteFunc == null || CanExecuteFunc();
    
    public void Execute(object? parameter)
    {
        try
        {
            CommandAction?.Invoke();
        }
        catch (Exception exception)
        {
            Debug.WriteLine(exception);
        }
    }
}