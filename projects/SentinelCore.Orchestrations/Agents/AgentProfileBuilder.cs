// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         AgentProfileBuilder.cs
// Author: Kyle L. Crowder
// Build Num:  081602



using Microsoft.Extensions.Options;

using SentinelCore.Abstractions;




namespace SentinelCore.Agents;





public interface IAgentProfileBuilder
{
    /// <summary>
    ///     Builds an <see cref="AgentProfile" /> for the specified role using the default settings.
    /// </summary>
    /// <param name="agentName"></param>
    /// <param name="role">The role of the agent for which the specification is being built.</param>
    /// <returns>An <see cref="AgentProfile" /> instance containing the configuration for the specified agent role.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when the specified <paramref name="role" /> is not a valid <see cref="AgentRole" />.
    /// </exception>
    AgentProfile BuildAgentSpec(string agentName, AgentRole role);








    /// <summary>
    ///     Builds an <see cref="AgentProfile" /> using the specified agent name.
    /// </summary>
    /// <param name="agentName">The name of the agent to be created.</param>
    /// <returns>An <see cref="AgentProfile" /> instance containing the configuration for the agent.</returns>
    AgentProfile BuildAgentSpec(string agentName);








    /// <summary>
    ///     Builds an <see cref="AgentProfile" /> for the specified agent name and role,
    ///     optionally including task-specific instructions.
    /// </summary>
    /// <param name="agentName">The name of the agent.</param>
    /// <param name="role">The role assigned to the agent.</param>
    /// <param name="taskInstructions">
    ///     Optional task-specific instructions to customize the agent's behavior.
    /// </param>
    /// <returns>An <see cref="AgentProfile" /> configured with the specified parameters.</returns>
    AgentProfile BuildAgentSpec(string agentName, AgentRole role, string? taskInstructions = null);
}





/// <summary>
///     Builds <see cref="AgentProfile" /> instances where <see cref="AgentRole" /> is the
///     single source of truth for the agent's default name, persona, model settings,
///     and tool set. Callers may optionally override the default persona.
/// </summary>
public sealed class AgentProfileBuilder : IAgentProfileBuilder
{
    private readonly SentinelCoreSettings _options;








    /// <summary>
    ///     Initializes a new instance of the <see cref="AgentProfileBuilder" /> class.
    /// </summary>
    /// <param name="options">
    ///     The <see cref="IOptions{TOptions}" /> instance containing the <see cref="SentinelCoreSettings" />
    ///     used to configure the agent profile builder.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when the <paramref name="options" /> parameter is <c>null</c>.
    /// </exception>
    public AgentProfileBuilder(IOptions<SentinelCoreSettings> options)
    {
        Throw.IfNull(options);
        _options = options.Value;
    }








    /// <summary>
    ///     Builds an <see cref="AgentProfile" /> for the specified agent name and role,
    ///     optionally including task-specific instructions.
    /// </summary>
    /// <param name="agentName">The name of the agent.</param>
    /// <param name="role">The role assigned to the agent.</param>
    /// <param name="taskInstructions">Optional task-specific instructions to customize the agent's behavior.</param>
    /// <returns>An <see cref="AgentProfile" /> configured for the specified agent name and role.</returns>
    public AgentProfile BuildAgentSpec(string agentName, AgentRole role, string? taskInstructions = null)
    {
        // Reuse the role‑based overload to construct a base profile.
        AgentProfile profile = BuildAgentSpec(agentName, role);
        if (!string.IsNullOrWhiteSpace(taskInstructions))
        {
            profile.Instructions = taskInstructions;
        }

        return profile;
    }








    /// <summary>
    ///     Builds an <see cref="AgentProfile" /> for the specified role using the default settings.
    /// </summary>
    /// <param name="agentName"></param>
    /// <param name="role">The role of the agent for which the specification is being built.</param>
    /// <returns>An <see cref="AgentProfile" /> instance containing the configuration for the specified agent role.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when the specified <paramref name="role" /> is not a valid <see cref="AgentRole" />.
    /// </exception>
    public AgentProfile BuildAgentSpec(string agentName, AgentRole role)
    {

        AgentProfile profile = BuildDefaultAgentSpec(agentName);
        profile.Role = role;

        switch (role)
        {
            case AgentRole.Core:
                profile.Model = _options.DefaultModel ?? ModelProfile.Glm5();
                break;
            case AgentRole.Manager:
                profile.Model = _options.DefaultModel ?? ModelProfile.Gpt120();
                break;
            case AgentRole.Utility:
                profile.Model = _options.DefaultUtilityModel ?? ModelProfile.Gpt20();
                break;
        }

        return profile;
    }








    /// <summary>
    ///     Builds an <see cref="AgentProfile" /> using settings from the SentinelCore configuration or system defaults. This
    ///     method is for creating agents with users or system defaults.
    /// </summary>
    /// <param name="agentName">The name of the agent.</param>
    /// <returns></returns>
    public AgentProfile BuildAgentSpec(string agentName)
    {
        AgentProfile profile = BuildDefaultAgentSpec(agentName);
        return profile;
    }








    /// <summary>
    ///     Builds an <see cref="AgentProfile" /> using settings from the SentinelCore configuration or system defaults. This
    ///     method is for creating agents with users or system defaults.
    /// </summary>
    /// <returns>An <see cref="AgentProfile" /> instance containing the configuration for the agent.</returns>
    public AgentProfile BuildAgentSpec()
    {
        return BuildDefaultAgentSpec("AIAgent");
    }








    /// <summary>
    ///     Builds an <see cref="AgentProfile" /> for the specified agent role and name.
    ///     Optionally, task-specific instructions can be provided to customize the agent's behavior.
    /// </summary>
    /// <param name="role">The role assigned to the agent, defining its default behavior and configuration.</param>
    /// <param name="agentName">The name of the agent.</param>
    /// <param name="taskInstructions">
    ///     Optional instructions specific to the task the agent will perform. These instructions
    ///     supplement the default behavior defined by the agent's role.
    /// </param>
    /// <returns>An <see cref="AgentProfile" /> configured with the specified role, name, and optional task instructions.</returns>
    public AgentProfile BuildAgentSpec(AgentRole role, string agentName, string? taskInstructions = null)
    {
        // First we build the profile from user options.
        AgentProfile profile = BuildDefaultAgentSpec(agentName);
        // Modify the profile based on persona and task instructions
        if (taskInstructions != null)
        {
            profile.Instructions = taskInstructions;

        }

        profile.Role = role;


        return profile;
    }








    private AgentProfile BuildDefaultAgentSpec(string agentName)
    {
        AgentProfile profile = new();
        profile.AgentName = agentName;
        profile.AgentId = agentName;
        profile.Instructions = "";
        // If the configuration does not provide a default model, fallback to Glm5.
        profile.Model = _options.DefaultModel ?? ModelProfile.Glm5();





        return profile;
    }
}