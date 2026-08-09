// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         ExecutorRegistration.cs
// Author: Kyle L. Crowder
// Build Num:  080801



using Microsoft.Extensions.DependencyInjection;

using SentinelCore.Workflows.Executors;
using SentinelCore.Workflows.Executors.SentinelCore.Workflows.Executors;




namespace SentinelCore.Infrastructure.DependencyInjection;





public static class ExecutorRegistrations
{

    /// <summary>
    ///     Registers all executors in DI
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection RegisterExecutors(this IServiceCollection services)
    {
        services.AddTransient<NewCaseExecutor>();
        services.AddTransient<AnalysisExecutor>();
        services.AddTransient<ClarificationExecutor>();
        services.AddTransient<MoreInformationExecutor>();
        services.AddTransient<EscalatedExecutor>();
        services.AddTransient<InvestigationExecutor>();
        services.AddTransient<PatternCheckExecutor>();
        services.AddTransient<SafetyExecutor>();
        services.AddTransient<VerifyEvidenceExecutor>();
        services.AddTransient<WhiteListExecutor>();
        services.AddTransient<HumanOperatorExecutor>();
        services.AddTransient<DirectAnswerExecutor>();
        services.AddTransient<AggregationExecutor>();
        services.AddTransient<CriticalAlert>();
        services.AddTransient<PersistTask>();
        services.AddTransient<PersistEvidence>();
        services.AddTransient<LoggingExecutor>();

        return services;
    }
}