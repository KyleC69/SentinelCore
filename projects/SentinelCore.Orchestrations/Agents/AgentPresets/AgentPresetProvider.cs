// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         AgentPresetProvider.cs
// Author: Kyle L. Crowder
// Build Num:  091418



using SentinelCore.Orchestrations.Personas;




namespace SentinelCore.Orchestrations.Agents.AgentPresets;





/// <summary>
///     Provides access to agent preset configurations.
/// </summary>
public interface IAgentPresetProvider
{

    /// <summary>
    ///     Gets all registered presets.
    /// </summary>
    /// <returns>A read-only list of all agent presets.</returns>
    IReadOnlyList<AgentPresetBase> GetAllPresets();








    /// <summary>
    ///     Gets the preset for the specified agent name.
    /// </summary>
    /// <param name="agentName">The agent name to look up.</param>
    /// <returns>The preset if found, otherwise <c>null</c>.</returns>
    AgentPresetBase? GetPreset(string agentName);








    /// <summary>
    ///     Gets the names of all registered presets.
    /// </summary>
    /// <returns>A read-only list of preset names.</returns>
    IReadOnlyList<string> ListPresets();
}





/// <summary>
///     Default implementation of <see cref="IAgentPresetProvider" /> that provides
///     all built-in agent presets (matching SentinelAgentCatalog names).
/// </summary>
public sealed class AgentPresetProvider : IAgentPresetProvider
{
    private readonly Dictionary<string, AgentPresetBase> _presets;








    /// <summary>
    ///     Initializes a new instance of the <see cref="AgentPresetProvider" /> class.
    /// </summary>
    public AgentPresetProvider()
    {
        AgentPresetBase[] presets =
        [
                new CoreChatPreset(),
                new ClassifierPreset(),
                new TheCorePreset(),
                new SafetyAgentPreset(),
                new ManagerPreset(),
                new Worker1Preset(),
                new Worker2Preset(),
                new Worker3Preset()
        ];

        _presets = new Dictionary<string, AgentPresetBase>(StringComparer.OrdinalIgnoreCase);
        foreach (AgentPresetBase preset in presets)
        {
            _presets[preset.AgentName] = preset;
        }
    }








    /// <inheritdoc />
    public IReadOnlyList<AgentPresetBase> GetAllPresets()
    {
        return _presets.Values.ToList().AsReadOnly();
    }








    /// <inheritdoc />
    public AgentPresetBase? GetPreset(string agentName)
    {
        return _presets.TryGetValue(agentName, out AgentPresetBase? preset) ? preset : null;
    }








    /// <inheritdoc />
    public IReadOnlyList<string> ListPresets()
    {
        return _presets.Keys.ToList().AsReadOnly();
    }
}





/// <summary>
///     Extension methods for working with agent presets.
/// </summary>
public static class AgentPresetExtensions
{
    /// <summary>
    ///     Gets the default persona for this preset, if configured.
    /// </summary>
    /// <param name="preset">The agent preset.</param>
    /// <returns>The persona if available, otherwise <c>null</c>.</returns>
    public static AgentPersona? GetDefaultPersona(this AgentPresetBase preset)
    {
        if (preset.DefaultPersona is null)
        {
            return null;
        }

        return PersonaRegistry.Get(preset.DefaultPersona.Value);
    }








    /// <summary>
    ///     Gets the instruction string for this preset.
    /// </summary>
    /// <param name="preset">The agent preset.</param>
    /// <param name="taskInstructions">
    ///     Optional task-specific instructions to append or replace defaults.
    /// </param>
    /// <returns>The complete instructions string.</returns>
    public static string GetInstructions(this AgentPresetBase preset, string? taskInstructions = null)
    {
        if (string.IsNullOrWhiteSpace(taskInstructions))
        {
            return preset.DefaultInstructions;
        }

        return $"{preset.DefaultInstructions}\n\n## Task-Specific Instructions\n{taskInstructions}";
    }
}