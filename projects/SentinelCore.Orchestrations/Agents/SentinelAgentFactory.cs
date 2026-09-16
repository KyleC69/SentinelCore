// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         SentinelAgentFactory.cs
// Author: Kyle L. Crowder
// Build Num:  091418



using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using SentinelCore.Abstractions;
using SentinelCore.Contracts.Contracts;
using SentinelCore.Contracts.Events;
using SentinelCore.Contracts.Mcp;
using SentinelCore.Orchestrations.Agents.AgentPresets;
using SentinelCore.Orchestrations.Agents.Middleware;
using SentinelCore.Orchestrations.Rag;
using SentinelCore.Orchestrations.SafetyEngine;




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
    Task<AIAgent> CreateAgentAsync(string presetName, string? taskInstructions = null, CancellationToken cancellationToken = default, ChatResponseFormat responseFormat = null);
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
    public Dictionary<string, string> ActiveAgents = new();
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
    public SentinelAgentFactory(IChatClientFactory chatClientFactory, ISentinelCoreEvents events, ILoggerFactory loggerFactory, IMcpServerRegistry mcpServerRegistry, IPatternMatcher patternMatcher, IRagSearchService ragSearchService, IOptions<RagSearchOptions> ragOptions, IAgentPresetProvider? presetProvider = null, IAgentProfileBuilder? profileBuilder = null)
    {
        _chatClientFactory = chatClientFactory ?? throw new ArgumentNullException(nameof(chatClientFactory));
        _events = events ?? throw new ArgumentNullException(nameof(events));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _mcpServerRegistry = mcpServerRegistry ?? throw new ArgumentNullException(nameof(mcpServerRegistry));
        _patternMatcher = patternMatcher ?? throw new ArgumentNullException(nameof(patternMatcher));
        _ragSearchService = ragSearchService ?? throw new ArgumentNullException(nameof(ragSearchService));
        _ragOptions = ragOptions ?? throw new ArgumentNullException(nameof(ragOptions));
        _presetProvider = presetProvider ?? new AgentPresetProvider();
        _profileBuilder = profileBuilder ?? new AgentProfileBuilder(Options.Create(new SentinelCoreSettings()));
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

        // 1. Create the chat client from the profile's model configuration.
        IChatClient chatClient = _chatClientFactory.CreateChatClient(profile.Model);

        // 2. Wrap with middleware (events → logging).
        IChatClient wrappedClient = WrapWithMiddleware(chatClient, profile);

        // 3. Build context providers (RAG injector).
        List<AIContextProvider> additionalContextProviders = BuildContextProviders();

        // 4. Build agent options including MCP and RAG tools.
        ChatClientAgentOptions agentOptions = await BuildAgentOptionsAsync(profile, profile.AgentName, additionalContextProviders, cancellationToken).ConfigureAwait(false);

        // 5. Construct the agent.
        ChatClientAgent agent = new(wrappedClient, agentOptions);

        // 6. Apply builder middleware (safety engine).
        AIAgent finalAgent = ApplyBuilderMiddleware(agent);

        return finalAgent;
    }








    /// <summary>
    /// </summary>
    /// <param name="presetName"></param>
    /// <param name="taskInstructions"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<AIAgent> CreateAgentAsync(string presetName, string? taskInstructions = null, CancellationToken cancellationToken = default, ChatResponseFormat? responseFormat = null)
    {
        Throw.IfNullOrWhitespace(presetName);

        // 1. Resolve the preset
        AgentPresetBase? preset = _presetProvider.GetPreset(presetName);
        if (preset is null)
        {
            throw new ArgumentException($"No preset found for name '{presetName}'. Available presets: {string.Join(", ", _presetProvider.ListPresets())}.", nameof(presetName));
        }

        // 2. Build the profile from preset
        AgentProfile profile = _profileBuilder.BuildFromPreset(preset, taskInstructions);
        profile.ResponseFormat = responseFormat;
        profile.Instructions = taskInstructions ?? profile.Instructions;
        // 3. Build and return the agent
        return await BuildFromProfileAsync(profile, cancellationToken).ConfigureAwait(false);
    }








    /// <summary>
    ///     Applies builder-level middleware to the agent, including the safety engine.
    /// </summary>
    /// <param name="agent">The chat client agent to apply middleware to.</param>
    /// <returns>The agent with middleware applied.</returns>
    private AIAgent ApplyBuilderMiddleware(ChatClientAgent agent)
    {
        // Note: Context providers (including RAG injector) are added via
        // ChatClientAgentOptions.AIContextProviders in BuildAgentOptionsAsync().
        // This method only applies builder-level middleware (safety engine).

        // Explicitly instantiate safety rules with required configurations.
        List<ISafetyRule> safetyRules = CreateSafetyRules();

        // Configure safety engine options.
        SafetyEngineOptions safetyOptions = new() { StopOnFirstBlock = true, TreatRuleErrorsAsBlocks = true };

        // Apply safety engine middleware via builder extension.
        return agent.AsBuilder().UseSafetyEngine(safetyRules, _loggerFactory.CreateLogger<SafetyEngineAgent>(), safetyOptions).Build();
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
        ChatOptions chatOptions = new()
        {
                ConversationId = Guid.NewGuid().ToString("N"),
                Instructions = profile.Instructions,
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
                ThrowOnChatHistoryProviderConflict = false,
                RequirePerServiceCallChatHistoryPersistence = false,
                EnableMessageInjection = false,
                DisableApprovalNotRequiredFunctionBypassing = false,
                DisableApprovalResponseBinding = false
        };
    }








    /// <summary>
    ///     Builds the list of context providers for the agent, including RAG context injector.
    /// </summary>
    /// <returns>A list of context providers to add to the agent.</returns>
    private List<AIContextProvider> BuildContextProviders()
    {
        List<AIContextProvider> providers = new();

        // RAG context injector is stubbed — MAF AIContextProvider API is not yet stable
        // When the API stabilizes, uncomment the following:
        // if (_ragOptions.Value.EnableContextInjection)
        // {
        //     providers.Add(new RagContextInjector(_ragSearchService, _ragOptions.Value));
        // }

        return providers;
    }








    /// <summary>
    ///     Creates the safety rules for the safety engine.
    /// </summary>
    /// <returns>A list of configured safety rules.</returns>
    private List<ISafetyRule> CreateSafetyRules()
    {
        // Return empty list — rules are configured via SafetyEngineSettings
        // This is a placeholder for future explicit rule instantiation if needed.
        return new List<ISafetyRule>();
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








    /// <summary>
    ///     Wraps the chat client with middleware layers (events, logging).
    /// </summary>
    /// <param name="chatClient">The base chat client to wrap.</param>
    /// <param name="profile">The agent profile containing configuration details.</param>
    /// <returns>The wrapped chat client.</returns>
    private IChatClient WrapWithMiddleware(IChatClient chatClient, AgentProfile profile)
    {
        // Layer 1: Event wrapper (publishes Before/AfterInvoke events)
        IChatClient eventClient = new EventPublishingChatClient(chatClient, _events, profile.AgentName, _loggerFactory.CreateLogger<EventPublishingChatClient>());

        // Layer 2: Logging wrapper (traces request/response)
        IChatClient loggingClient = new LoggingChatClient(eventClient, _loggerFactory.CreateLogger("InnerClientLogger"));



        return loggingClient;
    }
}