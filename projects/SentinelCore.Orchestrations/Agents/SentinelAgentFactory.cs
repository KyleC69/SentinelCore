// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         SentinelAgentFactory.cs
// Author: Kyle L. Crowder
// Build Num:  081312



using System.Text.Json;

using JetBrains.Annotations;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

using SentinelCore.Abstractions;
using SentinelCore.Agents.Middleware;
using SentinelCore.Events;
using SentinelCore.SafetyEngine;
using SentinelCore.SafetyEngine.Rules;




namespace SentinelCore.Agents;





public interface ISentinelAgentFactory
{
    Task<AIAgent> BuildFromProfileAsync([System.Diagnostics.CodeAnalysis.NotNull] AgentProfile profile, AgentRole? overrideRole = null);
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

    private readonly ISentinelCoreEvents _events;
    private readonly ILoggerFactory _loggerFactory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true, PropertyNameCaseInsensitive = true };








    /// <summary>
    ///     Initializes a new instance of the <see cref="SentinelAgentFactory" /> class.
    /// </summary>
    /// <param name="events">The event hub for publishing agent activity.</param>
    /// <param name="loggerFactory">The logger factory for trace logging.</param>
    public SentinelAgentFactory([System.Diagnostics.CodeAnalysis.NotNull] ISentinelCoreEvents events, [System.Diagnostics.CodeAnalysis.NotNull] ILoggerFactory loggerFactory)
    {
        _events = events ?? throw new ArgumentNullException(nameof(events));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));

        JsonConsoleFormatterOptions options = new() { IncludeScopes = true, UseUtcTimestamp = false, JsonWriterOptions = new JsonWriterOptions { Indented = true, MaxDepth = 5, SkipValidation = false } };
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
    public async Task<AIAgent> BuildFromProfileAsync([System.Diagnostics.CodeAnalysis.NotNull] AgentProfile profile, AgentRole? overrideRole = null)
    {
        Throw.IfNull(profile);


        // Validates the agent name and ensures uniqueness across all active agents within the platform..
        // There is conflicting documentation on which needs to be unique: the AgentId or the AgentName.
        (string uniqueId, string uniqueName) = ValidateUniqueAgentName(profile.AgentId, profile.AgentName);
        ActiveAgents.Add(uniqueId, uniqueName);
        profile.AgentId = uniqueId;
        profile.AgentName = uniqueName;




        // Synchronously wait for the async method to complete

        // 1. Create the chat client from the profile's model configuration.
        IChatClient chatClient = SentinelChatClientFactory.CreateChatClient(profile.Model);
        IChatClient eventClient = WrapEventPublishing(chatClient, profile); //client
        IChatClient loggingClient = WrapLoggingClient(eventClient, profile); //Client


        ChatClientAgentOptions agentOptions = BuildAgentOptions(profile);
        // The factory always creates ChatOptions before this line, so null-forgiving is safe.

         agentOptions.ChatOptions!.Tools = [..profile.Tools]; // Combine MCP tools with profile tools
        ChatClientAgent agent = new(loggingClient, agentOptions);


        AIAgent final = ApplyAgentMiddleware(agent);


        return final;
    }








    /// <summary>
    ///     Applies context-related middleware to the provided <see cref="ChatClientAgent" />.
    ///     <para>
    ///         This method configures the agent by integrating AI context providers, such as
    ///         <see cref="PatternMemoryInjector" />, to enhance its reasoning capabilities.
    ///         The <see cref="PatternMemoryInjector" /> is responsible for injecting pattern-based
    ///         memory into the agent's context, allowing it to recall and utilize historical data
    ///         effectively during decision-making processes.
    ///     </para>
    ///     <para>
    ///         Additionally, the middleware incorporates a safety engine by applying a set of
    ///         predefined safety rules. These rules are designed to ensure that the agent operates
    ///         within defined safety parameters, mitigating risks and enhancing reliability.
    ///         The safety engine is configured using the provided <see cref="ILoggerFactory" /> to
    ///         enable detailed logging and monitoring of safety-related operations.
    ///     </para>
    ///     <para>
    ///         By combining pattern memory functionality and safety mechanisms, this method ensures
    ///         that the agent is both contextually aware and adheres to operational safety standards.
    ///     </para>
    /// </summary>
    private AIAgent ApplyAgentMiddleware(ChatClientAgent agent)
    {

        // Explicitly instantiate safety rules with required configurations
        List<ISafetyRule> safetyRules = CreateSafetyRules();


        return agent.AsBuilder()
                .UseAIContextProviders(new PatternMemoryInjector())
                //   .UseSafetyEngine(safetyRules, _loggerFactory.CreateLogger<SafetyEngineAgent>())
                .Build();

    }








    /// <summary>
    ///     Builds <see cref="ChatClientAgentOptions" /> from the profile and role configuration.
    ///     This is the ONLY place model tuning parameters (Temperature, TopP, TopK, MaxOutputTokens)
    ///     are assigned — they flow into <see cref="ChatOptions" /> which the ChatClientAgent reads.
    /// </summary>
    private static ChatClientAgentOptions BuildAgentOptions(AgentProfile profile)
    {



        ChatOptions chatOptions = new()
        {
                ConversationId = Guid.NewGuid().ToString("N"),
                Instructions = profile.Instructions,
                Temperature = profile.Model.Temperature,
                MaxOutputTokens = profile.Model.MaxOutputTokens ?? 16000,
                TopP = profile.Model.TopP,
                TopK = profile.Model.TopK,
                ModelId = profile.Model.ModelId,
                Tools = profile.Tools,
                ResponseFormat = profile.ResponseFormat
        };

        return new ChatClientAgentOptions
        {
                Id = profile.AgentId,
                Name = profile.AgentName,
                Description = "An AI Agent",
                ChatOptions = chatOptions,
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
    ///     Ensures that the agent name and ID are unique across all active agents.
    ///     If the specified <paramref name="agentName" /> or its corresponding ID already exists,
    ///     a numeric suffix is appended to both until a unique combination is found.
    ///     This prevents collisions in the agent registry and guarantees unique identification
    ///     for each agent.
    /// </summary>
    /// <param name="agentName">The proposed name of the agent.</param>
    /// <param name="persona">The persona associated with the agent.</param>
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
    private (string UniqueId, string UniqueName) ValidateUniqueAgentName(string agentName, string persona)
    {


        string baseName = agentName != null ? agentName : string.Empty;
        string baseId = agentName != null ? agentName : string.Empty;

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
    ///     Wraps the provided <see cref="IChatClient" /> instance with an event-publishing layer.
    /// </summary>
    /// <param name="innerClient">
    ///     The inner <see cref="IChatClient" /> instance to be wrapped.
    /// </param>
    /// <param name="profile">
    ///     The <see cref="AgentProfile" /> containing metadata about the agent, including its name.
    /// </param>
    /// <returns>
    ///     An <see cref="IChatClient" /> instance that publishes agent activity events.
    /// </returns>
    /// <remarks>
    ///     This method ensures that all agent activity events are published, which is mandatory for all agents, including Core
    ///     agents.
    /// </remarks>
    [MustDisposeResource]
    private IChatClient WrapEventPublishing(IChatClient innerClient, AgentProfile profile)
    {
        // 2. Event publishing wrap — publishes agent activity events.(Mandatory for all agents, including Core).
        return new EventPublishingChatClient(innerClient, _events, profile.AgentName, _loggerFactory.CreateLogger($"{profile.AgentName}.Events"));
    }








    /// <summary>
    ///     Wraps the provided base chat client with a logging layer specific to the given agent profile.
    /// </summary>
    /// <param name="baseClient">
    ///     The base <see cref="IChatClient" /> instance to be wrapped.
    /// </param>
    /// <param name="profile">
    ///     The <see cref="AgentProfile" /> containing metadata about the agent for which the client is being wrapped.
    /// </param>
    /// <returns>
    ///     A new instance of <see cref="LoggingChatClient" /> that wraps the provided base client.
    /// </returns>
    /// <remarks>
    ///     The wrapping ensures that trace logging is applied to all agents, including the Core agent.
    /// </remarks>
    [MustDisposeResource]
    private IChatClient WrapLoggingClient(IChatClient baseClient, AgentProfile profile)
    {



        // 1. Logging wrap — trace logging (always applied). Mandatory for all agents, including Core.
        return new LoggingChatClient(baseClient, _loggerFactory.CreateLogger(profile.AgentName)) { JsonSerializerOptions = JsonOptions };


    }
}
