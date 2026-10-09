// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         SentinelAgentFactory.cs
// Author: Kyle L. Crowder
// Build Num:  100908



#pragma warning disable MAAI001

using Microsoft.Agents.AI.Compaction;
using Microsoft.Extensions.Logging;

using SentinelCore.Abstractions;
using SentinelCore.Contracts.Contracts;
using SentinelCore.Orchestrations.Abstractions;
using SentinelCore.Orchestrations.Agents.AgentPresets;
using SentinelCore.Orchestrations.Agents.Middleware;
using SentinelCore.Orchestrations.Agents.Models;
using SentinelCore.Orchestrations.Mcp;
using SentinelCore.Orchestrations.Providers;




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



    private readonly Dictionary<string, string> _builtAgents = new(StringComparer.Ordinal);
    private readonly object _builtAgentsLock = new();
    private readonly IChatClientFactory _chatClientFactory;
    private readonly ILogger<SentinelAgentFactory> _logger;
    private readonly ILoggerFactory _loggerFactory;

    // The legacy profile builder is no longer used for new code.
    // private readonly IAgentProfileBuilder _profileBuilder;
    private readonly IModelResolver _modelResolver;

    private readonly IPatternMatcher _patternMatcher;
    private static IMcpServerRegistry _registry;








    /// <summary>
    ///     Initializes a new instance of the <see cref="SentinelAgentFactory" /> class.
    /// </summary>
    /// <param name="chatClientFactory">Factory for creating chat clients from model profiles.</param>
    /// <param name="patternMatcher">The pattern matcher used by the Core agent's pattern-memory context provider.</param>
    /// <param name="loggerFactory">The logger factory used by client-level and pattern-memory logging.</param>
    /// <param name="registry">The MCP server registry for managing MCP server instances.</param>
    /// <exception cref="ArgumentNullException">Thrown when any required dependency is <c>null</c>.</exception>
    public SentinelAgentFactory(IChatClientFactory chatClientFactory, IModelResolver modelResolver, IPatternMatcher patternMatcher, ILoggerFactory loggerFactory, IMcpServerRegistry registry)
    {
        _chatClientFactory = chatClientFactory ?? throw new ArgumentNullException(nameof(chatClientFactory));
        // _profileBuilder is obsolete – not injected.
        _modelResolver = modelResolver ?? throw new ArgumentNullException(nameof(modelResolver));
        _patternMatcher = patternMatcher ?? throw new ArgumentNullException(nameof(patternMatcher));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _logger = loggerFactory.CreateLogger<SentinelAgentFactory>();
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
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

        if (preset.UsePatternMemory)
        {
            contextProviders.Add(new PatternMemoryInjector(_patternMatcher, _loggerFactory.CreateLogger<PatternMemoryInjector>()));
        }

        PipelineCompactionStrategy pipeline = new(new ToolResultCompactionStrategy(CompactionTriggers.TokensExceed(0x200)), new SlidingWindowCompactionStrategy(CompactionTriggers.TurnsExceed(25)), new TruncationCompactionStrategy(CompactionTriggers.TokensExceed(0x128000)));

        contextProviders.Add(new CompactionProvider(pipeline));

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
    private static ChatClientAgent BuildAgent(AgentPresetDefinition preset, IChatClient chatClient, ChatResponseFormatJson? chatResponseFormat = null, List<AIContextProvider?> contextProviders = null)
    {

        var aitools = _registry.GetAllTools(CancellationToken.None);

        ChatOptions chatOptions = new()
        {
            ConversationId = Guid.NewGuid().ToString("N"),
            Instructions = preset.DefaultSystemInstructions + "\n" + preset.TaskInstructions, //Setting instructions at system level adds strength to them making them less likely to be ignored by agent, user level chat messages are suggestions in comparison.
            Temperature = preset.ModelProfile?.Temperature,
            MaxOutputTokens = preset.ModelProfile?.MaxOutputTokens ?? 16000,
            TopP = preset.ModelProfile?.TopP,
            TopK = preset.ModelProfile?.TopK,
            ModelId = preset.ModelProfile?.ModelId,
            AllowMultipleToolCalls = true,
            AllowBackgroundResponses = null,
            ContinuationToken = null,
            RawRepresentationFactory = null,
            AdditionalProperties = null,
            ResponseFormat = chatResponseFormat
        };


        return new ChatClientAgent(chatClient, new ChatClientAgentOptions
        {
            Id = preset.AgentId,
            Name = preset.AgentName,
            Description = preset.Description,
            ChatOptions = chatOptions,
            AIContextProviders = (contextProviders.Count > 0 ? contextProviders : null)!,
            UseProvidedChatClientAsIs = false,
            ClearOnChatHistoryProviderConflict = false,
            WarnOnChatHistoryProviderConflict = false,
            ThrowOnChatHistoryProviderConflict = true,
            RequirePerServiceCallChatHistoryPersistence = false,
            EnableMessageInjection = false,
            DisableApprovalNotRequiredFunctionBypassing = false,
            DisableApprovalResponseBinding = false,
            ChatHistoryProvider = new AdvancedInMemoryChatHistoryProvider()
        });
    }

    /// <summary>
    /// Shared implementation for creating an <see cref="AIAgent" /> from a preset definition.
    /// Handles model resolution, chat client construction with logging, and optional context providers.
    /// </summary>
    private async Task<AIAgent> CreateAgentCoreAsync(AgentPresetDefinition preset, string? taskInstructions, ChatResponseFormatJson? forJsonSchema, CancellationToken token = default)
    {
        // Resolve the appropriate model profile based on the preset's tier
        ModelProfile? model = _modelResolver.ResolveByTier(preset.Tier);
        Throw.IfNull(model, $"Unable to resolve a model for preset '{preset.Tier}'.");
        preset.ModelProfile = model;

        // Apply task instructions if supplied (preserve existing if null)
        if (taskInstructions != null)
        {
            preset.TaskInstructions = taskInstructions;
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
        return (AIAgent)BuildAgent(preset, loggingClient, forJsonSchema, providers);
    }








    /// <summary>
    ///     Registers a worker agent with the next available positive numeric suffix.
    /// </summary>
    /// <param name="preset">The worker preset whose identity is being registered.</param>
    /// <returns>A copy of the worker preset with a unique agent identifier and name.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="preset" /> is <c>null</c>.</exception>
    private AgentPresetDefinition RegisterUniqueWorkerAgent(AgentPresetDefinition preset)
    {
        ArgumentNullException.ThrowIfNull(preset);

        lock (_builtAgentsLock)
        {
            int suffix = 1;
            string agentId;
            string agentName;

            do
            {
                agentId = $"{preset.AgentId}{suffix}";
                agentName = $"{preset.AgentName}{suffix}";
                suffix++;
            } while (_builtAgents.ContainsKey(agentName) || _builtAgents.ContainsValue(agentId));

            _builtAgents.Add(agentName, agentId);
            return preset with { AgentId = agentId, AgentName = agentName };
        }
    }
}





public sealed class SentinelCoreAgentException : Exception
{
    public SentinelCoreAgentException(string message, Exception? innerException = null) : base(ValidateMessage(message), innerException)
    {
        HResult = GenerateHResult(); // Assign a meaningful HResult for categorization
    }








    private static int GenerateHResult()
    {
        // Generate or assign a specific HResult value for this exception type
        return unchecked((int)0x80131500); // Example: COR_E_APPLICATION
    }








    private static string ValidateMessage(string message)
    {
        return string.IsNullOrWhiteSpace(message) ? "A SentinelCore agent error occurred. Please provide additional context for debugging." : message;
    }
}