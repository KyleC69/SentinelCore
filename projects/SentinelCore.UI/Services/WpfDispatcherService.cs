// Solution: SentinelCore
// Project:   SentinelCore.UI
// File:         WpfDispatcherService.cs
// Author: Kyle L. Crowder
// Build Num:  083003

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
        get
        {
            return (System.Windows.Application.Current?.Dispatcher)
                ?? throw new InvalidOperationException(
                    "No WPF Application is running; the dispatcher is unavailable. " +
                    "Use a mocked IDispatcherService in unit tests.");
        }
    }

    /// <inheritdoc />
    public bool CheckAccess()
    {
        return Dispatcher.CheckAccess();
    }





    /// <inheritdoc />
    public void Invoke(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        Dispatcher.Invoke(action);
    }





    /// <inheritdoc />
    public Task InvokeAsync(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        return Dispatcher.InvokeAsync(action).Task;
    }





    /// <inheritdoc />
    public async Task InvokeAsync(Func<Task> func)
    {
        ArgumentNullException.ThrowIfNull(func);

        // Run the async function on the dispatcher, then await its inner task
        // on the caller's context so completion propagates correctly.
        Task inner = await Dispatcher.InvokeAsync(func).Task.Unwrap().ConfigureAwait(false);
        await inner.ConfigureAwait(false);
    }
}
