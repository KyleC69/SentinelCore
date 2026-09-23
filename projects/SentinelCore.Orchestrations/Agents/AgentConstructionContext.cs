// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         AgentConstructionContext.cs
// Author: Kyle L. Crowder
// Build Num:  092308



using SentinelCore.Contracts.Contracts;
using SentinelCore.Orchestrations.Agents.AgentPresets;




namespace SentinelCore.Orchestrations.Agents;





/// <summary>
///     Accumulator for agent construction decisions. Each <see cref="IAgentConstructionContributor" />
///     inspects the <see cref="Preset" /> and adds its piece to this context.
///     The factory reads from this context at the end to build the final <see cref="AIAgent" />.
///     <para>
///         This context carries construction-time concerns only (chat client, wrappers,
///         context providers, tools). Invocation-time concerns (instructions, response format)
///         are handled by executors at <c>RunAsync</c> call time.
///     </para>
/// </summary>
public sealed class AgentConstructionContext
{



    /// <summary>
    ///     Initializes a new instance of the <see cref="AgentConstructionContext" /> class.
    /// </summary>
    /// <param name="preset">The preset definition for the agent being constructed.</param>
    /// <param name="model">The model profile resolved from settings.</param>
    /// <param name="chatClient">The base chat client created from the model profile.</param>
    /// <exception cref="ArgumentNullException">Thrown when any required argument is null.</exception>
    public AgentConstructionContext(AgentPresetDefinition preset, ModelProfile model, IChatClient chatClient)
    {
        ArgumentNullException.ThrowIfNull(preset);
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(chatClient);

        Preset = preset;
        Model = model;
        ChatClient = chatClient;
        WrappedClient = chatClient;
    }








    /// <summary>
    ///     Gets the base chat client created from the model profile.
    ///     Contributors may wrap this client (e.g., logging decorator).
    /// </summary>
    public IChatClient ChatClient { get; }

    /// <summary>
    ///     Gets the list of context providers to include in the agent's
    ///     <see cref="ChatClientAgentOptions" />. Contributors add providers here
    ///     (pattern memory, compaction, etc.).
    /// </summary>
    public List<AIContextProvider> ContextProviders { get; } = [];

    /// <summary>
    ///     Gets the model profile resolved from settings for this agent.
    /// </summary>
    public ModelProfile Model { get; }

    /// <summary>
    ///     Gets the preset definition that drives this agent's construction.
    /// </summary>
    public AgentPresetDefinition Preset { get; }

    /// <summary>
    ///     Gets the list of tools to include in the agent's <see cref="ChatOptions" />.
    ///     Contributors add tools here (MCP tools, RAG tools, etc.).
    /// </summary>
    public List<AITool> Tools { get; } = [];

    /// <summary>
    ///     Gets the wrapped chat client. Contributors should add wrappers here
    ///     by reassigning this property. The factory reads this at the end
    ///     to construct the <see cref="ChatClientAgent" />.
    /// </summary>
    public IChatClient WrappedClient { get; set; }
}