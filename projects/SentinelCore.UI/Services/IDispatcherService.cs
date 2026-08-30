// Solution: SentinelCore
// Project:   SentinelCore.UI
// File:         IDispatcherService.cs
// Author: Kyle L. Crowder
// Build Num:  083003



namespace SentinelCore.UI.Services;





/// <summary>
///     Abstraction over the WPF dispatcher to decouple ViewModels
///     from <see cref="System.Windows.Application.Current" />.
///     This enables unit testing of ViewModels without a running WPF application.
/// </summary>
public interface IDispatcherService
{
    /// <summary>
    ///     Determines whether the calling thread has access to the dispatcher thread.
    /// </summary>
    /// <returns><c>true</c> if the calling thread is the dispatcher thread; otherwise, <c>false</c>.</returns>
    bool CheckAccess();








    /// <summary>
    ///     Executes the specified <paramref name="action" /> synchronously on the dispatcher thread.
    /// </summary>
    /// <param name="action">The action to invoke on the dispatcher thread.</param>
    void Invoke(Action action);
}