// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         ISentinelAgentFactory.cs
// Author: Kyle L. Crowder
// Build Num:  111504



using SentinelCore.Orchestrations.Agents.AgentPresets;




namespace SentinelCore.Orchestrations.Abstractions;





/// <summary>
///     Defines the contract for building <see cref="AIAgent" /> instances from agent presets.
/// </summary>
public interface ISentinelAgentFactory
{



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
    Task<AIAgent> CreateAgentAsync(AgentPreset presetName, CancellationToken cancellationToken = default);








    Task<AIAgent> CreateAgentAsync(AgentPreset presetName, string taskInstructions, CancellationToken cancellationToken = default);



    Task<AIAgent> CreateAgentAsync(AgentPreset presetName, string taskInstructions, ChatResponseFormatJson forJsonSchema, CancellationToken none);


    Task<AIAgent> CreateAgentAsync(AgentPreset presetName, string agentName, string agentDescription, string taskInstructions);


    Task<AIAgent> CreateAgentAsync(AgentPreset presetName, string agentName, string agentDescription);


    /// <summary>
    ///     Asynchronously creates an instance of <see cref="AIAgent" /> with an explicit tool selection.
    ///     Use this overload to choose the agent's toolset from the call site instead of relying on the
    ///     preset default.
    /// </summary>
    /// <param name="presetName">The preset that selects the agent's role and default capabilities.</param>
    /// <param name="toolSelection">
    ///     The tools to bind to the agent. May reference MCP-server tools, local in-process tools, or both.
    /// </param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result is a fully configured
    ///     <see cref="AIAgent" /> instance.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="toolSelection" /> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when a named tool cannot be resolved, when a local tool name collides with another tool,
    ///     or when the Manager preset is given a non-empty toolset.
    /// </exception>
    Task<AIAgent> CreateAgentAsync(AgentPreset presetName, AgentToolSelection toolSelection, CancellationToken cancellationToken = default);


    /// <summary>
    ///     Asynchronously creates an instance of <see cref="AIAgent" /> with an explicit tool selection.
    /// </summary>
    /// <param name="presetName">The preset that selects the agent's role and default capabilities.</param>
    /// <param name="toolSelection">The tools to bind to the agent.</param>
    /// <param name="taskInstructions">Optional call-site instructions for this agent instance.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result is a fully configured
    ///     <see cref="AIAgent" /> instance.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="toolSelection" /> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when a named tool cannot be resolved, when a local tool name collides with another tool,
    ///     or when the Manager preset is given a non-empty toolset.
    /// </exception>
    Task<AIAgent> CreateAgentAsync(AgentPreset presetName, AgentToolSelection toolSelection, string? taskInstructions, CancellationToken cancellationToken = default);


    /// <summary>
    ///     Asynchronously creates an instance of <see cref="AIAgent" /> with an explicit tool selection that
    ///     must answer with JSON conforming to the supplied schema.
    /// </summary>
    /// <param name="presetName">The preset that selects the agent's role and default capabilities.</param>
    /// <param name="toolSelection">The tools to bind to the agent.</param>
    /// <param name="taskInstructions">Optional call-site instructions for this agent instance.</param>
    /// <param name="forJsonSchema">When supplied, the agent must answer with JSON matching this schema.</param>
    /// <param name="token">A token to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result is a fully configured
    ///     <see cref="AIAgent" /> instance.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="toolSelection" /> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when a named tool cannot be resolved, when a local tool name collides with another tool,
    ///     or when the Manager preset is given a non-empty toolset.
    /// </exception>
    Task<AIAgent> CreateAgentAsync(AgentPreset presetName, AgentToolSelection toolSelection, string? taskInstructions, ChatResponseFormatJson? forJsonSchema, CancellationToken token);


    /// <summary>
    ///     Asynchronously creates a named <see cref="AIAgent" /> with an explicit tool selection, overriding
    ///     the preset's name and description.
    /// </summary>
    /// <param name="presetName">The preset that selects the agent's role and default capabilities.</param>
    /// <param name="agentName">The name assigned to this agent instance.</param>
    /// <param name="agentDescription">The description assigned to this agent instance.</param>
    /// <param name="toolSelection">The tools to bind to the agent.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result is a fully configured
    ///     <see cref="AIAgent" /> instance.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="toolSelection" /> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when a named tool cannot be resolved, when a local tool name collides with another tool,
    ///     or when the Manager preset is given a non-empty toolset.
    /// </exception>
    Task<AIAgent> CreateAgentAsync(AgentPreset presetName, string agentName, string agentDescription, AgentToolSelection toolSelection);


    /// <summary>
    ///     Asynchronously creates a named <see cref="AIAgent" /> with an explicit tool selection, overriding
    ///     the preset's name and description.
    /// </summary>
    /// <param name="presetName">The preset that selects the agent's role and default capabilities.</param>
    /// <param name="agentName">The name assigned to this agent instance.</param>
    /// <param name="agentDescription">The description assigned to this agent instance.</param>
    /// <param name="taskInstructions">Optional call-site instructions for this agent instance.</param>
    /// <param name="toolSelection">The tools to bind to the agent.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result is a fully configured
    ///     <see cref="AIAgent" /> instance.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="toolSelection" /> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when a named tool cannot be resolved, when a local tool name collides with another tool,
    ///     or when the Manager preset is given a non-empty toolset.
    /// </exception>
    Task<AIAgent> CreateAgentAsync(AgentPreset presetName, string agentName, string agentDescription, string? taskInstructions, AgentToolSelection toolSelection);
}
