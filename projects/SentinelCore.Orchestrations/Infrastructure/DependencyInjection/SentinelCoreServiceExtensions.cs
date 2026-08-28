// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         SentinelCoreServiceExtensions.cs
// Author: Kyle L. Crowder
// Build Num:  082808



using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

using SentinelCore.Abstractions;
using SentinelCore.Agents;
using SentinelCore.Application;
using SentinelCore.Cfe;
using SentinelCore.Cfe.Persistence;
using SentinelCore.Events;
using SentinelCore.Infrastructure.Persistence;
using SentinelCore.Workflows;
using SentinelCore.Workflows.Executors;




namespace SentinelCore.Infrastructure.DependencyInjection;





/// <summary>
///     Provides extension methods for integrating SentinelCore into a host application.
///     This is the single public entry point for all SentinelCore library registration.
/// </summary>
public static class SentinelCoreServiceExtensions
{

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true, PropertyNameCaseInsensitive = true };








    /// <summary>
    ///     Adds the core SentinelCore services to the specified <see cref="IServiceCollection" />.
    ///     This method configures essential services and dependencies required for the SentinelCore framework.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to which the services will be added.</param>
    /// <param name="options">
    ///     The <see cref="SentinelCoreSettings" /> containing runtime configuration settings,
    ///     such as database connection strings and orchestration options.
    /// </param>
    /// <returns>The updated <see cref="IServiceCollection" /> with the SentinelCore services registered.</returns>
    /// <remarks>
    ///     This method performs the following actions:
    ///     <list type="bullet">
    ///         <item>Registers the <see cref="IOrchestrationControl" /> implementation.</item>
    ///         <item>Binds the provided <see cref="SentinelCoreSettings" /> to the options pipeline.</item>
    ///         <item>Configures logging services.</item>
    ///         <item>Registers persistence services, including the database context.</item>
    ///         <item>Registers core services such as safety middleware, agent factories, and orchestration components.</item>
    ///     </list>
    /// </remarks>
    public static IServiceCollection AddSentinelCore(this IServiceCollection services, SentinelCoreSettings options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        // -- Bind settings into the options pipeline --
        services.AddSingleton<IOrchestrationControl, OrchestrationControl>();
        services.AddOptions<SentinelCoreSettings>()
                .Configure(opt =>
                {

                    opt.TraceEnabled = options.TraceEnabled;
                    opt.TraceLogLevel = options.TraceLogLevel;
                    opt.DefaultModel = options.DefaultModel;
                    opt.DefaultUtilityModel = options.DefaultUtilityModel;
                    opt.OrchestrationType = options.OrchestrationType;
                    opt.SqlConnectionString = options.SqlConnectionString;
                });

        JsonConfiguredLogging(services);

        // -- Persistence (first-class, always-on) --
        // EF Core DbContext configured from SentinelCoreSettings.SqlConnectionString.
        // The DbContext is registered as Transient to keep scoping flexible and to
        // avoid captive-dependency issues when consumed from singleton orchestrations
        // or hosted services. The store and engine registrations below follow suit.
        // The factory overload is also transient so any design-time/internal factory
        // resolution matches the context lifetime.
        // Retrieve the connection string from the supplied settings. If it is missing
        // we skip DbContext registration so the host still starts — persistence-dependent
        // features will fail at runtime with a clear error, rather than preventing the
        // entire application from launching.
        string? connectionString = options.SqlConnectionString;

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddDbContext<SentinelCoreDBContext>(dbOptions => dbOptions.UseSqlServer(connectionString), ServiceLifetime.Transient, ServiceLifetime.Transient);
            services.AddTransient<IDbContextFactory<SentinelCoreDBContext>, PooledDbContextFactory<SentinelCoreDBContext>>();

            // DatabaseInitializer is [Obsolete] (persistence is migrating to *.sqlproj), but it
            // remains the only Database.MigrateAsync path in the solution — schema would never be
            // applied without it. Keep registering until the sqlproj migration lands.
#pragma warning disable CS0618 // Type or member is obsolete
            services.AddHostedService<DatabaseInitializer>();
#pragma warning restore CS0618 // Type or member is obsolete
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("[WARN] SqlConnectionString is not configured — database persistence will be unavailable. Provide a valid connection string in SentinelCoreSettings.");
        }

        // -- Always-on core services --
        // Safety middleware defaults to pass-through; host can override with real rules

        // Case Flow Engine — owns the entire case lifecycle; registers its own internal repository.
        // Transient so it does not capture scoped/transient persistence services (DbContext, IEvidenceStore)
        // and can be resolved safely from any scope.
        services.AddTransient<ICaseFlowEngine, CaseFlowEngine>();
        services.AddTransient<IEvidenceStore, EvidenceStore>();
        services.AddTransient<IPatternMemoryStore, PatternMemoryStore>();
        services.AddTransient<CaseGenExec>();
        services.AddTransient<CustomGroupWorkflow>();
        services.AddTransient<ICaseGenerator, CaseGenerator>();
        services.AddSingleton<ISentinelCoreEvents, SentinelCoreEvents>();
        services.AddSingleton<IAgentProfileBuilder, AgentProfileBuilder>();
        services.AddSingleton<ISystemReporter, SystemReporter>();
        services.AddSingleton<ISentinelWorkflowExecution, SentinelWorkflowExecution>();
        services.AddSingleton<TheCoreWorkflow>();
        services.AddSingleton<ISentinelAgentFactory, SentinelAgentFactory>();
        services.AddSingleton<IOrchestrationFactory, OrchestrationFactory>();
        services.AddSingleton<MagneticOrchestration>();
        services.AddTransient<NewCaseExecutor>();
        services.RegisterExecutors();


        return services;
    }








    private static IServiceCollection JsonConfiguredLogging(IServiceCollection services)
    {




        Action<JsonConsoleFormatterOptions> jops = options =>
        {
            options.IncludeScopes = true;
            options.UseUtcTimestamp = false;
            options.JsonWriterOptions = new JsonWriterOptions { Indented = true, SkipValidation = false, IndentSize = 4 };
        };



        JsonLoggerOptions jsonOptions = new()
        {
                MinimumLevel = LogLevel.Trace, Indented = true, Output = JsonLoggerOutput.File, FilePath = "SentinelCore.log"

                // Or:
                // Output = JsonLoggerOutput.File,
                // FilePath = "logs/sentinelcore.json"
        };



        services.AddLogging(op =>
        {
            //   op.AddJsonConsole(jops);
            op.AddConsole();
            op.AddProvider(new JsonLoggerProvider(jsonOptions));
            op.SetMinimumLevel(LogLevel.Trace);
            op.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);


        });
        return services;
    }
}