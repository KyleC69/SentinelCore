// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         SentinelCoreServiceExtensions.cs
// Author: Kyle L. Crowder
// Build Num:  112518



using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using ModelContextProtocol.Authentication;

using SentinelCore.CaseFlowEngine.CaseFlow;
using SentinelCore.CaseFlowEngine.Persistence;
using SentinelCore.Contracts.Abstractions;
using SentinelCore.Contracts.Contracts;
using SentinelCore.Contracts.Events;
using SentinelCore.Contracts.Mcp;
using SentinelCore.Orchestrations.Abstractions;
using SentinelCore.Orchestrations.Agents;
using SentinelCore.Orchestrations.Agents.Core.Tools;
using SentinelCore.Orchestrations.Agents.Middleware;
using SentinelCore.Orchestrations.Application;
using SentinelCore.Orchestrations.Mcp;
using SentinelCore.Orchestrations.Rag;
using SentinelCore.Orchestrations.Workflows;
using SentinelCore.Security.Infrastructure.DependencyInjection;




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

                    opt.ChatClientLoggingEnabled = options.ChatClientLoggingEnabled;
                    opt.OpenTelemetryTracingEnabled = options.OpenTelemetryTracingEnabled;
                    opt.OpenTelemetryLoggingEnabled = options.OpenTelemetryLoggingEnabled;
                    opt.TraceEnabled = options.TraceEnabled;
                    opt.TraceLogLevel = options.TraceLogLevel;

                    opt.OrchestrationType = options.OrchestrationType;
                    opt.SqlConnectionString = options.SqlConnectionString;

                    foreach (KeyValuePair<ModelTier, ModelProfile> entry in options.TierModels)
                    {
                        opt.TierModels[entry.Key] = entry.Value;
                    }
                });

        // -- Always-on core services --

        services.AddSingleton<ICaseFlowEngine, CaseFlowEngine.CaseFlow.CaseFlowEngine>();
        services.AddSingleton<IPatternMemoryStore, PatternMemoryStore>();
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
        services.AddSingleton<ISentinelCoreEvents, SentinelCoreEvents>();
        // Register the model resolver used by AgentProfileBuilder
        services.AddSingleton<IModelResolver, ModelResolver>();
        // AgentProfileBuilder is obsolete – no longer registered.
        services.AddSingleton<ISystemReporter, SystemReporter>();
        services.AddSingleton<TheCoreWorkflow>();
        services.AddSingleton<CaseEngineTools>();
        RegisterLocalAgentTools(services);
        // Register factory with model resolver dependency
        services.AddSingleton<ISentinelAgentFactory>(sp =>
        {
            IChatClientFactory chatFactory = sp.GetRequiredService<IChatClientFactory>();
            IModelResolver modelResolver = sp.GetRequiredService<IModelResolver>();
            IPatternMatcher patternMatcher = sp.GetRequiredService<IPatternMatcher>();
            ILoggerFactory loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            IMcpServerRegistry registry = sp.GetRequiredService<IMcpServerRegistry>();
            IEnumerable<AITool> localTools = sp.GetServices<AITool>();
            SentinelCoreSettings runtimeSettings = sp.GetRequiredService<IOptions<SentinelCoreSettings>>().Value;
            return new SentinelAgentFactory(chatFactory, modelResolver, patternMatcher, loggerFactory, registry, localTools, runtimeSettings);
        });

        services.AddSingleton<IOrchestrationFactory, OrchestrationFactory>();
        services.RegisterExecutors();

        // -- Message provenance security --
        // Signs messages stamped by approved entry points and quarantines anything that
        // fails validation. Registered for every host that uses AddSentinelCore.
        services.AddSentinelSecurity();

        // -- MCP server registry --
        // Stores server definitions in %APPDATA%\SentinelCore\mcp-servers.json unless
        // the SENTINEL_MCP_REGISTRY_PATH environment variable overrides it.
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string defaultRegistryPath = Path.Combine(appDataPath, "SentinelCore", "mcp-servers.json");
        string registryPath = Environment.GetEnvironmentVariable("SENTINEL_MCP_REGISTRY_PATH") ?? defaultRegistryPath;
        string tokenCachePath = Path.Combine(Path.GetDirectoryName(registryPath)!, "mcp-tokens.bin");

        services.AddSingleton<IMcpServerRegistry, McpServerRegistry>();
        services.AddSingleton<IMcpServerRegistryStore>(sp => new JsonFileMcpServerRegistryStore(registryPath, sp.GetRequiredService<ILogger<JsonFileMcpServerRegistryStore>>()));
        services.AddSingleton<ITokenCache>(_ => new DpapiTokenCache(tokenCachePath));
        services.AddSingleton<IMcpConnectionFactory, McpConnectionFactory>();
        // McpServerRegistryInitializer moved to App.xaml.cs OnStartup for explicit control


        return services;
    }








    /// <summary>
    ///     Registers the in-process agent tools that <see cref="AgentToolSelection" /> can bind to an agent
    ///     alongside MCP-server tools.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to which the tools will be added.</param>
    /// <returns>The updated <see cref="IServiceCollection" /> with the local agent tools registered.</returns>
    /// <remarks>
    ///     Only invocable tools may be registered here, which means every registration must derive from
    ///     <see cref="AIFunction" />. Plain <see cref="AITool" /> metadata stubs cannot be invoked by an agent
    ///     and must never be exposed through this catalog.
    /// </remarks>
    private static IServiceCollection RegisterLocalAgentTools(IServiceCollection services)
    {
        services.AddSingleton<MsLearnMcpTools>();
        services.AddSingleton<AITool>(sp =>
        {
            MsLearnMcpTools learnTools = sp.GetRequiredService<MsLearnMcpTools>();
            return AIFunctionFactory.Create(learnTools.SearchDocs, name: "sc_knowledge_search_docs", description: "Performs semantic search against Microsoft official technical documentation.");
        });

        services.AddSingleton<AITool>(sp =>
        {
            MsLearnMcpTools learnTools = sp.GetRequiredService<MsLearnMcpTools>();
            return AIFunctionFactory.Create(learnTools.FetchDocs, name: "sc_knowledge_fetch_docs", description: "Fetches a Microsoft documentation page and converts it into markdown format.");
        });

        services.AddSingleton<AITool>(sp =>
        {
            MsLearnMcpTools learnTools = sp.GetRequiredService<MsLearnMcpTools>();
            return AIFunctionFactory.Create(learnTools.SearchCodeSamples, name: "sc_knowledge_search_code_samples", description: "Searches for official Microsoft and Azure code snippets and examples.");
        });

        services.AddSingleton<AgentSpawnTools>();
        services.AddSingleton<AITool>(sp =>
        {
            AgentSpawnTools spawnTools = sp.GetRequiredService<AgentSpawnTools>();
            return AIFunctionFactory.Create(spawnTools.SpawnAgent, name: "sc_internal_spawn_agent", description: "Spawns a one-shot utility agent with the requested tool groups, runs the supplied task and returns the agent's answer.");
        });

        services.AddSingleton<AITool>(sp =>
        {
            CaseEngineTools caseEngineTools = sp.GetRequiredService<CaseEngineTools>();
            return AIFunctionFactory.Create(caseEngineTools.CreateCase, name: "sc_internal_case_create", description: "Creates a new investigative case for the Sentinel Core platform from a natural-language signal description.");
        });

        services.AddSingleton<AITool>(sp =>
        {
            CaseEngineTools caseEngineTools = sp.GetRequiredService<CaseEngineTools>();
            return AIFunctionFactory.Create(caseEngineTools.InsertReport, name: "sc_internal_case_insert_report", description: "Inserts the final investigation report for a case.");
        });

        services.AddSingleton<AITool>(sp =>
        {
            CaseEngineTools caseEngineTools = sp.GetRequiredService<CaseEngineTools>();
            return AIFunctionFactory.Create(caseEngineTools.InsertPatternSummaries, name: "sc_internal_case_insert_pattern_summaries", description: "Inserts the summaries extracted from the final investigation report.");
        });

        return services;
    }
}
