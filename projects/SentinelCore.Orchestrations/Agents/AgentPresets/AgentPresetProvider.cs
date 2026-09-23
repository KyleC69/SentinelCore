// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         AgentPresetProvider.cs
// Author: Kyle L. Crowder
// Build Num:  092308



using SentinelCore.Orchestrations.Agents.AgentPresets;
using SentinelCore.Orchestrations.Personas;




namespace SentinelCore.Orchestrations.Agents;





/// <summary>
///     Default implementation of <see cref="IAgentPresetProvider" /> that provides
///     all built-in agent presets (matching SentinelAgentCatalog names).
///     Each preset is an <see cref="AgentPresetDefinition" /> — an immutable record
///     that declares what infrastructure the agent needs at construction time.
/// </summary>
public sealed class AgentPresetProvider : IAgentPresetProvider
{
    private readonly Dictionary<string, AgentPresetDefinition> _presets;








    /// <summary>
    ///     Initializes a new instance of the <see cref="AgentPresetProvider" /> class.
    /// </summary>
    public AgentPresetProvider()
    {
        AgentPresetDefinition[] presets =
        [
                AgentPresetDefinition.CoreRole("CoreChat", "corechat") with { UsePatternMemory = true, UseRagSearch = true, DefaultSystemInstructions = AgentInstructionConstants.GetAgentPresetInstructions("CoreChat") },
                AgentPresetDefinition.UtilityRole("Classifier", "classifier") with { DefaultPersona = PersonaType.TheAnalyst, DefaultSystemInstructions = AgentInstructionConstants.CLASSIFIER_INSTRUCTIONS },
                AgentPresetDefinition.CoreRole("TheCore", "thecore") with { UsePatternMemory = true, DefaultSystemInstructions = AgentInstructionConstants.GetAgentPresetInstructions("TheCore") },
                AgentPresetDefinition.UtilityRole("SafetyAgent", "safetyagent") with { DefaultSystemInstructions = AgentInstructionConstants.SAFETY_AGENT_INSTRUCTIONS },
                AgentPresetDefinition.ManagerRole("Manager", "manager") with { DefaultSystemInstructions = AgentInstructionConstants.MAG_MANAGER_INSTRUCTIONS },
                AgentPresetDefinition.UtilityRole("Worker1", "worker1") with { DefaultSystemInstructions = AgentInstructionConstants.WORKER_INSTRUCTIONS },
                AgentPresetDefinition.UtilityRole("Worker2", "worker2") with { DefaultSystemInstructions = AgentInstructionConstants.WORKER_INSTRUCTIONS },
                AgentPresetDefinition.UtilityRole("Worker3", "worker3") with { DefaultSystemInstructions = AgentInstructionConstants.WORKER_INSTRUCTIONS },
                AgentPresetDefinition.UtilityRole("DirectAnswer", "directanswer") with { DefaultSystemInstructions = AgentInstructionConstants.GetAgentPresetInstructions("DirectAnswer") }
        ];

        _presets = new Dictionary<string, AgentPresetDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (AgentPresetDefinition preset in presets)
        {
            _presets[preset.AgentName] = preset;
        }
    }








    public IReadOnlyList<AgentPresetDefinition> GetAllPresets()
    {
        return _presets.Values.ToList().AsReadOnly();
    }








    public AgentPresetDefinition? GetPreset(string agentName)
    {
        return _presets.TryGetValue(agentName, out AgentPresetDefinition? preset) ? preset : null;
    }








    public IReadOnlyList<string> ListPresets()
    {
        return _presets.Keys.ToList().AsReadOnly();
    }
}