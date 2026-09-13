// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         IOrchestration.cs
// Author: Kyle L. Crowder
// Build Num:  091300



using SentinelCore.Orchestrations.Application;




namespace SentinelCore.Orchestrations.Abstractions;





public interface IOrchestration
{
    string Description { get; }
    string Name { get; }


    Task<Workflow> BuildWorkflow();


    /*
        Task<WorkflowExecutionResult> ExecuteAsync(
                ISentinelWorkflowExecution workflowExecution,
                ChatMessage promptSignal,
                CancellationToken token);
        */


    Task<WorkflowExecutionResult?> ExecuteAsync(ChatMessage promptSignal, CancellationToken token);


    // The underlying workflow (Magentic, group, single agent, etc.)








    /// <summary>
    ///     Initializes agents and other resources required for workflow execution.
    ///     Should be called once before any calls to <see cref="ExecuteAsync" />.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task InitializeAsync(CancellationToken cancellationToken = default);
}