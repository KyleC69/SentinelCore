// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         SentinelAgentFactory.cs
// Author: Kyle L. Crowder
// Build Num:  092200



#pragma warning disable MEAI001

using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

using SentinelCore.Abstractions;
using SentinelCore.Contracts.Contracts;
using SentinelCore.Orchestrations.Agents.AgentPresets;
using SentinelCore.Orchestrations.Agents.Models;
using SentinelCore.Orchestrations.Providers;




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
    Task<AIAgent> BuildFromProfileAsync(AgentProfile profile, CancellationToken cancellationToken = default);








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
///         This factory delegates agent construction to a pipeline of
///         <see cref="IAgentConstructionContributor" /> implementations. Each contributor
///         owns exactly one concern (logging, pattern memory, MCP tools, compaction, etc.).
///         Adding a new middleware type requires only a new contributor class and DI
///         registration — no factory modification needed.
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
    private readonly IReadOnlyList<IAgentConstructionContributor> _contributors;
    private readonly IAgentPresetProvider _presetProvider;
    private readonly IAgentProfileBuilder _profileBuilder;








    /// <summary>
    ///     Initializes a new instance of the <see cref="SentinelAgentFactory" /> class.
    /// </summary>
    /// <param name="chatClientFactory">Factory for creating chat clients from model profiles.</param>
    /// <param name="presetProvider">The preset provider for resolving agent presets.</param>
    /// <param name="profileBuilder">The profile builder for constructing agent profiles from presets.</param>
    /// <param name="contributors">
    ///     The pipeline of construction contributors. Each contributor adds one piece
    ///     of agent configuration (logging, pattern memory, MCP tools, etc.).
    ///     Contributors are run in <see cref="IAgentConstructionContributor.Order" /> sequence.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when any required dependency is <c>null</c>.</exception>
    public SentinelAgentFactory(IChatClientFactory chatClientFactory, IAgentPresetProvider presetProvider, IAgentProfileBuilder profileBuilder, IEnumerable<IAgentConstructionContributor> contributors)
    {
        _chatClientFactory = chatClientFactory ?? throw new ArgumentNullException(nameof(chatClientFactory));
        _presetProvider = presetProvider ?? throw new ArgumentNullException(nameof(presetProvider));
        _profileBuilder = profileBuilder ?? throw new ArgumentNullException(nameof(profileBuilder));

        ArgumentNullException.ThrowIfNull(contributors);
        _contributors = contributors.OrderBy(c => c.Order).ToList();
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

        // Resolve the preset to get construction-time flags (logging, pattern memory, etc.)
        AgentPresetDefinition? presetDef = _presetProvider.GetPreset(profile.AgentName);
        if (presetDef is null)
        {
            presetDef = AgentPresetDefinition.UtilityRole(profile.AgentName, profile.AgentId);
        }

        // Create the chat client from the profile's model configuration.
        IChatClient chatClient = _chatClientFactory.CreateChatClient(profile.Model);

        // Build the construction context — contributors will add to this.
        AgentConstructionContext context = new(presetDef, profile.Model, chatClient);

        // Add profile-level tools (set by callers like CaseGenerator before BuildFromProfileAsync).
        if (profile.Tools is { Count: > 0 })
        {
            context.Tools.AddRange(profile.Tools);
        }

        // Add profile-level context providers (set by callers like CaseGenerator).
        if (profile.AIContextProviders is { Count: > 0 })
        {
            context.ContextProviders.AddRange(profile.AIContextProviders);
        }

        // Run the contributor pipeline — each contributor adds its piece.
        foreach (IAgentConstructionContributor contributor in _contributors)
        {
            await contributor.ContributeAsync(context, cancellationToken).ConfigureAwait(false);
        }

        // Build the final agent from accumulated context.
        return BuildAgent(context);
    }








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

        // Resolve the preset
        AgentPresetDefinition preset = ResolvePreset(presetName);

        // Build the agent profile
        AgentProfile profile = BuildAgentProfile(preset, cancellationToken);

        // Build and return the agent via the contributor pipeline
        return await BuildFromProfileAsync(profile, cancellationToken).ConfigureAwait(false);
    }








    /// <summary>
    ///     Constructs the final <see cref="AIAgent" /> from the accumulated construction context.
    /// </summary>
    /// <param name="context">The construction context containing all accumulated decisions.</param>
    /// <returns>A fully configured <see cref="ChatClientAgent" />.</returns>
    private ChatClientAgent BuildAgent(AgentConstructionContext context)
    {
        ChatOptions chatOptions = new()
        {
                ConversationId = Guid.NewGuid().ToString("N"),
                Instructions = "", // Per PL-3: instructions are set at call site, not here
                Temperature = context.Model.Temperature,
                MaxOutputTokens = context.Model.MaxOutputTokens ?? 16000,
                TopP = context.Model.TopP,
                TopK = context.Model.TopK,
                ModelId = context.Model.ModelId,
                AllowMultipleToolCalls = true,
                // ResponseFormat is NOT set here — it is a per-call concern.
                // Executors pass it via AgentRunOptions or typed RunAsync<T> at invocation time.
        };

        if (context.Tools.Count > 0)
        {
            chatOptions.Tools = context.Tools;
        }

        return new ChatClientAgent(context.WrappedClient, new ChatClientAgentOptions
        {
                Id = context.Preset.AgentId,
                Name = context.Preset.AgentName,
                Description = "An AI Agent",
                ChatOptions = chatOptions,
                AIContextProviders = context.ContextProviders.Count > 0 ? context.ContextProviders : null,
                UseProvidedChatClientAsIs = false,
                ClearOnChatHistoryProviderConflict = false,
                WarnOnChatHistoryProviderConflict = false,
                ThrowOnChatHistoryProviderConflict = true,
                RequirePerServiceCallChatHistoryPersistence = false,
                EnableMessageInjection = false,
                DisableApprovalNotRequiredFunctionBypassing = false,
                DisableApprovalResponseBinding = false,
                ChatHistoryProvider = new AdvancedInMemoryChatHistoryProvider(),
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








    private AgentPresetDefinition ResolvePreset(string presetName)
    {
        AgentPresetDefinition? preset = _presetProvider.GetPreset(presetName);
        if (preset is null)
        {
            string availablePresets = string.Join(", ", _presetProvider.ListPresets());
            throw new ArgumentException($"No preset found for name '{presetName}'. Available presets: {availablePresets}.", nameof(presetName));
        }

        return preset;
    }
}
