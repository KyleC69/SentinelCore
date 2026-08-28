// Solution: SentinelCore
// Project:   SentinelCore.Contracts
// File:         ISentinelCoreEvents.cs
// Author: Kyle L. Crowder
// Build Num:  082808



namespace SentinelCore.Events;





/// <summary>
///     Central event hub for publishing SentinelCore activity to host UI and subscribers.
/// </summary>
public interface ISentinelCoreEvents
{
    /// <summary>
    ///     Raised when an error occurs that should be surfaced to the host UI.
    /// </summary>
    event Action<string, Exception>? ErrorOccurred;

    /// <summary>
    ///     Raised when an orchestration lifecycle event occurs.
    /// </summary>
    event Action<OrchestrationActivityArgs>? OrchestrationEvent;








    /// <summary>
    ///     Raises an error event.
    /// </summary>
    /// <param name="message">A descriptive error message.</param>
    /// <param name="exception">The exception that occurred.</param>
    void RaiseError(string message, Exception exception);








    /// <summary>
    ///     Raises an orchestration event that can be segregated by agent name and activity type.
    /// </summary>
    /// <param name="payload">The orchestration event payload.</param>
    void RaiseOrchestrationEvent(OrchestrationActivityArgs payload);








    /// <summary>
    ///     Raises a unified Sentinel output event for non-agent/orchestration activity.
    /// </summary>
    /// <param name="payload">The output event payload.</param>
    void RaiseSentinelOutputEvent(SentinelOutputEventArgs payload);








    /// <summary>
    ///     Raised during any normal non-agent/tool operation.
    ///     This is the single event channel for UI consumption of agent output.
    /// </summary>
    event Action<SentinelOutputEventArgs>? SentinelOutputEvent;
}