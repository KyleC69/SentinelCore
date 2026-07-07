// Solution: SentinelCoreLib
// Project:   SentinelCoreLib
// File:         SentinelCoreHostExtensions.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.Configuration;

using SentinelCore.Contracts;

using SentinelCoreLib.Agents.Core;
using SentinelCoreLib.Agents.Domain;
using SentinelCoreLib.Agents.Dynamic;
using SentinelCoreLib.Agents.Manager;
using SentinelCoreLib.Application;
using SentinelCoreLib.Application.Abstractions.Persistence;
using SentinelCoreLib.CaseFlow;
using SentinelCoreLib.Domain.Contracts;
using SentinelCoreLib.Infrastructure.Persistence;
using SentinelCoreLib.Orchestration;




namespace SentinelCoreLib.Hosting;





/// <summary>
///     Provides extension methods for integrating SentinelCore into a host application.
///     This is the single public entry point for all SentinelCore library registration.
/// </summary>
public static class SentinelCoreHostExtensions
{
    /// <summary>
    ///     Adds SentinelCore services and optional persistence to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">The runtime options.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddSentinelCore(this IServiceCollection services, SentinelCoreSettings options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);
        services.AddSingleton<CoreAgentFactory>();
        services.AddSingleton<TheCoreOrchestration>();
        services.AddSingleton(options);
        services.AddSingleton<IOptions<SentinelCoreSettings>>(new OptionsWrapper<SentinelCoreSettings>(options));
        services.AddSingleton<MagneticOrchestration>();

        services.AddLogging(o =>
        {
            o.AddConsole();
            o.AddDebug();
            if (options.AgentTraceEnabled)
            {
                o.SetMinimumLevel(options.AgentTraceLogLevel);
            }
        });

        services.AddSingleton<ICaseFlowEngine, CaseFlowEngine>();
        services.AddSingleton<ManagerAgentFactory>();
        services.AddSingleton<MagneticOrchestration>();
        services.AddSingleton<DomainAgentFactory>();
        services.AddSingleton<DynamicAgentFactory>();
        services.AddSingleton<InvestigationControl>();

        services.AddSentinelCorePersistence(Environment.GetEnvironmentVariable("SENTINEL_CORE"));

        return services;
    }








    private static IServiceCollection AddSentinelCorePersistence(this IServiceCollection services, string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidConfigurationException("The connection string for SentinelCore persistence is not configured.");
        }

        services.AddDbContext<SentinelCoreDbContext>(options => options.UseSqlServer(connectionString, sql => sql.MigrationsAssembly(typeof(SentinelCoreDbContext).Assembly.GetName().Name)));

        services.AddSingleton<ICaseRepository, CaseRepository>();
        services.AddSingleton<IEvidenceStore, EvidenceStore>();
        services.AddSingleton<IPatternMemoryStore, PatternMemoryStore>();

        return services;
    }
}