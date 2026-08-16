// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         IOrchestration.cs
// Author: Kyle L. Crowder
// Build Num:  081602



using SentinelCore.Application;




namespace SentinelCore.Abstractions;





public interface IOrchestration
{
    string Description { get; }
    string Name { get; }

    // The underlying workflow (Magentic, group, single agent, etc.)


    Task<Workflow> BuildWorkflow();


    /*
        Task<WorkflowExecutionResult> ExecuteAsync(
                ISentinelWorkflowExecution workflowExecution,
                ChatMessage promptSignal,
                CancellationToken token);
        */


    Task<WorkflowExecutionResult?> ExecuteAsync(ChatMessage promptSignal, CancellationToken token);
}