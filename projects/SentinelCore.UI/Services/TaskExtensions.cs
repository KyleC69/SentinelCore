// Solution: SentinelCore
// Project:   SentinelCore.UI
// File:         TaskExtensions.cs
// Author: Kyle L. Crowder
// Build Num:  110813



namespace SentinelCore.UI.Services;





/// <summary>
///     Provides extension methods that place an upper bound on how long a caller will wait for an
///     asynchronous operation before treating it as failed.
/// </summary>
/// <remarks>
///     These are thin adapters over <see cref="Task.WaitAsync(TimeSpan, CancellationToken)" />; they exist so
///     call sites state a timeout declaratively instead of hand-rolling
///     <see cref="CancellationTokenSource" /> lifetimes for every awaited operation.
/// </remarks>
public static class TaskExtensions
{








    /// <summary>
    ///     Awaits <paramref name="task" />, failing with a <see cref="TimeoutException" /> if it does not
    ///     complete within <paramref name="timeout" />.
    /// </summary>
    /// <param name="task">The operation to await.</param>
    /// <param name="timeout">The maximum time to wait. Must be positive.</param>
    /// <param name="cancellationToken">A token that abandons the wait when cancelled.</param>
    /// <returns>A task that completes when <paramref name="task" /> completes.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="task" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when <paramref name="timeout" /> is not positive, or exceeds the range accepted by
    ///     <see cref="Task.WaitAsync(TimeSpan, CancellationToken)" />.
    /// </exception>
    /// <exception cref="TimeoutException">Thrown when <paramref name="task" /> does not complete in time.</exception>
    /// <exception cref="OperationCanceledException">
    ///     Thrown when <paramref name="cancellationToken" /> is cancelled while waiting.
    /// </exception>
    /// <remarks>
    ///     Expiry abandons the wait only — the underlying operation keeps running and any result it
    ///     eventually produces is discarded. Pass a cancellable operation when the work itself must stop.
    /// </remarks>
    public static Task WithTimeout(this Task task, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(task);
        ValidateTimeout(timeout);

        return task.WaitAsync(timeout, cancellationToken);
    }








    /// <summary>
    ///     Awaits <paramref name="task" />, failing with a <see cref="TimeoutException" /> if it does not
    ///     complete within <paramref name="timeout" />.
    /// </summary>
    /// <typeparam name="TResult">The result type produced by <paramref name="task" />.</typeparam>
    /// <param name="task">The operation to await.</param>
    /// <param name="timeout">The maximum time to wait. Must be positive.</param>
    /// <param name="cancellationToken">A token that abandons the wait when cancelled.</param>
    /// <returns>The result produced by <paramref name="task" />.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="task" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when <paramref name="timeout" /> is not positive, or exceeds the range accepted by
    ///     <see cref="Task.WaitAsync(TimeSpan, CancellationToken)" />.
    /// </exception>
    /// <exception cref="TimeoutException">Thrown when <paramref name="task" /> does not complete in time.</exception>
    /// <exception cref="OperationCanceledException">
    ///     Thrown when <paramref name="cancellationToken" /> is cancelled while waiting.
    /// </exception>
    /// <remarks>
    ///     Expiry abandons the wait only — the underlying operation keeps running and any result it
    ///     eventually produces is discarded. Pass a cancellable operation when the work itself must stop.
    /// </remarks>
    public static Task<TResult> WithTimeout<TResult>(this Task<TResult> task, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(task);
        ValidateTimeout(timeout);

        return task.WaitAsync(timeout, cancellationToken);
    }








    /// <summary>
    ///     Rejects timeouts that cannot represent a finite wait, so callers fail at the call site rather
    ///     than at the point where the wait is scheduled.
    /// </summary>
    /// <param name="timeout">The timeout supplied by the caller.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="timeout" /> is not positive.</exception>
    /// <remarks>
    ///     The upper bound is intentionally not duplicated here; <see cref="Task.WaitAsync(TimeSpan, CancellationToken)" />
    ///     already rejects values it cannot schedule.
    /// </remarks>
    private static void ValidateTimeout(TimeSpan timeout)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeout, TimeSpan.Zero);
    }
}
