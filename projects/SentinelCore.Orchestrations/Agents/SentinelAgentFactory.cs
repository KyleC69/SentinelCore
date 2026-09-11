// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         SentinelAgentFactory.cs
// Author: Kyle L. Crowder
// Build Num:  091112



using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using SentinelCore.Abstractions;
using SentinelCore.Contracts.Contracts;
using SentinelCore.Contracts.Events;
using SentinelCore.Contracts.Mcp;
using SentinelCore.Orchestrations.Agents.Middleware;
using SentinelCore.Orchestrations.Rag;
using SentinelCore.Orchestrations.SafetyEngine;
using SentinelCore.Orchestrations.SafetyEngine.Rules;




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
    /// <param name="overrideRole">Optional role override.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A fully configured <see cref="AIAgent" /> instance.</returns>
    Task<AIAgent> BuildFromProfileAsync(AgentProfile profile, AgentRole? overrideRole = null, CancellationToken cancellationToken = default);
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

    /// <summary>
    ///     A container for active agent identifiers and names to prevent collisions.
    ///     Currently this list is not updated as agents are removed. MUST create a hook to properly remove when disposed or
    ///     removed.
    /// </summary>
    public Dictionary<string, string> ActiveAgents = new();

    private readonly IChatClientFactory _chatClientFactory;
    private readonly ISentinelCoreEvents _events;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IMcpServerRegistry _mcpServerRegistry;
    private readonly IPatternMatcher _patternMatcher;
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
    public SentinelAgentFactory(IChatClientFactory chatClientFactory, ISentinelCoreEvents events, ILoggerFactory loggerFactory, IMcpServerRegistry mcpServerRegistry, IPatternMatcher patternMatcher, IRagSearchService ragSearchService, IOptions<RagSearchOptions> ragOptions)
    {
        _chatClientFactory = chatClientFactory ?? throw new ArgumentNullException(nameof(chatClientFactory));
        _events = events ?? throw new ArgumentNullException(nameof(events));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _mcpServerRegistry = mcpServerRegistry ?? throw new ArgumentNullException(nameof(mcpServerRegistry));
        _patternMatcher = patternMatcher ?? throw new ArgumentNullException(nameof(patternMatcher));
        _ragSearchService = ragSearchService ?? throw new ArgumentNullException(nameof(ragSearchService));
        _ragOptions = ragOptions ?? throw new ArgumentNullException(nameof(ragOptions));
    }








    /// <summary>
    ///     Builds an <see cref="AIAgent" /> instance based on the provided <see cref="AgentProfile" />.
    /// </summary>
    /// <param name="profile">
    ///     The <see cref="AgentProfile" /> containing the configuration details for the agent,
    ///     including its role, persona, tools, and model tuning.
    /// </param>
    /// <param name="overrideRole">
    ///     An optional <see cref="AgentRole" /> to override the role specified in the profile.
    /// </param>
    /// <param name="cancellationToken"></param>
    /// <returns>
    ///     A fully configured <see cref="AIAgent" /> tailored to the specified role and profile.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if the <paramref name="profile" /> is <c>null</c>.
    /// </exception>
    /// <remarks>
    ///     This method ensures that the agent's name is unique across all active agents in the platform.
    ///     It also creates and wraps the necessary chat client, applies middleware, and configures the agent
    ///     with the appropriate options.
    /// </remarks>
    public async Task<AIAgent> BuildFromProfileAsync(AgentProfile profile, AgentRole? overrideRole = null, CancellationToken cancellationToken = default)
    {
        Throw.IfNull(profile);


        // Validates the agent name and ensures uniqueness across all active agents within the platform..
        // There is conflicting documentation on which needs to be unique: the AgentId or the AgentName.
        // Capture the logical name requested by the caller before ValidateUniqueAgentName rewrites it for
        // collision avoidance. This is the identity used to decide which MCP servers are available.
        string logicalAgentName = profile.AgentName;

        (string uniqueId, string uniqueName) = ValidateUniqueAgentName(profile.AgentId, profile.AgentName);
        ActiveAgents.Add(uniqueId, uniqueName);
        profile.AgentId = uniqueId;
        profile.AgentName = uniqueName;




        // Synchronously wait for the async method to complete

        // Configuration gate — an agent without a model profile cannot run.
        // There is deliberately no fallback: the user must configure the agent
        // on the Model Configuration page.
        if (profile.Model is null)
        {
            throw new InvalidOperationException($"Agent '{logicalAgentName}' has no model configuration. " + "Open the Model Configuration page and set a provider, model, and endpoint for this agent.");
        }

        // 1. Create the chat client from the profile's model configuration.
        IChatClient chatClient = _chatClientFactory.CreateChatClient(profile.Model);

        // 2. Wrap with middleware (events → logging).
        IChatClient wrappedClient = WrapWithMiddleware(chatClient, profile);

        // 3. Build context providers (RAG injector).
        List<AIContextProvider> additionalContextProviders = BuildContextProviders();

        // 4. Build agent options including MCP and RAG tools.
        ChatClientAgentOptions agentOptions = await BuildAgentOptionsAsync(profile, logicalAgentName, additionalContextProviders, cancellationToken).ConfigureAwait(false);

        // 5. Construct the agent.
        ChatClientAgent agent = new(wrappedClient, agentOptions);

        // 6. Apply builder middleware (safety engine).
        AIAgent finalAgent = ApplyBuilderMiddleware(agent);

        return finalAgent;
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
                ModelId = profile.Model.ModelId,
                Tools = profile.Tools,
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
        var providers = new List<AIContextProvider>();

        // Add RAG context injector if enabled.
        RagContextInjector? ragContextInjector = RagMiddlewarePipeline.CreateContextInjector(_ragOptions, _ragSearchService, _loggerFactory);
        if (ragContextInjector != null)
        {
            providers.Add(ragContextInjector);
        }

        // Add pattern memory injector if pattern matcher is configured.
        PatternMemoryInjector patternInjector = new(_patternMatcher, _loggerFactory.CreateLogger<PatternMemoryInjector>());
        providers.Add(patternInjector);

        return providers;
    }








    internal static List<ISafetyRule> CreateSafetyRules()
    {
        // Explicitly instantiate all safety rules with their required configurations
        // This avoids using Activator.CreateInstance which requires parameterless constructors
        return new List<ISafetyRule>
        {
                // Blocklist Rule - blocks specific terms
                new BlocklistRule("Blocklist", new[] { "malicious", "harmful", "exploit" }, SafetySeverity.High, "Blocks prompts containing blocklisted terms"),

                // Code Injection Detection - SQL, shell, script injection patterns
                new CodeInjectionRule(),

                // Data Exfiltration Detection - API calls, webhooks, email exfiltration
                new DataExfiltrationRule(),

                // Encoding Evasion Detection - base64, URL encoding, Unicode escapes
                new EncodingEvasionRule(),

                // Harmful Content Detection - violence, self-harm, hate speech
                new HarmfulContentRule(),

                // Max Length Rule - prevents excessively long prompts
                new MaxLengthRule(),

                // PII Detection - SSN, credit cards, emails, phone numbers
                new PIIDetectionRule(),

                // Prompt Injection Detection - jailbreak attempts, instruction manipulation
                new PromptInjectionRule(),

                // Regex Block Rule - custom patterns (example with empty patterns)
                new RegexBlockRule("RegexBlock", Array.Empty<string>()),

                // Repetition Attack Detection - detects repetitive text attacks
                new RepetitionAttackRule(),

                // Role Escalation Detection - prevents privilege escalation attempts
                new RoleEscalationRule(),

                // System Prompt Extraction Detection - prevents prompt leakage
                new SystemPromptExtractionRule(),

                // Token Limit Rule - prevents context window exhaustion
                new TokenLimitRule(),

                // URL Block Rule - detects/warns on URLs in prompts
                new UrlBlockRule() // Configure allowed domains if needed

                // Note: CompositeRule is excluded as it requires other ISafetyRule instances as parameters
                // and should be instantiated separately when composing multiple rules together
        };
    }








    /// <summary>
    ///     Retrieves tools from connected MCP servers that are available to the specified agent.
    /// </summary>
    /// <param name="logicalAgentName">The logical agent name to filter server assignments.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    ///     A list of <see cref="AITool" /> instances from connected MCP servers. A server is included
    ///     when it is connected and either has no assigned agent names or its assignments include
    ///     <paramref name="logicalAgentName" />.
    /// </returns>
    private async Task<List<AITool>> GetMcpToolsAsync(string logicalAgentName, CancellationToken cancellationToken)
    {
        IReadOnlyList<AITool> tools = await _mcpServerRegistry.GetToolsForAgentAsync(logicalAgentName, cancellationToken).ConfigureAwait(false);

        return tools.ToList();
    }








    /// <summary>
    ///     Retrieves RAG search tools for the specified agent.
    /// </summary>
    /// <param name="agentName">The agent name to check for RAG tool eligibility.</param>
    /// <returns>A list of RAG tools available to the agent.</returns>
    private List<AITool> GetRagTools(string agentName)
    {
        // Check if RAG tools are enabled globally
        if (!_ragOptions.Value.ToolEnabled || !_ragOptions.Value.Enabled)
        {
            return new List<AITool>();
        }

        // Check if RAG is enabled for this specific agent role
        IReadOnlyList<string> enabledForRoles = _ragOptions.Value.EnabledForAgentRoles;
        if (enabledForRoles.Count > 0 && !enabledForRoles.Contains(agentName, StringComparer.OrdinalIgnoreCase))
        {
            return new List<AITool>();
        }

        // Get RAG tools from the pipeline
        return RagMiddlewarePipeline.GetTools(_ragSearchService, _ragOptions, _loggerFactory).ToList();
    }








    /// <summary>
    ///     Ensures that the agent name and ID are unique across all active agents.
    ///     If the specified <paramref name="agentName" /> or its corresponding ID already exists,
    ///     a numeric suffix is appended to both until a unique combination is found.
    ///     Validates and ensures unique names for agents within the active agent registry.
    ///     This prevents collisions in the agent registry and guarantees unique identification
    ///     for each agent.
    /// </summary>
    /// <param name="agentName">The requested agent name.</param>
    /// <param name="agentId">The requested agent identifier.</param>
    /// <returns>
    ///     A tuple containing:
    ///     <list type="bullet">
    ///         <item>
    ///             <description><c>UniqueId</c>: A unique identifier for the agent.</description>
    ///         </item>
    ///         <item>
    ///             <description><c>UniqueName</c>: A unique name for the agent.</description>
    ///         </item>
    ///     </list>
    /// </returns>
    private (string UniqueId, string UniqueName) ValidateUniqueAgentName(string agentName, string agentId)
    {
        string baseName = agentName;
        string baseId = agentId;
        string uniqueName = baseName;
        string uniqueId = baseId;
        int counter = 1;

        // Keep iterating while either the key (agent name) exists, or the value (agent id)
        // already exists in ActiveAgents.
        while (ActiveAgents.ContainsKey(uniqueName) || ActiveAgents.ContainsValue(uniqueId))
        {
            uniqueName = $"{baseName}_{counter}";
            uniqueId = $"{baseId}_{counter}";
            counter++;
        }

        return (uniqueId, uniqueName);
    }








    /// <summary>
    ///     Wraps the provided base chat client with a logging layer specific to the given agent profile.
    /// </summary>
    /// <param name="innerClient">
    ///     The inner <see cref="IChatClient" /> instance to be wrapped.
    /// </param>
    /// <param name="profile">
    ///     The <see cref="AgentProfile" /> containing metadata about the agent for which the client is being wrapped.
    /// </param>
    /// <returns>
    ///     An <see cref="IChatClient" /> instance that wraps the provided base client with logging.
    /// </returns>
    /// <remarks>
    ///     The wrapping ensures that trace logging is applied to all agents, including the Core agent.
    /// </remarks>
    private IChatClient WrapWithMiddleware(IChatClient innerClient, AgentProfile profile)
    {
        IChatClient eventClient = new EventPublishingChatClient(innerClient, _events, profile.AgentName, _loggerFactory.CreateLogger($"{profile.AgentName}.Events"));

        // Use Microsoft.Extensions.AI.LoggingChatClient for trace logging
        Microsoft.Extensions.AI.LoggingChatClient loggingClient = new(eventClient, _loggerFactory.CreateLogger(profile.AgentName));

        return loggingClient;
    }
}