// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         ISentinelAgentFactory.cs
// Author: Kyle L. Crowder
// Build Num:  111816



using SentinelCore.Orchestrations.Agents.AgentPresets;




namespace SentinelCore.Orchestrations.Abstractions;





/// <summary>
///     Creates Sentinel Core agents from reusable presets, binding toolsets from the live tool catalogue at
///     construction time.
/// </summary>
/// <remarks>
///     <para>
///         Every agent must be constructed through this factory. <c>ChatClientAgent</c> is never constructed
///         directly by call sites.
///     </para>
///     <para>
///         A preset declares the default tool groups for the agent role it represents. An
///         <see cref="AgentToolSelection" /> supplied at the call site is an override: it replaces the preset's
///         groups rather than unioning with them, so a call site can reduce an agent's toolset.
///     </para>
/// </remarks>
public interface ISentinelAgentFactory
{
    /// <summary>
    ///     Creates an agent from the preset's default configuration: default name, description and preset tool groups.
    /// </summary>
    /// <param name="presetName">The preset that supplies the agent's role, instructions and default tool groups.</param>
    /// <param name="cancellationToken">A token to cancel the construction.</param>
    /// <returns>The constructed agent.</returns>
    Task<AIAgent> CreateAgentAsync(AgentPreset presetName, CancellationToken cancellationToken = default);


    /// <summary>
    ///     Creates an agent from the preset's default configuration, appending task-specific instructions.
    /// </summary>
    /// <param name="presetName">The preset that supplies the agent's role, instructions and default tool groups.</param>
    /// <param name="taskInstructions">Task-specific instructions appended to the preset instructions; may be <c>null</c>.</param>
    /// <param name="cancellationToken">A token to cancel the construction.</param>
    /// <returns>The constructed agent.</returns>
    Task<AIAgent> CreateAgentAsync(AgentPreset presetName, string? taskInstructions, CancellationToken cancellationToken = default);


    /// <summary>
    ///     Creates an agent from the preset's default configuration, appending task-specific instructions and
    ///     constraining responses to a JSON schema.
    /// </summary>
    /// <param name="presetName">The preset that supplies the agent's role, instructions and default tool groups.</param>
    /// <param name="taskInstructions">Task-specific instructions appended to the preset instructions; may be <c>null</c>.</param>
    /// <param name="forJsonSchema">The JSON response format to enforce; may be <c>null</c> for free-form text.</param>
    /// <param name="token">A token to cancel the construction.</param>
    /// <returns>The constructed agent.</returns>
    Task<AIAgent> CreateAgentAsync(AgentPreset presetName, string? taskInstructions, ChatResponseFormatJson? forJsonSchema, CancellationToken token);


    /// <summary>
    ///     Creates an agent from the preset's default configuration with an explicit name and description, appending
    ///     task-specific instructions.
    /// </summary>
    /// <param name="presetName">The preset that supplies the agent's role and default tool groups.</param>
    /// <param name="agentName">The display name to give the agent instance, overriding the preset default.</param>
    /// <param name="agentDescription">The description to give the agent instance, overriding the preset default.</param>
    /// <param name="taskInstructions">Task-specific instructions appended to the preset instructions.</param>
    /// <returns>The constructed agent.</returns>
    Task<AIAgent> CreateAgentAsync(AgentPreset presetName, string agentName, string agentDescription, string taskInstructions);


    /// <summary>
    ///     Creates an agent from the preset's default configuration with an explicit name and description.
    /// </summary>
    /// <param name="presetName">The preset that supplies the agent's role and default tool groups.</param>
    /// <param name="agentName">The display name to give the agent instance, overriding the preset default.</param>
    /// <param name="agentDescription">The description to give the agent instance, overriding the preset default.</param>
    /// <returns>The constructed agent.</returns>
    Task<AIAgent> CreateAgentAsync(AgentPreset presetName, string agentName, string agentDescription);


    /// <summary>
    ///     Creates an agent from the preset with an explicit tool selection overriding the preset's tool groups.
    /// </summary>
    /// <param name="presetName">The preset that supplies the agent's role and instructions.</param>
    /// <param name="toolSelection">The toolset to bind, replacing the preset's tool groups.</param>
    /// <param name="cancellationToken">A token to cancel the construction.</param>
    /// <returns>The constructed agent.</returns>
    Task<AIAgent> CreateAgentAsync(AgentPreset presetName, AgentToolSelection toolSelection, CancellationToken cancellationToken = default);


    /// <summary>
    ///     Creates an agent from the preset with an explicit tool selection, appending task-specific instructions and
    ///     optionally constraining responses to a JSON schema.
    /// </summary>
    /// <param name="presetName">The preset that supplies the agent's role and instructions.</param>
    /// <param name="toolSelection">The toolset to bind, replacing the preset's tool groups.</param>
    /// <param name="taskInstructions">Task-specific instructions appended to the preset instructions; may be <c>null</c>.</param>
    /// <param name="forJsonSchema">The JSON response format to enforce; may be <c>null</c> for free-form text.</param>
    /// <param name="cancellationToken">A token to cancel the construction.</param>
    /// <returns>The constructed agent.</returns>
    Task<AIAgent> CreateAgentAsync(AgentPreset presetName, AgentToolSelection toolSelection, string? taskInstructions, ChatResponseFormat? forJsonSchema = null, CancellationToken cancellationToken = default);


    /// <summary>
    ///     Creates an agent from the preset with an explicit name, description and tool selection.
    /// </summary>
    /// <param name="presetName">The preset that supplies the agent's role and instructions.</param>
    /// <param name="agentName">The display name to give the agent instance, overriding the preset default.</param>
    /// <param name="agentDescription">The description to give the agent instance, overriding the preset default.</param>
    /// <param name="toolSelection">The toolset to bind, replacing the preset's tool groups.</param>
    /// <returns>The constructed agent.</returns>
    Task<AIAgent> CreateAgentAsync(AgentPreset presetName, string agentName, string agentDescription, AgentToolSelection toolSelection);


    /// <summary>
    ///     Creates an agent from the preset with an explicit name, description, tool selection and task instructions.
    /// </summary>
    /// <param name="presetName">The preset that supplies the agent's role and instructions.</param>
    /// <param name="agentName">The display name to give the agent instance, overriding the preset default.</param>
    /// <param name="agentDescription">The description to give the agent instance, overriding the preset default.</param>
    /// <param name="taskInstructions">Task-specific instructions appended to the preset instructions; may be <c>null</c>.</param>
    /// <param name="toolSelection">The toolset to bind, replacing the preset's tool groups.</param>
    /// <returns>The constructed agent.</returns>
    Task<AIAgent> CreateAgentAsync(AgentPreset presetName, string agentName, string agentDescription, string? taskInstructions, AgentToolSelection toolSelection);








    /// <summary>
    ///     Refreshes the tool cache by querying the MCP server registry for the latest tools.
    ///     This should be called when the MCP server registry changes (e.g., after servers are started).
    /// </summary>
    void RefreshToolCache();
}
