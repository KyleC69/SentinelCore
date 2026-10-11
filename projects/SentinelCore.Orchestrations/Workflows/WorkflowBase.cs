// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         WorkflowBase.cs
// Author: Kyle L. Crowder
// Build Num:  100908



using Microsoft.Extensions.Logging;

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
    protected ILogger<WorkflowBase> _logger;
    protected ILoggerFactory _loggerFactory;
    protected ISystemReporter _reporter;








    protected WorkflowBase(ISystemReporter reporter, ILoggerFactory logger)
    {
        _reporter = reporter;
        _loggerFactory = logger;
        _logger = logger.CreateLogger<WorkflowBase>();
    }
}