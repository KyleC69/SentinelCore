// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         MagneticOrchestration.cs
// Author: Kyle L. Crowder
// Build Num:  081312



using Microsoft.Extensions.Logging;

using SentinelCore.Agents;
using SentinelCore.Application;
using SentinelCore.Cfe;
using SentinelCore.Events;




namespace SentinelCore.Orchestrations;





public interface IMagneticOrchestration
{
    Task<InvestigationPlan> ExecuteTasksAsync(List<InvestigationPlanStep> tasks, string caseId, CancellationToken cancellationToken = default);
}





/// <summary>
///     Represents a magnetic orchestrated workflow that is handed a list of investigation tasks.
///     The workflow manager monitors streaming execution and logs communication between the
///     manager and the domain agents created for each task.
/// </summary>
public sealed class MagneticOrchestration
{
    private readonly IAgentProfileBuilder _agentProfileBuilder;
    private readonly ISentinelCoreEvents _events;
    private readonly ILogger<MagneticOrchestration> _logger;
    private readonly ILoggerFactory _loggerFactory;








    /// <summary>
    ///     Initializes a new instance of the <see cref="MagneticOrchestration" /> class.
    /// </summary>
    public MagneticOrchestration(IAgentProfileBuilder agentProfileBuilder, ISentinelCoreEvents events, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(agentProfileBuilder);
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(loggerFactory);
        _agentProfileBuilder = agentProfileBuilder;
        _events = events;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<MagneticOrchestration>();
    }








    public string Description { get; } = "Represents a magnetic orchestrated workflow that is handed a list of investigation tasks.";

    public string Name { get; } = "MagneticOrchestration";








    public Workflow BuildWorkflow()
    {
        throw new NotImplementedException();
    }








    public Task<WorkflowExecutionResult> ExecuteAsync(ISentinelWorkflowExecution executor, ChatMessage prompt, CancellationToken token)
    {
        throw new NotImplementedException();
    }
}