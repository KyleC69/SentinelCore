// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         OrchestrationControl.cs
// Author: Kyle L. Crowder
// Build Num:  100310



using Microsoft.Extensions.Options;

using SentinelCore.Abstractions;
using SentinelCore.Contracts.Abstractions;
using SentinelCore.Contracts.Contracts;
using SentinelCore.Orchestrations.Abstractions;
using SentinelCore.Orchestrations.Services;




namespace SentinelCore.Orchestrations.Application;





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
    private readonly ISystemReporter _systemReporter;








    //TODO: Incorrect settings param needs to come from UI settings page- Orchestration Factory can read options directly for selected orchestration
    public OrchestrationControl(IOrchestrationFactory orchestrationFactory, IOptions<SentinelCoreSettings> settings, ISystemReporter systemReporter, IWorkflowEventProcessor eventProcessor)
    {
        SentinelCoreSettings settings1 = settings.Value != null ? settings.Value : Throw.IfNull(settings.Value);
        _systemReporter = systemReporter;
        Throw.IfNull(orchestrationFactory);
        _orchestration = orchestrationFactory.CreateOrchestrationInstance(settings1.OrchestrationType);
    }








    /// <summary>
    ///     Executes a streaming operation asynchronously, producing a sequence of workflow events.
    /// </summary>
    /// <param name="input">The <see cref="ChatMessage" /> containing the input data for the operation.</param>
    /// <param name="linkedCtsToken">
    ///     A <see cref="CancellationToken" /> used to propagate notifications that the operation
    ///     should be canceled.
    /// </param>
    /// <returns>
    ///     An asynchronous enumerable of <see cref="WorkflowEvent" /> representing the sequence of events produced by the
    ///     operation,
    ///     or <c>null</c> if the operation does not produce any events.
    /// </returns>
    /// <remarks>
    ///     This method delegates the execution to the underlying orchestration component.
    /// </remarks>
    public async Task<IAsyncEnumerable<WorkflowEvent>> ExecuteStreamingAsync(ChatMessage input, CancellationToken linkedCtsToken)
    {
        return await _orchestration.ExecuteStreamingAsync(input, linkedCtsToken);
    }








    /// <summary>
    ///     Called from Background Service at startup
    /// </summary>
    /// <param name="linkedCtsToken"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task InitializeOrchestrationAsync(CancellationToken linkedCtsToken)
    {
        if (_orchestration is null)
        {
            throw new InvalidOperationException("No orchestration instance is available.");
        }


        // Initialize agents once (idempotent — repeated calls are safe no-ops)
        await _orchestration.InitializeAsync(linkedCtsToken).ConfigureAwait(false);

        // Raising an event to notify that the orchestration process is starting. This can be useful for logging, monitoring, or triggering other actions in response to the start of the orchestration.
        _systemReporter.ReportInfo($"Starting orchestration {_orchestration.Name}");
    }
}