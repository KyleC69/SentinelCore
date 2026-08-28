// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         OrchestrationControl.cs
// Author: Kyle L. Crowder
// Build Num:  082808



using Microsoft.Extensions.Options;

using SentinelCore.Abstractions;
using SentinelCore.Events;




namespace SentinelCore.Application;





/// <summary>
///     Represents the control mechanism for managing investigations within the SentinelCore system.
///     Will be primary entry point for initiating an investigation and control optional components such as the Case Flow
///     Engine (CFE) and other orchestration processes.
///     TODO: Implement gating for components used in builder pattern for optional components such as CFE and other
///     orchestration processes.
/// </summary>
public sealed class OrchestrationControl : IOrchestrationControl
{
    private readonly IOrchestration? _orchestration;

    private readonly ISentinelCoreEvents _sentinelCoreEvents;
    private readonly ISystemReporter _systemReporter;
    private readonly ISentinelWorkflowExecution _workflowExecution;








    public OrchestrationControl(IOrchestrationFactory orchestrationFactory, IOptions<SentinelCoreSettings> settings, ISentinelCoreEvents events, ISystemReporter systemReporter, ISentinelWorkflowExecution workflowExecution)
    {
        SentinelCoreSettings settings1 = settings.Value != null ? settings.Value : Throw.IfNull(settings.Value);
        _sentinelCoreEvents = events != null ? events : Throw.IfNull(events);
        _systemReporter = systemReporter != null ? systemReporter : Throw.IfNull(systemReporter);
        _workflowExecution = workflowExecution != null ? workflowExecution : Throw.IfNull(workflowExecution);
        Throw.IfNull(orchestrationFactory);
        _orchestration = orchestrationFactory.CreateOrchestrationInstance(settings1.OrchestrationType);
    }








    /// <summary>
    ///     Initializes the orchestration process asynchronously with the provided signal and cancellation token.
    /// </summary>
    /// <param name="promptSignal">
    ///     The <see cref="ChatMessage" /> that serves as the initial signal for the orchestration process.
    /// </param>
    /// <param name="token">
    ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when no orchestration instance is available.
    /// </exception>
    public async Task<WorkflowExecutionResult?> InitializeOrchestrationAsync(ChatMessage promptSignal, CancellationToken token)
    {
        if (_orchestration is null)
        {
            throw new InvalidOperationException("No orchestration instance is available.");
        }

        // Raising an event to notify that the orchestration process is starting. This can be useful for logging, monitoring, or triggering other actions in response to the start of the orchestration.
        _sentinelCoreEvents.RaiseSentinelOutputEvent(new SentinelOutputEventArgs(_orchestration.Name, "Starting orchestration", ActivityType.Orchestration));

        return await _orchestration.ExecuteAsync(promptSignal, token).ConfigureAwait(false);
    }
}