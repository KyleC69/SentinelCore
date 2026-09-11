// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         SentinelCoreServiceExtensions.cs
// Author: Kyle L. Crowder
// Build Num:  082808



using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

using ModelContextProtocol.Authentication;

using SentinelCore.Abstractions;
using SentinelCore.Agents;
using SentinelCore.Application;
using SentinelCore.Cfe;
using SentinelCore.Events;
using SentinelCore.Infrastructure.Persistence;
using SentinelCore.Mcp;
using SentinelCore.Orchestrations.Mcp;
using SentinelCore.Workflows;
using SentinelCore.Workflows.Executors;

using System.Text.Json;




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
                    opt.ManagerModel = options.ManagerModel;
                    opt.DefaultUtilityModel = options.DefaultUtilityModel;
                    opt.OrchestrationType = options.OrchestrationType;
                    opt.SqlConnectionString = options.SqlConnectionString;

                    foreach (KeyValuePair<string, ModelProfile> entry in options.AgentModels)
                    {
                        opt.AgentModels[entry.Key] = entry.Value;
                    }
                });

        JsonConfiguredLogging(services);


        // -- Always-on core services --
        // Safety middleware defaults to pass-through; host can override with real rules

        // Case Flow Engine — owns the entire case lifecycle; registers its own internal repository.
        // Transient so it does not capture scoped/transient persistence services (DbContext, IEvidenceStore)
        // and can be resolved safely from any scope.
        services.AddTransient<ICaseFlowEngine, CaseFlowEngine>();
        services.AddTransient<IEvidenceStore, EvidenceStore>();
        services.AddTransient<IPatternMemoryStore, PatternMemoryStore>();
        services.AddSingleton<IOrchestrationControl, OrchestrationControl>();
        services.AddTransient<IOrchestration, CustomGroupWorkflow>();
        services.AddTransient<IOrchestration, TheCoreWorkflow>();
        //services.AddTransient<IClipboardService>();
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

        // -- MCP server registry --
        // Stores server definitions in %APPDATA%\SentinelCore\mcp-servers.json unless
        // the SENTINEL_MCP_REGISTRY_PATH environment variable overrides it.
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string defaultRegistryPath = Path.Combine(appDataPath, "SentinelCore", "mcp-servers.json");
        string registryPath = Environment.GetEnvironmentVariable("SENTINEL_MCP_REGISTRY_PATH") ?? defaultRegistryPath;
        string tokenCachePath = Path.Combine(Path.GetDirectoryName(registryPath)!, "mcp-tokens.bin");

        services.AddSingleton<IMcpServerRegistryStore>(sp => new JsonFileMcpServerRegistryStore(
            registryPath,
            sp.GetRequiredService<ILogger<JsonFileMcpServerRegistryStore>>()));
        services.AddSingleton<ITokenCache>(_ => new DpapiTokenCache(tokenCachePath));
        services.AddSingleton<IMcpConnectionFactory, McpConnectionFactory>();
        services.AddSingleton<IMcpServerRegistry, McpServerRegistry>();
        services.AddSingleton<ISentinelAgentCatalog, SentinelAgentCatalog>();
        services.AddHostedService<McpServerRegistryInitializer>();


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
            MinimumLevel = LogLevel.Trace,
            Indented = true,
            Output = JsonLoggerOutput.File,
            FilePath = "SentinelCore.log"

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
