// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         SentinelAgentFactory.cs
// Author: Kyle L. Crowder
// Build Num:  091522



using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using SentinelCore.Abstractions;
using SentinelCore.Contracts.Contracts;
using SentinelCore.Contracts.Events;
using SentinelCore.Contracts.Mcp;
using SentinelCore.Orchestrations.Agents.AgentPresets;
using SentinelCore.Orchestrations.Agents.Middleware;
using SentinelCore.Orchestrations.Agents.Models;
using SentinelCore.Orchestrations.Rag;
using SentinelCore.Orchestrations.Workflows;




namespace SentinelCore.Orchestrations.Agents;





/// <summary>
///     Defines the contract for building <see cref="AIAgent" /> instances from agent profiles.
/// </summary>
public interface ISentinelAgentFactory
{

    /// <summary>
    ///     Builds an <see cref="AIAgent" /> instance from the specified profile.
    /// </summary>
    /// <param name="profile">The agent profile containing configuration details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A fully configured <see cref="AIAgent" /> instance.</returns>
    Task<AIAgent> BuildFromProfileAsync(AgentProfile profile, CancellationToken cancellationToken = default);








    /// <summary>
    ///     Asynchronously creates an instance of <see cref="AIAgent" /> based on the specified preset name.
    /// </summary>
    /// <param name="presetName">
    ///     The name of the preset to use for creating the agent (e.g., "Classifier", "Researcher").
    /// </param>
    /// <param name="taskInstructions">
    ///     Optional additional instructions to customize the agent's behavior, appended to the preset defaults.
    /// </param>
    /// <param name="cancellationToken">
    ///     A token to observe while waiting for the task to complete, enabling cancellation of the operation.
    /// </param>
    /// <param name="responseFormat">
    ///     The format in which the agent's responses should be structured. This parameter is optional.
    /// </param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result is a fully configured
    ///     <see cref="AIAgent" /> instance.
    /// </returns>
    /// <exception cref="ArgumentException">
    ///     Thrown if the specified <paramref name="presetName" /> does not correspond to any registered preset.
    /// </exception>
    Task<AIAgent> CreateAgentAsync(string presetName, string? taskInstructions = null, CancellationToken cancellationToken = default, ChatResponseFormat? responseFormat = null);
}





/// <summary>
///     Provides functionality to create and configure instances of <see cref="AIAgent" />
///     based on the provided <see cref="AgentProfile" />.
///     <para>
///         This factory encapsulates the entire agent construction pipeline, ensuring that
///         the <see cref="AgentProfile" /> serves as the single source of truth. It manages
///         client creation, applies client wrappers (such as safety, logging, and events),
///         configures <see cref="ChatClientAgentOptions.ChatOptions" /> for model tuning,
///         and integrates role-based middleware through the builder pipeline.
///     </para>
///     <para>
///         Model tuning parameters are exclusively handled through
///         <see cref="ChatClientAgentOptions.ChatOptions" />, ensuring a centralized
///         configuration point for such settings.
///     </para>
/// </summary>
public sealed class SentinelAgentFactory : ISentinelAgentFactory
{
    private readonly IChatClientFactory _chatClientFactory;
    private readonly ISentinelCoreEvents _events;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IMcpServerRegistry _mcpServerRegistry;
    private readonly IPatternMatcher _patternMatcher;
    private readonly IAgentPresetProvider _presetProvider;

    private readonly IAgentProfileBuilder _profileBuilder;
    private readonly IOptions<RagSearchOptions> _ragOptions;
    private readonly IRagSearchService _ragSearchService;








    /// <summary>
    ///     Initializes a new instance of the <see cref="SentinelAgentFactory" /> class.
    /// </summary>
    /// <param name="chatClientFactory">Factory for creating chat clients from model profiles.</param>
    /// <param name="events">The event hub for publishing agent activity.</param>
    /// <param name="loggerFactory">The logger factory for trace logging.</param>
    /// <param name="mcpServerRegistry">The MCP server registry that provides connected server tools.</param>
    /// <param name="patternMatcher">The pattern matcher for pattern memory search.</param>
    /// <param name="ragSearchService">The RAG search service for knowledge base queries.</param>
    /// <param name="ragOptions">Configuration options for RAG search.</param>
    /// <param name="presetProvider">The preset provider for resolving agent presets.</param>
    /// <param name="profileBuilder">The profile builder for constructing agent profiles from presets.</param>
    /// <exception cref="ArgumentNullException">Thrown when any required dependency is <c>null</c>.</exception>
    public SentinelAgentFactory(IChatClientFactory chatClientFactory, ISentinelCoreEvents events, ILoggerFactory loggerFactory, IMcpServerRegistry mcpServerRegistry, IPatternMatcher patternMatcher, IRagSearchService ragSearchService, IOptions<RagSearchOptions> ragOptions, IAgentPresetProvider presetProvider, IAgentProfileBuilder profileBuilder)
    {
        _chatClientFactory = chatClientFactory ?? throw new ArgumentNullException(nameof(chatClientFactory));
        _events = events ?? throw new ArgumentNullException(nameof(events));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _mcpServerRegistry = mcpServerRegistry ?? throw new ArgumentNullException(nameof(mcpServerRegistry));
        _patternMatcher = patternMatcher ?? throw new ArgumentNullException(nameof(patternMatcher));
        _ragSearchService = ragSearchService ?? throw new ArgumentNullException(nameof(ragSearchService));
        _ragOptions = ragOptions ?? throw new ArgumentNullException(nameof(ragOptions));
        _presetProvider = presetProvider ?? throw new ArgumentNullException(nameof(presetProvider));
        _profileBuilder = profileBuilder ?? throw new ArgumentNullException(nameof(profileBuilder));
    }








    /// <summary>
    ///     Asynchronously builds an <see cref="AIAgent" /> instance based on the provided <see cref="AgentProfile" />.
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
    public async Task<AIAgent> BuildFromProfileAsync(AgentProfile profile, CancellationToken cancellationToken = default)
    {
        Throw.IfNull(profile);


        // Configuration gate — an agent without a model profile cannot run.
        // There is deliberately no fallback: the user must configure the agent
        // on the Model Configuration page.
        if (profile.Model is null)
        {
            throw new InvalidOperationException($"Agent '{profile.AgentName}' has no model configuration. " + "Open the Model Configuration page and set a provider, model, and endpoint for this agent.");
        }

        // Resolve the preset's middleware flags — the documented pipeline contract.
        // MiddlewareFlags on the preset is the single source of truth for which
        // wrappers and providers each agent role receives (PL-3).
        MiddlewareFlags flags = ResolveMiddlewareFlags(profile.AgentName);

        // 1. Create the chat client from the profile's model configuration.
        IChatClient chatClient = _chatClientFactory.CreateChatClient(profile.Model);

        // 2. Wrap with client middleware per the preset flags (events → logging).
        IChatClient wrappedClient = WrapWithMiddleware(chatClient, profile, flags);

        // 3. Build context providers per the preset flags (pattern memory — Core only, PL-3).
        List<AIContextProvider> additionalContextProviders = BuildContextProviders(profile.AgentName, flags);

        // 4. Build agent options including MCP and RAG tools.
        ChatClientAgentOptions agentOptions = await BuildAgentOptionsAsync(profile, profile.AgentName, additionalContextProviders, cancellationToken).ConfigureAwait(false);

        // 5. Construct the agent. No builder-level safety middleware here: the
        // workflow-level SafetyExecutor gate evaluates every signal before any
        // agent runs, and per PL-3 agent-level safety belongs only to the Core
        // agent — the workflow gate already covers that path.
        ChatClientAgent agent = new(wrappedClient, agentOptions);

        return agent;
    }








    /// <summary>
    ///     This is the single source of agent creation. It takes a preset name and optional task instructions,
    ///     resolves the preset, builds the agent profile, and constructs the agent.
    ///     NOTE: This method is the only entry point for creating agents from presets. All other agent creation methods should funnel through this one. Do not create other forms of creation unless justified with ADR
    /// </summary>
    /// <param name="presetName"></param>
    /// <param name="taskInstructions"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<AIAgent> CreateAgentAsync(string presetName, string? taskInstructions = null, CancellationToken cancellationToken = default, ChatResponseFormat? responseFormat = null)
    {
        // Validate input
        Throw.IfNullOrWhitespace(presetName);
        // Prepare model instructions
        ChatMessages modelInstructions = CreateModelInstructions(presetName, taskInstructions,true);
        // Resolve the preset
        AgentPresetBase preset = ResolvePreset(presetName);
        // Build the agent profile
        AgentProfile profile = BuildAgentProfile(preset, taskInstructions, responseFormat, modelInstructions);
        // Build and return the agent
        return await BuildFromProfileAsync(profile, cancellationToken).ConfigureAwait(false);
    }








    /// <summary>
    ///     Builds <see cref="ChatClientAgentOptions" /> from the profile and role configuration.
    ///     This is the ONLY place model tuning parameters (Temperature, TopP, TopK, MaxOutputTokens)
    ///     are assigned — they flow into <see cref="ChatOptions" /> which the ChatClientAgent reads.
    /// </summary>
    /// <param name="profile">The <see cref="AgentProfile" /> containing base agent configuration.</param>
    /// <param name="logicalAgentName">
    ///     The logical agent name used to filter MCP servers by assignment.
    ///     An empty value or a server with no assignments allows the server to be used by any agent.
    /// </param>
    /// <param name="additionalContextProviders">Additional context providers to include (e.g., RAG injector).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    ///     A task that resolves to a fully configured <see cref="ChatClientAgentOptions" /> instance.
    /// </returns>
    private async Task<ChatClientAgentOptions> BuildAgentOptionsAsync(AgentProfile profile, string logicalAgentName, List<AIContextProvider> additionalContextProviders, CancellationToken cancellationToken)
    {
        var roleInstructions = AgentInstructionConstants.GetAgentPresetInstructions(logicalAgentName);
        ChatOptions chatOptions = new()
        {

            ConversationId = Guid.NewGuid().ToString("N"),
            Instructions = AgentInstructionConstants.CURRENT_PLATFORM_DOMAIN_S + "\n\n" + roleInstructions,

            Temperature = profile.Model!.Temperature,
            MaxOutputTokens = profile.Model.MaxOutputTokens ?? 16000,
            TopP = profile.Model.TopP,
            TopK = profile.Model.TopK,
            // Reasoning = new ReasoningOptions { Effort = ReasoningEffort.Medium, Output = ReasoningOutput.Full },
            ModelId = profile.Model.ModelId,
            ResponseFormat = profile.ResponseFormat
        };

        List<AITool> mcpTools = await GetMcpToolsAsync(logicalAgentName, cancellationToken).ConfigureAwait(false);
        if (mcpTools.Count > 0)
        {
            chatOptions.Tools = chatOptions.Tools is null ? mcpTools : new List<AITool>(chatOptions.Tools.Concat(mcpTools));
        }

        // Add RAG search tools if enabled
        List<AITool> ragTools = GetRagTools(logicalAgentName);
        if (ragTools.Count > 0)
        {
            chatOptions.Tools = chatOptions.Tools is null ? ragTools : new List<AITool>(chatOptions.Tools.Concat(ragTools));
        }

        // Merge context providers: profile providers + additional providers (RAG injector)
        List<AIContextProvider> allContextProviders = new();
        if (profile.AIContextProviders is { Count: > 0 })
        {
            allContextProviders.AddRange(profile.AIContextProviders);
        }

        allContextProviders.AddRange(additionalContextProviders);

        return new ChatClientAgentOptions
        {
            Id = profile.AgentId,
            Name = profile.AgentName,
            Description = "An AI Agent",
            ChatOptions = chatOptions,
            AIContextProviders = allContextProviders.Count > 0 ? allContextProviders : null,
            UseProvidedChatClientAsIs = false,
            ClearOnChatHistoryProviderConflict = false,
            WarnOnChatHistoryProviderConflict = false,
            ThrowOnChatHistoryProviderConflict = true,
            RequirePerServiceCallChatHistoryPersistence = false,
            EnableMessageInjection = false,
            DisableApprovalNotRequiredFunctionBypassing = false,
            DisableApprovalResponseBinding = false
        };
    }








    private AgentProfile BuildAgentProfile(AgentPresetBase preset, string? taskInstructions, ChatResponseFormat? responseFormat, ChatMessages modelInstructions)
    {


        // Build the agent profile from the preset and task instructions, Both AgentId and AgentName are required.
        // Some internals use name and other use ID, ensure both are set for consistency.
        AgentProfile profile = _profileBuilder.BuildFromPreset(preset, taskInstructions);
        if (string.IsNullOrEmpty(profile.AgentName) || string.IsNullOrWhiteSpace(profile.AgentId))
        {
            throw new InvalidOperationException($"Preset '{preset.GetType().Name}' did not provide a valid AgentName.");
        }
        if (string.IsNullOrWhiteSpace(profile.AgentId))
        {
            throw new InvalidOperationException($"Preset '{preset.GetType().Name}' did not provide a valid AgentId.");
        }
        profile.ResponseFormat = responseFormat;
        profile.ModelInstructions = modelInstructions;
        return profile;
    }








    /// <summary>
    /// Creates a collection of system instruction messages for a model by layering the platform domain, the specified
    /// preset's instructions, and optional task instructions.
    /// </summary>
    /// <remarks>Empty or whitespace preset or task instructions are ignored. Preset instructions are obtained
    /// via AgentInstructionConstants.GetAgentPresetInstructions and all entries are added as system messages to
    /// preserve directive priority.</remarks>
    /// <param name="presetName">Agent preset name whose instructions are retrieved and included if present.</param>
    /// <param name="taskInstructions">Optional task-specific instructions appended as the highest-priority system message.</param>
    /// <returns>A ChatMessages instance containing the assembled system messages in order: platform domain, preset instructions
    /// (if any), then task instructions (if any).</returns>
    private static ChatMessages CreateModelInstructions(string presetName, string? taskInstructions, bool overridePreset)
    {
        ChatMessages modelInstructions = new();

        // Layer 1: Base (Lowest) - Shared with each agent
        modelInstructions.AddSystemMessage(AgentInstructionConstants.CURRENT_PLATFORM_DOMAIN_S);

        if (!overridePreset)
        {
            // Layer 2: Preset (Middle) - Per-agent preset instruction
            string presetInstructions = AgentInstructionConstants.GetAgentPresetInstructions(presetName);
            if (!string.IsNullOrWhiteSpace(presetInstructions))
            {
                modelInstructions.AddSystemMessage(presetInstructions);
            }
        }

        // Layer 3: Task (Topmost) - Optional task instructions passed at agent build call
        if (!string.IsNullOrWhiteSpace(taskInstructions))
        {
            modelInstructions.AddUserMessage(taskInstructions);
        }

        return modelInstructions;
    }








    /// <summary>
    ///     Retrieves MCP tools available for the specified logical agent name.
    /// </summary>
    /// <param name="logicalAgentName">
    ///     The logical agent name used to filter MCP servers by assignment.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of available MCP tools.</returns>
    private async Task<List<AITool>> GetMcpToolsAsync(string logicalAgentName, CancellationToken cancellationToken)
    {
        List<AITool> tools = new();

        IReadOnlyList<AITool> serverTools = await _mcpServerRegistry.GetToolsForAgentAsync(logicalAgentName, cancellationToken).ConfigureAwait(false);
        tools.AddRange(serverTools);

        return tools;
    }








    /// <summary>
    ///     Gets RAG search tools for the specified agent.
    /// </summary>
    /// <param name="logicalAgentName">The logical agent name.</param>
    /// <returns>A list of RAG search tools.</returns>
    private List<AITool> GetRagTools(string logicalAgentName)
    {
        if (!_ragOptions.Value.ToolEnabled)
        {
            return new List<AITool>();
        }

        // RAG search tools would be added here when the API stabilizes
        return new List<AITool>();
    }








    private AgentPresetBase ResolvePreset(string presetName)
    {
        AgentPresetBase? preset = _presetProvider.GetPreset(presetName);
        if (preset is null)
        {
            string availablePresets = string.Join(", ", _presetProvider.ListPresets());
            throw new ArgumentException($"No preset found for name '{presetName}'. Available presets: {availablePresets}.", nameof(presetName));
        }

        return preset;
    }








    /// <summary>
    ///     Resolves the <see cref="MiddlewareFlags" /> for a logical agent name from its
    ///     preset. Agents without a preset receive the Utility pipeline (safety gate is
    ///     workflow-level; events + logging are always on for observability).
    /// </summary>
    /// <param name="agentName">The logical agent name.</param>
    /// <returns>The middleware flags declared by the agent's preset.</returns>
    private MiddlewareFlags ResolveMiddlewareFlags(string agentName)
    {
        return _presetProvider.GetPreset(agentName)?.MiddlewareFlags ?? MiddlewareFlags.Utility;
    }

    /// <summary>
    ///     Wraps the chat client with the middleware layers declared by the preset's
    ///     <see cref="MiddlewareFlags" /> (events, logging).
    /// </summary>
    /// <param name="chatClient">The base chat client to wrap.</param>
    /// <param name="profile">The agent profile containing configuration details.</param>
    /// <param name="flags">The preset's middleware flags.</param>
    /// <returns>The wrapped chat client.</returns>
    private IChatClient WrapWithMiddleware(IChatClient chatClient, AgentProfile profile, MiddlewareFlags flags)
    {
        IChatClient current = chatClient;

        // Layer 1: Event wrapper (publishes agent output to the UI event hub).
        if (flags.HasFlag(MiddlewareFlags.Events))
        {
            current = new EventPublishingChatClient(current, _events, profile.AgentName, _loggerFactory.CreateLogger<EventPublishingChatClient>());
        }

        // Layer 2: Logging wrapper (traces request/response).
        if (flags.HasFlag(MiddlewareFlags.Logging))
        {
            current = new LoggingChatClient(current, _loggerFactory.CreateLogger("InnerClientLogger"));
        }

        return current;
    }

    /// <summary>
    ///     Builds the context providers declared by the preset's <see cref="MiddlewareFlags" />.
    ///     Per PL-3, the pattern memory injector is applied only to the Core agent.
    /// </summary>
    /// <param name="agentName">The logical agent name.</param>
    /// <param name="flags">The preset's middleware flags.</param>
    /// <returns>A list of context providers to add to the agent.</returns>
    private List<AIContextProvider> BuildContextProviders(string agentName, MiddlewareFlags flags)
    {
        List<AIContextProvider> providers = new();

        // Pattern memory injection — Core agent only (PL-3). The injector searches
        // pattern memory for similar prior cases and injects them as context.
        if (flags.HasFlag(MiddlewareFlags.PatternMemory))
        {
            providers.Add(new PatternMemoryInjector(_patternMatcher, _loggerFactory.CreateLogger<PatternMemoryInjector>()));
        }

        return providers;
    }
}
