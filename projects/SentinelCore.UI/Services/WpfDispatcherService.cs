// Solution: SentinelCore
// Project:   SentinelCore.UI
// File:         WpfDispatcherService.cs
// Author: Kyle L. Crowder
// Build Num:  091418



namespace SentinelCore.UI.Services;





/// <summary>
///     WPF implementation of <see cref="IDispatcherService" /> that delegates
///     to <see cref="System.Windows.Application.Current" />.
/// </summary>
public sealed class WpfDispatcherService : IDispatcherService
{
    /// <summary>
    ///     Gets the UI dispatcher, throwing a descriptive exception when the
    ///     application has not been created (e.g. unit-test hosts).
    /// </summary>
    private static System.Windows.Threading.Dispatcher Dispatcher
    {
        get =>
                System.Windows.Application.Current?.Dispatcher ?? throw new InvalidOperationException("No WPF Application is running; the dispatcher is unavailable. " + "Use a mocked IDispatcherService in unit tests.");
    }









    public bool CheckAccess()
    {
        return Dispatcher.CheckAccess();
    }









    public void Invoke(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        Dispatcher.Invoke(action);
    }









    public Task InvokeAsync(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        return Dispatcher.InvokeAsync(action).Task!;
    }









    public async Task InvokeAsync(Func<Task> func)
    {
        ArgumentNullException.ThrowIfNull(func);

        // Run the async function on the dispatcher and await the returned
        // DispatcherOperation<Task> directly (it exposes GetAwaiter), then
        // await the inner task so completion propagates to the caller.
        System.Windows.Threading.DispatcherOperation<Task> operation = Dispatcher.InvokeAsync(func)!;

        Task inner = await operation.Task!.ConfigureAwait(false);
        await inner.ConfigureAwait(false);
    }
}
