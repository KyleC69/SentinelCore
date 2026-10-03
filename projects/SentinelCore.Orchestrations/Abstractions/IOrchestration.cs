// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         IOrchestration.cs
// Author: Kyle L. Crowder
// Build Num:  100310



using SentinelCore.Orchestrations.Workflows;




namespace SentinelCore.Orchestrations.Abstractions;





/// <summary>
///     Main interface for any workflow in application. Encapsulates execution and event handling for workflows within its
///     owning class.
///     allows for flexible execution and event processing.
///     See <see cref="WorkflowBase" /> for helpers and handlers
/// </summary>
public interface IOrchestration
{
    string Description { get; }
    string Name { get; }








    /// <summary>
    ///     Executes the workflow asynchronously based on the provided input message.
    /// </summary>
    /// <param name="inputMessage">
    ///     The input message that serves as the context or payload for the workflow execution.
    /// </param>
    /// <param name="token">
    ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
    /// </param>
    /// <returns>
    ///     An asynchronous stream of <see cref="WorkflowEvent" /> instances representing the events
    ///     generated during the workflow execution, or <c>null</c> if no events are produced.
    /// </returns>
    Task<IAsyncEnumerable<WorkflowEvent>?> ExecuteStreamingAsync(ChatMessage inputMessage, CancellationToken token);








    /// <summary>
    ///     Initializes the necessary agents and resources required for the execution of the workflow.
    ///     This method should be invoked once before any calls to <see cref="ExecuteAsync" /> to ensure
    ///     the workflow is properly prepared for execution.
    /// </summary>
    /// <param name="cancellationToken">
    ///     A token to monitor for cancellation requests. This allows the initialization process
    ///     to be gracefully terminated if required.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    Task InitializeAsync(CancellationToken cancellationToken = default);
}