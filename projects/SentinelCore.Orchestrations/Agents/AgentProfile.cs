// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         AgentProfile.cs
// Author: Kyle L. Crowder
// Build Num:  091418



using Microsoft.Extensions.Logging;

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
    ///     Gets or initializes the delegate responsible for constructing an <see cref="AIAgent" />.
    /// </summary>
    /// <remarks>
    ///     This property provides a function that takes a <see cref="ChatClientAgent" /> and an <see cref="ILoggerFactory" />
    ///     as parameters and returns an instance of <see cref="AIAgent" />. It is used to define the logic for creating
    ///     agents based on the provided context and logging capabilities.
    /// </remarks>
    public Func<ChatClientAgent, ILoggerFactory, AIAgent>? BuildAgent { get; init; }

    /// <summary>
    ///     Gets or sets the model-specific instructions for the agent.
    ///     These instructions are represented as a collection of <see cref="ChatMessage" /> objects
    ///     and define the contextual guidelines or directives for the agent's behavior.
    ///     <para>
    ///         This property replaces the deprecated <see cref="Instructions" /> property and provides
    ///         a more structured approach to managing agent instructions.
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
    ///     Gets or sets the <see cref="Type" /> used to configure <see cref="ChatResponseFormat" />
    ///     for structured output. When <c>null</c>, no structured output format is applied.
    /// </summary>
    public ChatResponseFormat? ResponseFormat { get; set; }

    /// <summary>
    ///     The list of tools available to the agent. Default empty, set at runtime
    /// </summary>
    public IList<AITool> Tools { get; set; } = new List<AITool>();
    /// <summary>
    /// Not used see constants file
    /// </summary>
    [Obsolete("Being removed from agent construction in favor of per-agent usage function. Agents are reusable and need different instructions. To be generated from constants and added at RunAsync call.")]
    public string Instructions { get; set; } = string.Empty;
}
