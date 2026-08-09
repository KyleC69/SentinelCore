// Solution: SentinelCore
// Project:   SentinelCoreHost
// File:         AsyncRelayCommand.cs
// Author: Kyle L. Crowder
// Build Num:  080801



using System.Windows.Input;




namespace SentinelCoreHost.ViewModels;





/// <summary>
///     Async ICommand implementation that supports cancellation and safe exception handling.
/// </summary>
public sealed class AsyncRelayCommand : ICommand
{
    private readonly Func<bool> _canExecute;
    private CancellationTokenSource? _cts;
    private readonly Func<CancellationToken, Task> _execute;
    private readonly Action<Exception>? _onException;








    public AsyncRelayCommand(Func<CancellationToken, Task> execute, Func<bool> canExecute, Action<Exception>? onException = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute ?? throw new ArgumentNullException(nameof(canExecute));
        _onException = onException;
    }








    public bool IsRunning { get; private set; }








    public bool CanExecute(object? parameter)
    {
        return !IsRunning && _canExecute();
    }








    public event EventHandler? CanExecuteChanged;








    public async void Execute(object? parameter)
    {
        _cts?.Dispose();
        _cts = new CancellationTokenSource();

        IsRunning = true;
        RaiseCanExecuteChanged();

        try
        {
            await _execute(_cts.Token).ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            // Expected when the user cancels; do not surface as a crash.
        }
        catch (Exception ex)
        {
            _onException?.Invoke(ex);
        }
        finally
        {
            IsRunning = false;
            RaiseCanExecuteChanged();
        }
    }








    public void Cancel()
    {
        try
        {
            _cts?.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }
    }








    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}