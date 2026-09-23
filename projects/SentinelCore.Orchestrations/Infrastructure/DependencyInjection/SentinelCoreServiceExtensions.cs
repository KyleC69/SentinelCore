// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         SentinelCoreServiceExtensions.cs
// Author: Kyle L. Crowder
// Build Num:  092308



using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using ModelContextProtocol.Authentication;

using SentinelCore.CaseFlowEngine.Cfe;
using SentinelCore.CaseFlowEngine.Infrastructure.Persistence;
using SentinelCore.Contracts.Abstractions;
using SentinelCore.Contracts.Contracts;
using SentinelCore.Contracts.Events;
using SentinelCore.Contracts.Mcp;
using SentinelCore.Orchestrations.Abstractions;
using SentinelCore.Orchestrations.Agents;
using SentinelCore.Orchestrations.Agents.Middleware;
using SentinelCore.Orchestrations.Application;
using SentinelCore.Orchestrations.Mcp;
using SentinelCore.Orchestrations.Rag;
using SentinelCore.Orchestrations.Workflows;




namespace SentinelCore.Orchestrations.Infrastructure.DependencyInjection;





/// <summary>
///     Provides extension methods for integrating SentinelCore into a host application.
///     This is the single public entry point for all SentinelCore library registration.
/// </summary>
public static class SentinelCoreServiceExtensions
{



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

        // -- Always-on core services --
        // Safety middleware defaults to pass-through; host can override with real rules

        // Case Flow Engine is an OPTIONAL module (PL-4/PL-5): register null-object
        // defaults so the system runs without persistence. The host opts in to the
        // real EF Core-backed engine by calling AddCaseFlowEngine() (CaseFlowEngine
        // project), which overrides these defaults via RemoveAll<T>() + Add<T>().
        services.AddSingleton<ICaseFlowEngine, NullCaseFlowEngine>();
        services.AddSingleton<IEvidenceStore, NullEvidenceStore>();
        services.AddSingleton<IPatternMemoryStore, NullPatternMemoryStore>();
        services.AddTransient<IPatternMatcher, SemanticPatternMatcher>();

        // -- RAG Search Services --
        // Register RAG search options with defaults from settings
        services.Configure<RagSearchOptions>(opt =>
        {
            if (options.RagSearch != null)
            {
                opt.Enabled = options.RagSearch.Enabled;
                opt.AutoInjectEnabled = options.RagSearch.AutoInjectEnabled;
                opt.MaxResults = options.RagSearch.MaxResults;
                opt.RelevanceThreshold = options.RagSearch.RelevanceThreshold;
                opt.MaxContextSize = options.RagSearch.MaxContextSize;
                opt.RelevanceKeywords = options.RagSearch.RelevanceKeywords;
                opt.VectorSearchEnabled = options.RagSearch.VectorSearchEnabled;
                opt.EnabledForAgentRoles = options.RagSearch.EnabledForAgentRoles;
                opt.ToolEnabled = options.RagSearch.ToolEnabled;
                opt.ContextInjectorEnabled = options.RagSearch.ContextInjectorEnabled;
            }
        });
        services.AddSingleton<IRagSearchService, RagSearchService>();

        services.AddSingleton<IOrchestrationControl, OrchestrationControl>();
        services.AddTransient<IChatClientFactory, SentinelChatClientFactory>();
        services.AddTransient<CustomGroupWorkflow>();
        services.AddTransient<ICaseGenerator, CaseGenerator>();
        services.AddSingleton<ISentinelCoreEvents, SentinelCoreEvents>();
        services.AddSingleton<IAgentPresetProvider, AgentPresetProvider>();
        services.AddSingleton<IAgentProfileBuilder, AgentProfileBuilder>();
        services.AddSingleton<ISystemReporter, SystemReporter>();
        services.AddSingleton<ISentinelWorkflowExecution, SentinelWorkflowExecution>();
        services.AddSingleton<TheCoreWorkflow>();
        services.AddSingleton<ISentinelAgentFactory, SentinelAgentFactory>();

        // -- Agent construction contributors (pipeline) --
        // Each contributor owns exactly one concern. Adding a new middleware type
        // requires only a new contributor class and DI registration — no factory changes.
        services.AddSingleton<IAgentConstructionContributor, LoggingClientContributor>();
        services.AddSingleton<IAgentConstructionContributor, PatternMemoryContributor>();
        services.AddSingleton<IAgentConstructionContributor, McpToolContributor>();
        services.AddSingleton<IAgentConstructionContributor, CompactionContributor>();

        services.AddSingleton<IOrchestrationFactory, OrchestrationFactory>();
        services.RegisterExecutors();

        // -- MCP server registry --
        // Stores server definitions in %APPDATA%\SentinelCore\mcp-servers.json unless
        // the SENTINEL_MCP_REGISTRY_PATH environment variable overrides it.
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string defaultRegistryPath = Path.Combine(appDataPath, "SentinelCore", "mcp-servers.json");
        string registryPath = Environment.GetEnvironmentVariable("SENTINEL_MCP_REGISTRY_PATH") ?? defaultRegistryPath;
        string tokenCachePath = Path.Combine(Path.GetDirectoryName(registryPath)!, "mcp-tokens.bin");

        services.AddSingleton<IMcpServerRegistryStore>(sp => new JsonFileMcpServerRegistryStore(registryPath, sp.GetRequiredService<ILogger<JsonFileMcpServerRegistryStore>>()));
        services.AddSingleton<ITokenCache>(_ => new DpapiTokenCache(tokenCachePath));
        services.AddSingleton<IMcpConnectionFactory, McpConnectionFactory>();
        services.AddSingleton<IMcpServerRegistry, McpServerRegistry>();
        services.AddSingleton<ISentinelAgentCatalog, SentinelAgentCatalog>();
        services.AddHostedService<McpServerRegistryInitializer>();


        return services;
    }
}