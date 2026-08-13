// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         IOrchestrationControl.cs
// Author: Kyle L. Crowder
// Build Num:  081312



namespace SentinelCore.Abstractions;





/// <summary>
///     Controls initialization of an orchestration from an incoming prompt signal.
/// </summary>
public interface IOrchestrationControl
{
    /// <summary>
    ///     Initializes the orchestration from the provided prompt signal.
    /// </summary>
    /// <param name="promptSignal">The prompt signal that starts the orchestration.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task InitializeOrchestrationAsync(ChatMessage promptSignal, CancellationToken token);
}