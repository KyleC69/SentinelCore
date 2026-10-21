// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         SentinelAgentFactory.cs
// Author: Kyle L. Crowder
// Build Num:  100920



#pragma warning disable MAAI001

using Microsoft.Extensions.Logging;

using SentinelCore.CaseFlowEngine.Infrastructure;
using SentinelCore.Contracts.Contracts;
using SentinelCore.Contracts.Mcp;
using SentinelCore.Orchestrations.Abstractions;
using SentinelCore.Orchestrations.Agents.AgentPresets;
using SentinelCore.Orchestrations.Agents.Middleware;
using SentinelCore.Orchestrations.Agents.Models;




// experimental API: suppressed with MAAI001




namespace SentinelCore.Orchestrations.Agents;





/// <summary>
///     Provides functionality to create and configure instances of <see cref="AIAgent" />
///     based on the provided <see cref="AgentProfile" /> or preset name.
///     <para>
///         This factory is the canonical composition boundary for the chat client,
///         context providers, tools, and agent options. It applies a mandatory
///         <see cref="LoggingChatClient" /> wrapper to every chat client and configures
///         pattern memory and MAF context compaction from the agent preset.
///     </para>
///     <para>
///         Instructions and response format are per-call concerns handled by executors
///         at invocation time via <see cref="ChatMessages" /> and <see cref="AgentRunOptions" />.
///         The factory does NOT set <see cref="ChatOptions.Instructions" /> or
///         <see cref="ChatOptions.ResponseFormat" />.
///     </para>
/// </summary>
public sealed class SentinelAgentFactory : ISentinelAgentFactory
{


    private IReadOnlyList<AITool> _builtToolCache;
    private readonly Dictionary<string, string> _builtAgents = new(StringComparer.Ordinal);
    private readonly Lock _builtAgentsLock = new();
    private readonly IChatClientFactory _chatClientFactory;
    private readonly ILogger<SentinelAgentFactory> _logger;
    private readonly ILoggerFactory _loggerFactory;

    // The legacy profile builder is no longer used for new code.
    // private readonly IAgentProfileBuilder _profileBuilder;
    private readonly IModelResolver _modelResolver;

    private readonly IPatternMatcher _patternMatcher;
    private IMcpServerRegistry _registry;








    /// <summary>
    ///     Initializes a new instance of the <see cref="SentinelAgentFactory" /> class.
    /// </summary>
    /// <param name="chatClientFactory">Factory for creating chat clients from model profiles.</param>
    /// <param name="modelResolver"></param>
    /// <param name="patternMatcher">The pattern matcher used by the Core agent's pattern-memory context provider.</param>
    /// <param name="loggerFactory">The logger factory used by client-level and pattern-memory logging.</param>
    /// <param name="registry">The MCP server registry for managing MCP server instances.</param>
    /// <exception cref="ArgumentNullException">Thrown when any required dependency is <c>null</c>.</exception>
    public SentinelAgentFactory(IChatClientFactory chatClientFactory, IModelResolver modelResolver, IPatternMatcher patternMatcher, ILoggerFactory loggerFactory, IMcpServerRegistry registry)
    {
        _chatClientFactory = chatClientFactory ?? throw new ArgumentNullException(nameof(chatClientFactory));
        _modelResolver = modelResolver ?? throw new ArgumentNullException(nameof(modelResolver));
        _patternMatcher = patternMatcher ?? throw new ArgumentNullException(nameof(patternMatcher));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _logger = loggerFactory.CreateLogger<SentinelAgentFactory>();
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _builtToolCache = _registry.GetToolsFromCache();
        _registry.ToolCacheChanged += OnToolCacheChanged;

    }








    private void OnToolCacheChanged()
    {
        RefreshToolCache();
    }








    public async Task<AIAgent> CreateAgentAsync(AgentPreset presetName, CancellationToken cancellationToken = default)
    {
        // 2‑parameter overload forwards to the 3‑parameter overload with no task instructions.
        return await CreateAgentAsync(presetName, null, cancellationToken);
    }








    public Task<AIAgent> CreateAgentAsync(AgentPreset presetName, string? taskInstructions, CancellationToken cancellationToken = default)
    {
        // 3‑parameter overload forwards to the 4‑parameter overload with no JSON schema.
        return CreateAgentAsync(presetName, taskInstructions, null, cancellationToken);
    }








    public async Task<AIAgent> CreateAgentAsync(AgentPreset presetName, string? taskInstructions, ChatResponseFormatJson? forJsonSchema, CancellationToken token)
    {
        // Resolve the preset definition from the enum
        AgentPresetDefinition preset = AgentPresetDefinition.FromName(presetName);
        // Forward to core implementation that performs all setup.
        return await CreateAgentCoreAsync(preset, taskInstructions, forJsonSchema, token);
    }








    public async Task<AIAgent> CreateAgentAsync(AgentPreset presetName, string agentName, string agentDescription, string taskInstructions)
    {
        // Resolve the preset definition from the enum
        AgentPresetDefinition preset = AgentPresetDefinition.FromName(presetName);
        //edit the preset with the provided name and description
        preset.AgentName = agentName;
        preset.Description = agentDescription;


        return await CreateAgentCoreAsync(preset, taskInstructions, null, CancellationToken.None);
    }








    public async Task<AIAgent> CreateAgentAsync(AgentPreset presetName, string agentName, string agentDescription)
    {
        _logger.LogInformation("Creating agent with preset: {PresetName}, name: {AgentName}, description: {AgentDescription}", presetName, agentName, agentDescription);

        var presetDefinition = AgentPresetDefinition.FromName(presetName);
        if (presetDefinition == null)
        {
            throw new InvalidOperationException($"Preset definition for {presetName} could not be resolved.");
        }

        var agent = await CreateAgentCoreAsync(presetDefinition, null, null, CancellationToken.None);
        _logger.LogInformation("Agent created successfully: {AgentName}", agentName);

        return agent;
    }








    /// <summary>
    ///     Adds context providers specified by the agent preset.
    /// </summary>
    /// <param name="preset">The preset that determines which optional context providers apply.</param>
    /// <param name="contextProviders">The context-provider collection for the agent being constructed.</param>
    /// <exception cref="ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    private void AddPresetContextProviders(AgentPresetDefinition preset, List<AIContextProvider> contextProviders)
    {
        ArgumentNullException.ThrowIfNull(preset);
        if (contextProviders is not List<AIContextProvider>)
        {
            contextProviders = new List<AIContextProvider>();
        }

        /*
         TEMPORARILY DISABLED UNTIL PATTERN MEMORY IS STABLE

                if (preset.UsePatternMemory)
                {
                    contextProviders.Add(new PatternMemoryInjector(_patternMatcher, _loggerFactory.CreateLogger<PatternMemoryInjector>()));
                }

             //   PipelineCompactionStrategy pipeline = new(new ToolResultCompactionStrategy(CompactionTriggers.TokensExceed(0x200)), new SlidingWindowCompactionStrategy(CompactionTriggers.TurnsExceed(25)), new TruncationCompactionStrategy(CompactionTriggers.TokensExceed(0x128000)));

             //   contextProviders.Add(new CompactionProvider(pipeline));
        */
        if (preset.UseContextEnrichers)
        {
            // Add context enrichers here
        }
    }








    /// <summary>
    ///     Constructs the final <see cref="AIAgent" /> from the configured chat client and context providers.
    /// </summary>
    /// <param name="preset">The preset that supplies the agent identity.</param>
    /// <param name="profile">The model settings that supply chat options.</param>
    /// <param name="chatClient">The configured chat client that serves model requests.</param>
    /// <param name="chatResponseFormat"></param>
    /// <param name="contextProviders">The context providers that enrich agent invocations.</param>
    /// <returns>A fully configured <see cref="ChatClientAgent" />.</returns>
    private ChatClientAgent BuildAgent(AgentPresetDefinition preset, IChatClient chatClient, ChatResponseFormat? chatResponseFormat = null, List<AIContextProvider?>? contextProviders = null)
    {



        ChatOptions chatOptions = new()
        {
            ConversationId = Guid.NewGuid().ToString("N"),
            Instructions = string.Concat(preset.DefaultSystemInstructions, preset.RoleInstructions), //Setting instructions at system level adds strength to them making them less likely to be ignored by agent, user level chat messages are suggestions in comparison.
            Temperature = preset.ModelProfile?.Temperature,
            MaxOutputTokens = preset.ModelProfile?.MaxOutputTokens ?? 25000,

            TopP = preset.ModelProfile?.TopP ?? .10f,
            TopK = preset.ModelProfile?.TopK ?? 30,
            Seed = 69,
            ModelId = preset.ModelProfile?.ModelId,
            AllowMultipleToolCalls = true,
            ToolMode = ChatToolMode.Auto,
            Tools = preset.UseTools ? _builtToolCache.ToList() : new List<AITool>(),
            ResponseFormat = chatResponseFormat ?? ChatResponseFormat.Json
        };


        return new ChatClientAgent(chatClient, new ChatClientAgentOptions
        {
            Id = preset.AgentId,
            Name = preset.AgentName,
            Description = preset.Description,
            ChatOptions = chatOptions,
            AIContextProviders = preset.UseContextEnrichers ? contextProviders : null,
            UseProvidedChatClientAsIs = false,
            AllowConcurrentInvocation = false,
            ClearOnChatHistoryProviderConflict = true,
            WarnOnChatHistoryProviderConflict = false,
            ThrowOnChatHistoryProviderConflict = true,
            RequirePerServiceCallChatHistoryPersistence = false,
            EnableMessageInjection = false,
            DisableApprovalNotRequiredFunctionBypassing = false,
            DisableApprovalResponseBinding = false,
            ChatHistoryProvider = null
        });
    }








    /// <summary>
    ///     Shared implementation for creating an <see cref="AIAgent" /> from a preset definition.
    ///     Handles model resolution, chat client construction with logging, and optional context providers.
    /// </summary>
    private async Task<AIAgent> CreateAgentCoreAsync(AgentPresetDefinition preset, string? taskInstructions, ChatResponseFormatJson? forJsonSchema, CancellationToken token = default)
    {
        // Resolve the appropriate model profile based on the preset's tier
        ModelProfile? model = _modelResolver.ResolveByTier(preset.Tier);
        preset.ModelProfile = model;
        Throw.IfNull(model, $"Unable to resolve a model for preset '{preset.Tier}'.");

        // Apply task instructions if supplied (preserve existing if null)
        if (taskInstructions != null)
        {
            preset.RoleInstructions += taskInstructions;
        }

        // Build the base chat client and wrap with logging middleware
        IChatClient chatclient = _chatClientFactory.CreateChatClient(model);
        IChatClient loggingClient = chatclient.AsBuilder().UseLogging(_loggerFactory).Build();

        // Prepare context providers when the preset requests any enrichers.
        List<AIContextProvider?> providers = null!;
        if (preset.UseContextEnrichers || preset.UsePatternMemory || preset.UseRagSearch)
        {
            providers = new();
            AddPresetContextProviders(preset, providers);
        }

        // Build and cast to the public interface type.
        return BuildAgent(preset, loggingClient, forJsonSchema, providers);
    }








    /// <summary>
    ///     Registers a worker agent with the next available positive numeric suffix.
    /// </summary>
    /// <param name="preset">The worker preset whose identity is being registered.</param>
    /// <returns>A copy of the worker preset with a unique agent identifier and name.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="preset" /> is <c>null</c>.</exception>
    /// <summary>
    ///     Refreshes the tool cache by querying the MCP server registry for the latest tools.
    ///     This should be called when the MCP server registry changes (e.g., after servers are started).
    /// </summary>
    public void RefreshToolCache()
    {
        _builtToolCache = _registry.GetToolsFromCache();
    }








    private (string, string) RegisterUniqueWorkerAgent(string agentId, string agentName)
    {
        ArgumentNullException.ThrowIfNull(agentId);
        ArgumentNullException.ThrowIfNull(agentName);

        lock (_builtAgentsLock)
        {
            int suffix = 1;


            do
            {
                agentId = $"{agentId}{suffix}";
                agentName = $"{agentName}{suffix}";
                suffix++;
            } while (_builtAgents.ContainsKey(agentId) || _builtAgents.ContainsValue(agentName));

            _builtAgents.Add(agentId, agentName);
            return (agentId, agentName);
        }
    }
}
