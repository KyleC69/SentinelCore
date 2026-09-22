// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         AgentProfileBuilder.cs
// Author: Kyle L. Crowder
// Build Num:  091418



using Microsoft.Extensions.Options;

using SentinelCore.Abstractions;
using SentinelCore.Contracts.Contracts;
using SentinelCore.Orchestrations.Agents.AgentPresets;
using SentinelCore.Orchestrations.Personas;




namespace SentinelCore.Orchestrations.Agents;





/// <summary>
///     Builds <see cref="AgentProfile" /> instances from agent presets or configuration.
///     The preset is the single source of truth for the agent's default name, persona,
///     model settings, and tool set. Callers may optionally override the default persona
///     or append task-specific instructions.
/// </summary>
public interface IAgentProfileBuilder
{
    /// <summary>
    ///     Builds an <see cref="AgentProfile" /> using the specified agent name.
    ///     If a preset exists for the agent name, it will be used to populate defaults.
    /// </summary>
    /// <param name="agentName">The name of the agent to be created.</param>








    /// <summary>
    ///     Builds an <see cref="AgentProfile" /> from an agent preset definition.
    ///     This is the preferred method for creating agent profiles.
    /// </summary>
    /// <param name="preset">The agent preset definition containing default configuration.</param>
    /// <returns>An <see cref="AgentProfile" /> configured from the preset.</returns>
    AgentProfile BuildFromPreset(AgentPresetDefinition preset);

    /// <summary>
    ///     Builds an <see cref="AgentProfile" /> from an agent preset definition,
    ///     optionally overriding the default persona.
    /// </summary>
    /// <param name="preset">The agent preset definition containing default configuration.</param>
    /// <param name="personaOverride">Optional persona override; if null, the preset's default persona is used.</param>
    /// <returns>An <see cref="AgentProfile" /> configured from the preset.</returns>
    AgentProfile BuildFromPreset(AgentPresetDefinition preset, AgentPersona? personaOverride);








    /// <summary>
    ///     Attempts to resolve the model profile configured for a logical agent name.
    /// </summary>
    /// <param name="agentName">The logical agent name.</param>
    /// <returns>The configured model profile, or <c>null</c> when the agent is unconfigured.</returns>
    ModelProfile? TryGetModel(string agentName);
}





/// <summary>
///     Builds <see cref="AgentProfile" /> instances from agent presets or configuration.
///     The preset is the single source of truth for the agent's default name, persona,
///     model settings, and tool set. Callers may optionally override the default persona.
/// </summary>
public sealed class AgentProfileBuilder : IAgentProfileBuilder
{
    private readonly SentinelCoreSettings _options;
    private readonly IAgentPresetProvider _presetProvider;








    /// <summary>
    ///     Initializes a new instance of the <see cref="AgentProfileBuilder" /> class.
    /// </summary>
    /// <param name="options">
    ///     The <see cref="IOptions{TOptions}" /> instance containing the <see cref="SentinelCoreSettings" />
    ///     used to configure the agent profile builder.
    /// </param>
    public AgentProfileBuilder(IOptions<SentinelCoreSettings> options) : this(options, new AgentPresetProvider())
    {
    }








    /// <summary>
    ///     Initializes a new instance of the <see cref="AgentProfileBuilder" /> class.
    /// </summary>
    /// <param name="options">
    ///     The <see cref="IOptions{TOptions}" /> instance containing the <see cref="SentinelCoreSettings" />
    ///     used to configure the agent profile builder.
    /// </param>
    /// <param name="presetProvider">
    ///     The preset provider for resolving agent presets. Defaults to <see cref="AgentPresetProvider" />.
    /// </param>
    public AgentProfileBuilder(IOptions<SentinelCoreSettings> options, IAgentPresetProvider presetProvider)
    {
        Throw.IfNull(options);
        Throw.IfNull(presetProvider);

        _options = options.Value;
        _presetProvider = presetProvider;
    }








    /// <summary>
    /// Builds an agent profile from a preset when one exists; otherwise creates a default profile and applies any
    /// overrides.
    /// </summary>
    /// <param name="agentName">The agent name used to resolve a preset or create the default profile.</param>
    /// <param name="personaOverride">Optional persona to apply to the generated profile.</param>
    /// <returns>The generated agent profile.</returns>
    public AgentProfile BuildAgentSpec(string agentName, AgentPersona? personaOverride = null)
    {
        // Try to get a preset for this agent name
        AgentPresetDefinition? preset = _presetProvider.GetPreset(agentName);
        if (preset != null)
        {
            return BuildFromPreset(preset, personaOverride);
        }

        // No preset found - build with minimal defaults
        AgentProfile profile = BuildDefaultAgentSpec(agentName);

        if (personaOverride != null)
        {
            profile.Persona = personaOverride;
        }

        return profile;
    }









    /// <summary>
    ///     Builds an <see cref="AgentProfile" /> from an <see cref="AgentPresetDefinition" />,
    ///     optionally overriding the default persona.
    /// </summary>
    /// <param name="preset">The preset definition containing default configuration.</param>
    /// <param name="personaOverride">Optional persona override; if null, the preset's default persona is used.</param>
    /// <returns>An <see cref="AgentProfile" /> configured from the preset.</returns>
    public AgentProfile BuildFromPreset(AgentPresetDefinition preset, AgentPersona? personaOverride = null)
    {
        Throw.IfNull(preset);

        AgentProfile profile = BuildFromPreset(preset);

        // Apply persona: explicit override wins, otherwise use preset default
        if (personaOverride != null)
        {
            profile.Persona = personaOverride;
        }
        else if (preset.DefaultPersona.HasValue)
        {
            profile.Persona = PersonaRegistry.Get(preset.DefaultPersona.Value);
        }

        return profile;
    }









    public ModelProfile? TryGetModel(string agentName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentName);

        // Try per-agent configuration first
        if (_options.AgentModels.TryGetValue(agentName, out ModelProfile? perAgent))
        {
            return perAgent;
        }

        // Fall back to default model
        return _options.DefaultModel;
    }








    /// <summary>
    ///     Builds an <see cref="AgentProfile" /> using settings from the SentinelCore configuration
    ///     or system defaults. This method is for creating agents with users or system defaults.
    /// </summary>
    /// <returns>An <see cref="AgentProfile" /> instance containing the configuration for the agent.</returns>
    public AgentProfile BuildAgentSpec()
    {
        return BuildDefaultAgentSpec("AIAgent");
    }








    private AgentProfile BuildDefaultAgentSpec(string agentName)
    {
        AgentProfile profile = new() { AgentName = agentName, AgentId = agentName };

        // No hardcoded fallback — the factory gate rejects unconfigured agents.
        profile.Model = _options.AgentModels.TryGetValue(agentName, out ModelProfile? perAgent) ? perAgent : null;

        return profile;
    }








    public AgentProfile BuildFromPreset(AgentPresetDefinition preset)
    {
        Throw.IfNull(preset);

        AgentProfile profile = new() { AgentName = preset.AgentName, AgentId = preset.AgentId };

        // Model configuration comes from settings
        profile.Model = TryGetModel(preset.AgentName);

        return profile;
    }
}
