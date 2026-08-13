// Solution: SentinelCore
// Project:   SentinelCore.Contracts
// File:         SentinelCoreEvents.cs
// Author: Kyle L. Crowder
// Build Num:  081312



namespace SentinelCore.Events;





/// <summary>
///     Default implementation of <see cref="ISentinelCoreEvents" />.
///     Publishes activity through multicast events so multiple subscribers (e.g., the Host UI)
///     can observe orchestration and agent output without taking a direct dependency on the core library.
/// </summary>
public sealed class SentinelCoreEvents : ISentinelCoreEvents
{
    public event Action<string, Exception>? ErrorOccurred;

    /// <summary>
    ///     Raised when an orchestration lifecycle event occurs.
    /// </summary>
    public event Action<OrchestrationActivityArgs>? OrchestrationEvent;








    public void RaiseError(string message, Exception exception)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ArgumentNullException.ThrowIfNull(exception);
        ErrorOccurred?.Invoke(message, exception);
    }








    public void RaiseOrchestrationEvent(OrchestrationActivityArgs payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        OrchestrationEvent?.Invoke(payload);
    }








    /// <summary>
    ///     Raises a unified Sentinel output event for all normal agent/tool/workflow activity.
    ///     This is the single event channel for UI consumption of agent output.
    /// </summary>
    /// <param name="payload">The output event payload.</param>
    public void RaiseSentinelOutputEvent(SentinelOutputEventArgs payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        SentinelOutputEvent?.Invoke(payload);
    }








    /// <summary>
    ///     Can be raised at any time to relay information from the core library to the host UI or other subscribers.
    ///     This event is not tied to any specific orchestration or agent, and is intended for general output messages.
    /// </summary>
    public event Action<SentinelOutputEventArgs>? SentinelOutputEvent;

    public event Action<SentinelErrorEventArgs>? SentinelErrorEvent;
}





public record SentinelErrorEventArgs(string Message, Exception? Exception = null)
{
    public Exception? Exception { get; } = Exception;
    public string Message { get; } = Message;
}