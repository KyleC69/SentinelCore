// Solution: SentinelCore
// Project:   SentinelCore.UI
// File:         WpfDispatcherService.cs
// Author: Kyle L. Crowder
// Build Num:  083003

namespace SentinelCore.UI.Services;





/// <summary>
///     WPF implementation of <see cref="IDispatcherService" /> that delegates
///     to <see cref="Application.Current.Dispatcher" />.
/// </summary>
public sealed class WpfDispatcherService : IDispatcherService
{
    /// <inheritdoc />
    public bool CheckAccess()
    {
        return System.Windows.Application.Current.Dispatcher.CheckAccess();
    }








    /// <inheritdoc />
    public void Invoke(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        System.Windows.Application.Current.Dispatcher.Invoke(action);
    }
}
