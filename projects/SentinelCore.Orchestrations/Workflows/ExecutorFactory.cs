// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         ExecutorFactory.cs
// Author: Kyle L. Crowder
// Build Num:  092308



using Microsoft.Extensions.DependencyInjection;

using SentinelCore.Orchestrations.Workflows.Executors;




namespace SentinelCore.Orchestrations.Workflows;





/// <summary>
///     Factory for creating workflow executors with proper dependency injection support.
///     This reduces the constructor dependency count in TheCoreWorkflow and improves testability.
/// </summary>
internal sealed class ExecutorFactory
{
    private readonly IServiceProvider _serviceProvider;








    public ExecutorFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }








    /// <summary>
    ///     Creates an executor of the specified type using the service provider.
    /// </summary>
    /// <typeparam name="T">The executor type to create.</typeparam>
    /// <returns>A new instance of the executor.</returns>
    public T Create<T>() where T : class
    {
        return ActivatorUtilities.CreateInstance<T>(_serviceProvider);
    }








    /// <summary>
    ///     Creates all workflow executors used by the TheCore graph in a single call.
    /// </summary>
    /// <returns>A container holding every executor wired into the workflow graph.</returns>
    public ExecutorCollection CreateAllExecutors()
    {
        return new ExecutorCollection
        {
            SafetyExecutor = Create<SafetyExecutor>(),
            EscalatedExecutor = Create<EscalatedExecutor>(),
            PatternCheckExecutor = Create<PatternCheckExecutor>(),
            HumanOperatorExecutor = Create<HumanOperatorExecutor>(),
            PersistEvidenceExecutor = Create<PersistEvidence>(),
            NewCaseExecutor = Create<NewCaseExecutor>(),
            AggregationExecutor = Create<AggregationExecutor>(),
            MoreInformationExecutor = Create<MoreInformationExecutor>(),
            CriticalAlert = Create<CriticalAlert>(),
            TerminateWorkflow = Create<TerminateWorkflow>(),
            SafetyReviewExecutor = Create<SafetyReviewExecutor>(),

        };
    }
}





/// <summary>
///     Container for all workflow executors to simplify passing them around.
///     Holds exactly the executors wired into the TheCore workflow graph.
/// </summary>
internal sealed class ExecutorCollection
{
    public required AggregationExecutor AggregationExecutor { get; init; }
    public required CriticalAlert CriticalAlert { get; init; }
    public required EscalatedExecutor EscalatedExecutor { get; init; }
    public required HumanOperatorExecutor HumanOperatorExecutor { get; init; }
    public required MoreInformationExecutor MoreInformationExecutor { get; init; }
    public required NewCaseExecutor NewCaseExecutor { get; init; }
    public required PatternCheckExecutor PatternCheckExecutor { get; init; }
    public required PersistEvidence PersistEvidenceExecutor { get; init; }
    public required SafetyExecutor SafetyExecutor { get; init; }
    public required TerminateWorkflow TerminateWorkflow { get; init; }
    public SafetyReviewExecutor SafetyReviewExecutor { get; init; }
}