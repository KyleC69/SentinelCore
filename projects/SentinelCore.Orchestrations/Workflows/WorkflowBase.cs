// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         WorkflowBase.cs
// Author: Kyle L. Crowder
// Build Num:  100310



using SentinelCore.Contracts.Abstractions;
using SentinelCore.Orchestrations.Services;




namespace SentinelCore.Orchestrations.Workflows;





/// <summary>
///     Base class for all workflow orchestrations. Provides common utilities,
///     state management, and event processing capabilities.
/// </summary>
public abstract class WorkflowBase
{
    protected IWorkflowEventProcessor _eventProcessor;
    protected ISystemReporter _reporter;








    protected WorkflowBase(ISystemReporter reporter, IWorkflowEventProcessor eventProcessor)
    {
        _reporter = reporter;
        _eventProcessor = eventProcessor;
    }
}