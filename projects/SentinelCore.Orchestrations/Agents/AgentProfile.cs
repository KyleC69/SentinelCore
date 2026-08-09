// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         AgentProfile.cs
// Author: Kyle L. Crowder
// Build Num:  080801



using Microsoft.Extensions.Logging;

using SentinelCore.Personas;




namespace SentinelCore.Agents;





/// <summary>
///     Represents the immutable specification for constructing an <see cref="AIAgent" />.
///     This record is utilized by agent factories to produce an <see cref="AgentProfile" /> and delegate
///     the construction process to <see cref="SentinelAgentFactory.BuildFromProfile" />.
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
///         The <see cref="AgentRole" /> defines presets for the agent's position (e.g., manager, critic, worker) within an
///         orchestration.
///         It is not the sole determinant of an agent's configuration but controls the base model, tools, and instructions
///         specifically for 'TheCore' orchestration agents.
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
    public string AgentId { get; set; } = string.Empty;
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
    ///     ///     The instructions for the agent. Default empty, set at runtime
    /// </summary>
    public string Instructions { get; set; } = string.Empty;

    public ModelProfile Model { get; set; } = new();

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
    ///     Gets or sets the role assigned to the agent.
    /// </summary>
    /// <remarks>
    ///     The role determines the responsibilities and permissions of the agent within the system.
    /// </remarks>
    public AgentRole Role { get; set; }

    /// <summary>
    ///     The list of tools available to the agent. Default empty, set at runtime
    /// </summary>
    public IList<AITool> Tools { get; set; } = new List<AITool>();
}
