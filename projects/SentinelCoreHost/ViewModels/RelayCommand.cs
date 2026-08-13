// Solution: SentinelCore
// Project:   SentinelCoreHost
// File:         RelayCommand.cs
// Author: Kyle L. Crowder
// Build Num:  081312



using System.Windows.Input;




namespace SentinelCoreHost.ViewModels;





/// <summary>
///     Minimal ICommand implementation for the host view-model.
/// </summary>
public sealed class RelayCommand : ICommand
{
    private readonly Func<bool> _canExecute;
    private readonly Action _execute;








    public RelayCommand(Action execute, Func<bool> canExecute)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute ?? throw new ArgumentNullException(nameof(canExecute));
    }








    public bool CanExecute(object? parameter)
    {
        return _canExecute();
    }








    public event EventHandler? CanExecuteChanged;








    public void Execute(object? parameter)
    {
        _execute();
    }








    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}