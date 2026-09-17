// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         ExecutorFactory.cs
// Author: Kyle L. Crowder
// Build Num:  091418



using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using SentinelCore.Orchestrations.Workflows.Executors;




namespace SentinelCore.Orchestrations.Workflows;





/// <summary>
///     Factory for creating workflow executors with proper dependency injection support.
///     This reduces the constructor dependency count in TheCoreWorkflow and improves testability.
/// </summary>
internal sealed class ExecutorFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly IServiceProvider _serviceProvider;








    public ExecutorFactory(IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
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
    ///     Creates all workflow executors in a single call to reduce ceremony.
    /// </summary>
    /// <returns>A tuple containing all workflow executors.</returns>
    public ExecutorCollection CreateAllExecutors()
    {
        return new ExecutorCollection
        {
            SafetyExecutor = Create<SafetyExecutor>(),
            EscalatedExecutor = Create<EscalatedExecutor>(),
            WhiteListExecutor = Create<WhiteListExecutor>(),
            PatternCheckExecutor = Create<PatternCheckExecutor>(),
            HumanOperatorExecutor = Create<HumanOperatorExecutor>(),
            VerifyEvidenceExecutor = Create<VerifyEvidenceExecutor>(),

            NewCaseExecutor = Create<NewCaseExecutor>(),
            AggregationExecutor = Create<AggregationExecutor>(),
            MoreInformationExecutor = Create<MoreInformationExecutor>(),
            CriticalAlert = Create<CriticalAlert>(),
            LoggingExecutor = Create<LoggingExecutor>(),
            CaseGenExecutor = Create<CaseGenExec>(),

        };
    }
}





/// <summary>
///     Container for all workflow executors to simplify passing them around.
/// </summary>
internal sealed class ExecutorCollection
{
    public required AggregationExecutor AggregationExecutor { get; init; }
    public required CaseGenExec CaseGenExecutor { get; init; }
    public required CriticalAlert CriticalAlert { get; init; }
    public required EscalatedExecutor EscalatedExecutor { get; init; }
    public required HumanOperatorExecutor HumanOperatorExecutor { get; init; }
    public required LoggingExecutor LoggingExecutor { get; init; }
    public required MoreInformationExecutor MoreInformationExecutor { get; init; }
    public required NewCaseExecutor NewCaseExecutor { get; init; }
    public required PatternCheckExecutor PatternCheckExecutor { get; init; }
    public required SafetyExecutor SafetyExecutor { get; init; }
    public required VerifyEvidenceExecutor VerifyEvidenceExecutor { get; init; }
    public required WhiteListExecutor WhiteListExecutor { get; init; }
}