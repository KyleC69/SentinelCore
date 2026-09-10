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
    ///     Blocks the calling thread until the action completes — prefer
    ///     <see cref="InvokeAsync(Action)" /> for anything that may run on a background thread.
    /// </summary>
    /// <param name="action">The action to invoke on the dispatcher thread.</param>
    void Invoke(Action action);





    /// <summary>
    ///     Executes the specified <paramref name="action" /> asynchronously on the dispatcher thread
    ///     without blocking the calling thread.
    /// </summary>
    /// <param name="action">The action to invoke on the dispatcher thread.</param>
    /// <returns>A task that completes when the action has run on the dispatcher thread.</returns>
    Task InvokeAsync(Action action);





    /// <summary>
    ///     Executes the specified <paramref name="func" /> asynchronously on the dispatcher thread
    ///     and proxies its returned task so awaiting completes only when the inner work finishes.
    /// </summary>
    /// <param name="func">An async function to invoke on the dispatcher thread.</param>
    /// <returns>A task that completes when the inner async function completes.</returns>
    Task InvokeAsync(Func<Task> func);
}