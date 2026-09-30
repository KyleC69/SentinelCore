// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         WorkflowBase.cs
// Author: Kyle L. Crowder
// Build Num:  092308



using SentinelCore.Contracts.Abstractions;
using SentinelCore.Orchestrations.Services;




namespace SentinelCore.Orchestrations.Workflows;





/// <summary>
///     Base class for all workflow orchestrations. Provides common utilities,
///     state management, and event processing capabilities.
/// </summary>
public abstract class WorkflowBase
{
    protected ISystemReporter _reporter;
    protected IWorkflowEventProcessor _eventProcessor;

    protected WorkflowBase(ISystemReporter reporter, IWorkflowEventProcessor eventProcessor)
    {
        _reporter = reporter;
        _eventProcessor = eventProcessor;
    }



}

