// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         AgentProfile.cs
// Author: Kyle L. Crowder
// Build Num:  091418



using SentinelCore.Contracts.Contracts;
using SentinelCore.Orchestrations.Agents.Models;
using SentinelCore.Orchestrations.Personas;




namespace SentinelCore.Orchestrations.Agents;





/// <summary>
///     Represents the immutable specification for constructing an <see cref="AIAgent" />.
///     This record is utilized by agent factories to produce an <see cref="AgentProfile" /> and delegate
///     the construction process to <see cref="SentinelAgentFactory.BuildFromProfileAsync" />.
///     <para>
///         This system supports a flexible investigation platform using a core workflow and a set of agents known as
///         'TheCore'.
///         'TheCore' orchestration includes advanced tools and instructions that are not fully customizable beyond model
///         parameters.
///         Additionally, other orchestrations are available for various industries or use cases, offering greater
///         flexibility
///         and customization for agents, tools, and instructions.
///     </para>
///     <para>
///         The <see cref="Persona" /> provides unique personality characteristics to the agent. It is not an instruction
///         set but
///         a randomizer that gives similar agents different perspectives, fostering more creative and diverse
///         problem-solving approaches.
///     </para>
/// </summary>
public sealed record AgentProfile
{

    /// <summary>
    ///     Gets or sets the list of <see cref="AIContextProvider" /> instances to attach to the
    ///     agent's context provider pipeline via <see cref="ChatClientAgentOptions.AIContextProviders" />.
    ///     <para>
    ///         Use this for providers that derive from <see cref="AIContextProvider" /> but not from
    ///         <see cref="MessageAIContextProvider" />, such as
    ///         <see cref="Microsoft.Agents.AI.Compaction.CompactionProvider" />.
    ///     </para>
    /// </summary>
    public IList<AIContextProvider> AIContextProviders { get; set; } = new List<AIContextProvider>();

    /// <summary>
    /// Gets or sets the unique identifier for the agent.
    /// </summary>
    /// <value>
    /// A <see cref="string"/> representing the unique identifier of the agent.
    /// </value>
    public string AgentId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the agent.
    /// BOTH AgentId and AgentName are required for the agent to be valid.
    /// </summary>
    /// <value>
    /// A <see cref="string"/> representing the name of the agent.
    /// </value>
    public string AgentName { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the model-specific instructions for the agent.
    ///     These instructions are represented as a collection of <see cref="ChatMessage" /> objects
    ///     and define the contextual guidelines or directives for the agent's behavior.
    ///     <para>
    ///         Instructions are assembled at the call site (executor) using <see cref="InstructionLayerBuilder" />,
    ///         not baked into the agent at construction time. This allows the same agent instance
    ///         to be reused with different instructions per invocation.
    ///     </para>
    /// </summary>
    public ChatMessages ModelInstructions { get; set; } = new ChatMessages();

    /// <summary>
    ///     The model profile for this agent. <c>null</c> means the agent is not
    ///     configured — <see cref="SentinelAgentFactory.BuildFromProfileAsync" />
    ///     rejects such agents with a descriptive error pointing at the Model
    ///     Configuration page.
    /// </summary>
    public ModelProfile? Model { get; set; }

    /// <summary>
    ///     A persona is a unique feature within this platform. It provides an agent with a strong personality characteristic.
    ///     It's not an instruction it more of a randomizer to give 2 like agents a different perspective and allows for more
    ///     'out of the box' thinking.
    /// </summary>
    public AgentPersona? Persona { get; set; }

    /// <summary>
    ///     The list of tools available to the agent. Default empty, set at runtime
    /// </summary>
    public IList<AITool> Tools { get; set; } = new List<AITool>();
}
