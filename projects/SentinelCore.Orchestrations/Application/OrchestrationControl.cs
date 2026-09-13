// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         OrchestrationControl.cs
// Author: Kyle L. Crowder
// Build Num:  091300



using Microsoft.Extensions.Options;

using SentinelCore.Abstractions;
using SentinelCore.Contracts.Abstractions;
using SentinelCore.Contracts.Contracts;
using SentinelCore.Contracts.Events;
using SentinelCore.Contracts.Mcp;
using SentinelCore.Orchestrations.Abstractions;




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
    private readonly IMcpServerRegistry _mcpServerRegistry;
    private readonly IOrchestration? _orchestration;
    private readonly ISentinelCoreEvents _sentinelCoreEvents;
    private readonly ISystemReporter _systemReporter;
    private readonly ISentinelWorkflowExecution _workflowExecution;








    public OrchestrationControl(IOrchestrationFactory orchestrationFactory, IOptions<SentinelCoreSettings> settings, ISentinelCoreEvents events, ISystemReporter systemReporter, ISentinelWorkflowExecution workflowExecution, IMcpServerRegistry mcpServerRegistry)
    {
        SentinelCoreSettings settings1 = settings.Value != null ? settings.Value : Throw.IfNull(settings.Value);
        _sentinelCoreEvents = events;
        _systemReporter = systemReporter;
        _workflowExecution = workflowExecution;
        _mcpServerRegistry = mcpServerRegistry;
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

        await EnsureMcpServersStartedAsync(token).ConfigureAwait(false);

        // Initialize agents once (idempotent - will throw if called twice)
        await _orchestration.InitializeAsync(token).ConfigureAwait(false);

        // Raising an event to notify that the orchestration process is starting. This can be useful for logging, monitoring, or triggering other actions in response to the start of the orchestration.
        _sentinelCoreEvents.RaiseSentinelOutputEvent(new SentinelOutputEventArgs(_orchestration.Name, "Starting orchestration", ActivityType.Orchestration));

        return await _orchestration.ExecuteAsync(promptSignal, token).ConfigureAwait(false);
    }








    /// <summary>
    ///     Ensures all registered MCP servers are started before the orchestration executes.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task EnsureMcpServersStartedAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<McpServerInfo> servers = await _mcpServerRegistry.ListAsync(cancellationToken).ConfigureAwait(false);

        foreach (McpServerInfo server in servers)
        {
            try
            {
                await _mcpServerRegistry.StartAsync(server.Definition.Id, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _systemReporter.ReportWarning($"Failed to start MCP server '{server.Definition.Id}': {ex.Message}");
            }
        }
    }
}