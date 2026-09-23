// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         ExecutorRegistration.cs
// Author: Kyle L. Crowder
// Build Num:  092308



using Microsoft.Extensions.DependencyInjection;

using SentinelCore.Orchestrations.Workflows.Executors;




namespace SentinelCore.Orchestrations.Infrastructure.DependencyInjection;





/// <summary>
///     Extension methods for registering all workflow executors in the DI container.
///     Executors are resolved by <see cref="ExecutorFactory" /> via ActivatorUtilities;
///     these registrations exist so executors can also be resolved directly from DI when needed.
/// </summary>
public static class ExecutorRegistrations
{

    /// <summary>
    ///     Registers all workflow executors as transient services. Each executor is
    ///     registered exactly once — duplicate registrations create competing instances
    ///     and violate the one-canonical-location rule.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection RegisterExecutors(this IServiceCollection services)
    {
        services.AddTransient<NewCaseExecutor>();
        services.AddTransient<MoreInformationExecutor>();
        services.AddTransient<EscalatedExecutor>();
        services.AddTransient<PatternCheckExecutor>();
        services.AddTransient<SafetyExecutor>();
        services.AddTransient<HumanOperatorExecutor>();
        services.AddTransient<DirectAnswerExecutor>();
        services.AddTransient<AggregationExecutor>();
        services.AddTransient<CriticalAlert>();

        return services;
    }
}