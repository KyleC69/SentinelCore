// Solution: SentinelCore
// Project:   SentinelCore.Tests
// File:         SentinelCoreServiceExtensions.cs
// Author: Kyle L. Crowder
// Build Num:  080801



using SentinelCore.Abstractions;
using SentinelCore.Agents;
using SentinelCore.Application;
using SentinelCore.Contracts;
using SentinelCore.DependencyInjection;
using SentinelCore.Events;
using SentinelCore.Infrastructure.DependencyInjection;
using SentinelCore.Workflows;




namespace SentinelCore.Orchestrations.Infrastructure.DependencyInjection;





/// <summary>
///     Provides extension methods for integrating SentinelCore into a host application.
///     This is the single public entry point for all SentinelCore library registration.
/// </summary>
public static class SentinelCoreServiceExtensions
{
    /// <summary>
    ///     Adds SentinelCore services to the service collection and invokes the
    ///     configuration callback so the host can opt into optional modules.
    ///     <para>
    ///         Always-on registrations include the event hub, agent builder, agent
    ///         factories, and The Core orchestration.
    ///     </para>
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">The runtime settings.</param>
    /// <param name="configure">A callback that configures optional SentinelCore modules.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddSentinelCore(this IServiceCollection services, SentinelCoreSettings options, Action<ISentinelCoreBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(configure);

        // -- Bind settings into the options pipeline --
        services.AddOptions<SentinelCoreSettings>()
                .Configure(opt =>
                {
                    opt.TraceEnabled = options.TraceEnabled;
                    opt.TraceLogLevel = options.TraceLogLevel;
                    opt.CoreModel = options.CoreModel;
                    opt.DomainModel = options.DomainModel;
                    opt.ManagerModel = options.ManagerModel;
                    opt.OrchestrationType = options.OrchestrationType;
                    opt.SqlConnectionString = options.SqlConnectionString;
                    opt.DefaultModel = options.DefaultModel;
                });

        // -- Always-on core services --

        services.AddSingleton<ISentinelCoreEvents, SentinelCoreEvents>();
        services.AddSingleton<IAgentSpecBuilder, AgentProfileBuilder>();
        services.AddSingleton<ICoreAgentFactory, CoreAgentFactory>();
        services.AddSingleton<ISystemReporter, SystemReporter>();
        services.AddSingleton<SentinelWorkflowExecution>();
        services.AddSingleton<TheCoreWorkflow>();
        services.AddSingleton<TheCoreOrchestration>();
        services.AddSingleton<IManagerAgentFactory, ManagerAgentFactory>();
        services.AddSingleton<IDomainAgentFactory, DomainAgentFactory>();
        services.AddSingleton<IOrchestrationFactory, OrchestrationFactory>();
        services.AddSingleton<IOrchestrationControl, OrchestrationControl>();
        services.AddSingleton<GroupConcurrentOrchestration>();
        services.AddSingleton<GroupTurnBasedOrchestration>();
        services.AddSingleton<SequentialOrchestration>();
        services.AddSingleton<MagneticCoopOrchestration>();
        services.AddSingleton<SingleAgent>();
        services.AddSingleton<MagneticOrchestration>();

        // -- Invoke host configuration --
        SentinelCoreBuilder builder = new(services);
        configure(builder);

        // -- Validate module dependency chains --
        builder.ValidateDependencies();

        return services;
    }
}