// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         SentinelAgentFactory.cs
// Author: Kyle L. Crowder
// Build Num:  100409



#pragma warning disable MAAI001

using System.Diagnostics.CodeAnalysis;

using Microsoft.Agents.AI.Compaction;
using Microsoft.Extensions.Logging;

using SentinelCore.Abstractions;
using SentinelCore.Contracts.Contracts;
using SentinelCore.Orchestrations.Agents.AgentPresets;
using SentinelCore.Orchestrations.Agents.Middleware;
using SentinelCore.Orchestrations.Agents.Models;
using SentinelCore.Orchestrations.Mcp;
using SentinelCore.Orchestrations.Providers;
// experimental API: suppressed with MAAI001




namespace SentinelCore.Orchestrations.Agents;





/// <summary>
///     Defines the contract for building <see cref="AIAgent" /> instances from agent presets.
/// </summary>
public interface ISentinelAgentFactory
{

    /// <summary>
    ///     Builds an <see cref="AIAgent" /> instance from the specified profile.
    ///     <para>
    ///         This method is for callers that need to construct an agent from a pre-built
    ///         <see cref="AgentProfile" /> (e.g., <see cref="CaseGenerator" />).
    ///         Prefer <see cref="CreateAgentAsync" /> for preset-based construction.
    ///     </para>
    /// </summary>
    /// <param name="profile">The agent profile containing configuration details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A fully configured <see cref="AIAgent" /> instance.</returns>
    Task<AIAgent> BuildFromProfileAsync(AgentPresetDefinition preset, AgentProfile profile, CancellationToken cancellationToken = default);








    /// <summary>
    ///     Asynchronously creates an instance of <see cref="AIAgent" /> based on the specified preset name.
    ///     The agent is constructed with persistent infrastructure (chat client, wrappers, context providers, tools).
    ///     Instructions and response format are per-call concerns handled by the executor at invocation time,
    ///     not baked into the agent at construction time.
    /// </summary>
    /// <param name="presetName">
    ///     The name of the preset to use for creating the agent (e.g., "Classifier", "TheCore").
    /// </param>
    /// <param name="cancellationToken">
    ///     A token to observe while waiting for the task to complete, enabling cancellation of the operation.
    /// </param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result is a fully configured
    ///     <see cref="AIAgent" /> instance.
    /// </returns>
    /// <exception cref="ArgumentException">
    ///     Thrown if the specified <paramref name="presetName" /> does not correspond to any registered preset.
    /// </exception>
    Task<AIAgent> CreateAgentAsync(string presetName, CancellationToken cancellationToken = default);
}





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
    private readonly IChatClientFactory _chatClientFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IPatternMatcher _patternMatcher;
    private readonly IAgentProfileBuilder _profileBuilder;
    private readonly IMcpServerRegistry _registry;








    /// <summary>
    ///     Initializes a new instance of the <see cref="SentinelAgentFactory" /> class.
    /// </summary>
    /// <param name="chatClientFactory">Factory for creating chat clients from model profiles.</param>
    /// <param name="profileBuilder">The profile builder for constructing agent profiles from presets.</param>
    /// <param name="patternMatcher">The pattern matcher used by the Core agent's pattern-memory context provider.</param>
    /// <param name="loggerFactory">The logger factory used by client-level and pattern-memory logging.</param>
    /// <param name="registry">The MCP server registry for managing MCP server instances.</param>
    /// <exception cref="ArgumentNullException">Thrown when any required dependency is <c>null</c>.</exception>
    public SentinelAgentFactory(IChatClientFactory chatClientFactory, IAgentProfileBuilder profileBuilder, IPatternMatcher patternMatcher, ILoggerFactory loggerFactory, IMcpServerRegistry registry)
    {
        _chatClientFactory = chatClientFactory ?? throw new ArgumentNullException(nameof(chatClientFactory));
        _profileBuilder = profileBuilder ?? throw new ArgumentNullException(nameof(profileBuilder));
        _patternMatcher = patternMatcher ?? throw new ArgumentNullException(nameof(patternMatcher));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _registry = registry ?? Throw.IfNull(registry);
    }








    /// <summary>
    ///     Asynchronously builds an <see cref="AIAgent" /> instance based on the provided <see cref="AgentProfile" />.
    ///     <para>
    ///         This method is for callers that need to construct an agent from a pre-built
    ///         <see cref="AgentProfile" /> (e.g., <see cref="CaseGenerator" />).
    ///         Prefer <see cref="CreateAgentAsync" /> for preset-based construction.
    ///     </para>
    /// </summary>
    /// <param name="profile">
    ///     The <see cref="AgentProfile" /> containing the configuration details required to build the agent.
    /// </param>
    /// <param name="cancellationToken">
    ///     A <see cref="CancellationToken" /> that can be used to cancel the asynchronous operation.
    /// </param>
    /// <returns>
    ///     A task representing the asynchronous operation. The task result contains the constructed <see cref="AIAgent" />.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the provided <see cref="AgentProfile" /> does not contain a valid model configuration.
    /// </exception>
    [Experimental("MAAI001")]
    public Task<AIAgent> BuildFromProfileAsync(AgentPresetDefinition preset, AgentProfile profile, CancellationToken cancellationToken = default)
    {
        Throw.IfNull(profile);
        cancellationToken.ThrowIfCancellationRequested();

        // Configuration gate — an agent without a model profile cannot run.
        // There is deliberately no fallback: the user must configure the agent
        // on the Model Configuration page.
        if (profile.Model is null)
        {
            throw new InvalidOperationException($"Agent '{profile.AgentName}' has no model configuration. " + "Open the Model Configuration page and set a provider, model, and endpoint for this agent.");
        }

        // Resolve the preset to get construction-time flags (logging, pattern memory, etc.)
        //  AgentPresetDefinition? presetDef = _presetProvider.GetPreset(profile.AgentName);
        //  if (presetDef is null)
        //   {
        //     presetDef = AgentPresetDefinition.UtilityRole(profile.AgentName, profile.AgentId);
        // }

        // Create the chat client from the profile's model configuration.
        IChatClient chatClient = _chatClientFactory.CreateChatClient(profile.Model);

        IChatClient loggedChatClient = new LoggingChatClient(chatClient, _loggerFactory.CreateLogger<LoggingChatClient>());
        List<AIContextProvider> contextProviders = [];

        // Add profile-level context providers (set by callers like CaseGenerator).
        if (profile.AIContextProviders is { Count: > 0 })
        {
            contextProviders.AddRange(profile.AIContextProviders);
        }

        AddPresetContextProviders(preset, contextProviders);
        return Task.FromResult<AIAgent>(BuildAgent(preset, profile.Model, loggedChatClient, contextProviders));
    }




    private readonly Dictionary<string, string> BuiltAgents = new(StringComparer.Ordinal);
    private readonly object _builtAgentsLock = new();



    /// <summary>
    ///     This is the single source of agent creation. It takes a preset name,
    ///     resolves the preset, builds the agent profile, and constructs the agent.
    ///     Instructions and response format are per-call concerns — they are NOT set here.
    ///     Executors set instructions via <see cref="ChatMessages" /> and response format
    ///     via <see cref="AgentRunOptions" /> or typed <c>RunAsync&lt;T&gt;</c> at invocation time.
    ///     NOTE: This method is the only entry point for creating agents from presets.
    ///     All other agent creation methods should funnel through this one.
    ///     Do not create other forms of creation unless justified with ADR.
    /// </summary>
    /// <param name="presetName">The preset name identifying the agent role.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A fully configured <see cref="AIAgent" /> instance.</returns>
    /// <exception cref="ArgumentException">Thrown if the preset name is not found.</exception>
    [Experimental("MAAI001")]
    public async Task<AIAgent> CreateAgentAsync(string presetName, CancellationToken cancellationToken = default)
    {

        // Validate input
        Throw.IfNullOrWhitespace(presetName);
        AgentPresetDefinition preset = null;
        AgentProfile profile = null!;
        string workerBaseName = presetName;
        bool isWorker = presetName.Contains("worker", StringComparison.OrdinalIgnoreCase);

        if (isWorker)
        {
            presetName = "worker";


        }

        switch (presetName.ToLower())
        {
            case "thecore":

                // Build the agent profile
                preset = AgentPresetDefinition.CoreRole();
                profile = BuildAgentProfile(preset, cancellationToken);
                profile.Tools = _registry.GetAllTools(CancellationToken.None);
                break;

            case "worker": // To be used for the WindowsOSInvestigator agent role


                preset = RegisterUniqueWorkerAgent(AgentPresetDefinition.UtilityRole(workerBaseName, workerBaseName));
                profile = BuildAgentProfile(preset, cancellationToken);
                profile.Tools = _registry.GetAllTools(CancellationToken.None);
                break;


            case "classifier":
                // Build the agent profile
                preset = AgentPresetDefinition.ClassifierRole();
                profile = BuildAgentProfile(preset, cancellationToken);
                break;

            case "manager":
                // Build the agent profile
                preset = AgentPresetDefinition.ManagerRole();

                profile = BuildAgentProfile(preset, cancellationToken);
                break;

            case "safetyagent":
                // Build the agent profile
                preset = AgentPresetDefinition.SafetyAgent();
                profile = BuildAgentProfile(preset, cancellationToken);
                break;
            default:
                throw new ArgumentException($"Missing switch case named {presetName}");
                break;
        }
        // Track existing Id/Name pairs for non-worker agents.
        // Worker identities are registered inside RegisterUniqueWorkerAgent.
        if (!isWorker)
        {
            BuiltAgents.Add(profile.AgentName, profile.AgentId);
        }
        // Build and return the agent via the contributor pipeline
        return await BuildFromProfileAsync(preset, profile, cancellationToken).ConfigureAwait(false);
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
            }
            while (BuiltAgents.ContainsKey(agentName) || BuiltAgents.ContainsValue(agentId));

            BuiltAgents.Add(agentName, agentId);
            return preset with { AgentId = agentId, AgentName = agentName };
        }
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
        ArgumentNullException.ThrowIfNull(contextProviders);

        if (preset.UsePatternMemory)
        {
            contextProviders.Add(new PatternMemoryInjector(_patternMatcher, _loggerFactory.CreateLogger<PatternMemoryInjector>()));
        }

        PipelineCompactionStrategy pipeline = new(new ToolResultCompactionStrategy(CompactionTriggers.TokensExceed(0x200)), new SlidingWindowCompactionStrategy(CompactionTriggers.TurnsExceed(25)), new TruncationCompactionStrategy(CompactionTriggers.TokensExceed(0x128000)));

        contextProviders.Add(new CompactionProvider(pipeline));
    }








    /// <summary>
    ///     Constructs the final <see cref="AIAgent" /> from the configured chat client and context providers.
    /// </summary>
    /// <param name="preset">The preset that supplies the agent identity.</param>
    /// <param name="model">The model settings that supply chat options.</param>
    /// <param name="chatClient">The configured chat client that serves model requests.</param>
    /// <param name="contextProviders">The context providers that enrich agent invocations.</param>
    /// <returns>A fully configured <see cref="ChatClientAgent" />.</returns>
    private static ChatClientAgent BuildAgent(AgentPresetDefinition preset, ModelProfile model, IChatClient chatClient, List<AIContextProvider> contextProviders)
    {
        ChatOptions chatOptions = new()
        {
            ConversationId = Guid.NewGuid().ToString("N"),
            Instructions = AgentInstructionConstants.CURRENT_PLATFORM_DOMAIN_S,
            Temperature = model.Temperature,
            MaxOutputTokens = model.MaxOutputTokens ?? 16000,
            TopP = model.TopP,
            TopK = model.TopK,
            ModelId = model.ModelId,
            AllowMultipleToolCalls = true
            // ResponseFormat is NOT set here — it is a per-call concern.
            // Executors pass it via AgentRunOptions or typed RunAsync<T> at invocation time.
        };

        return new ChatClientAgent(chatClient, new ChatClientAgentOptions
        {
            Id = preset.AgentId,
            Name = preset.AgentName,
            Description = "An AI Agent",
            ChatOptions = chatOptions,
            AIContextProviders = contextProviders.Count > 0 ? contextProviders : null,
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








    private AgentProfile BuildAgentProfile(AgentPresetDefinition preset, CancellationToken token)
    {
        // Build the agent profile from the preset. Both AgentId and AgentName are required.
        // Some internals use name and others use ID, ensure both are set for consistency.
        AgentProfile profile = _profileBuilder.BuildFromPreset(preset);


        if (string.IsNullOrEmpty(profile.AgentName) || string.IsNullOrWhiteSpace(profile.AgentId))
        {
            throw new InvalidOperationException($"Preset '{preset.GetType().Name}' did not provide a valid AgentName.");
        }

        if (string.IsNullOrWhiteSpace(profile.AgentId))
        {
            throw new InvalidOperationException($"Preset '{preset.GetType().Name}' did not provide a valid AgentId.");
        }

        return profile;
    }
}