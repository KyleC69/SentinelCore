// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         IAgentPresetProvider.cs
// Author: Kyle L. Crowder
// Build Num:  092308



using SentinelCore.Orchestrations.Agents.AgentPresets;




namespace SentinelCore.Orchestrations.Agents;





/// <summary>
///     Provides access to agent preset configurations.
/// </summary>
public interface IAgentPresetProvider
{

    /// <summary>
    ///     Gets all registered presets.
    /// </summary>
    /// <returns>A read-only list of all agent presets.</returns>
    IReadOnlyList<AgentPresetDefinition> GetAllPresets();








    /// <summary>
    ///     Gets the preset for the specified agent name.
    /// </summary>
    /// <param name="agentName">The agent name to look up.</param>
    /// <returns>The preset if found, otherwise <c>null</c>.</returns>
    AgentPresetDefinition? GetPreset(string agentName);








    /// <summary>
    ///     Gets the names of all registered presets.
    /// </summary>
    /// <returns>A read-only list of preset names.</returns>
    IReadOnlyList<string> ListPresets();
}