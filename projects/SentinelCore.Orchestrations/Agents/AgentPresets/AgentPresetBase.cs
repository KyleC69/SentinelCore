// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         AgentPresetBase.cs
// Author: Kyle L. Crowder
// Build Num:  091200



using SentinelCore.Orchestrations.Personas;




namespace SentinelCore.Orchestrations.Agents.AgentPresets;





/// <summary>
///     Base record for agent preset configurations. Each named agent has a preset
///     that defines default instructions, persona, middleware, and tool configuration.
/// </summary>
/// <remarks>
///     Presets are immutable and serve as the default source of truth. Callers may
///     override any property when building an <see cref="AgentProfile" />.
/// </remarks>
public abstract record AgentPresetBase
{
    /// <summary>
    ///     Gets the unique agent name identifier.
    /// </summary>
    public abstract string AgentName { get; }

    /// <summary>
    ///     Gets the default instructions for the agent.
    /// </summary>
    public abstract string DefaultInstructions { get; }

    /// <summary>
    ///     Gets the default persona type, or <c>null</c> if no persona should be applied.
    /// </summary>
    public virtual PersonaType? DefaultPersona
    {
        get => null;
    }

    /// <summary>
    ///     Gets the middleware flags to apply to this agent.
    /// </summary>
    public virtual MiddlewareFlags MiddlewareFlags
    {
        get => MiddlewareFlags.None;
    }

    /// <summary>
    ///     Gets the model tier for this agent, used to select default model from settings.
    /// </summary>
    public virtual ModelTier Tier
    {
        get => ModelTier.Utility;
    }

    /// <summary>
    ///     Gets the tool names to include by default, or <c>null</c> for all available tools.
    /// </summary>
    public virtual IReadOnlyList<string>? ToolSet
    {
        get => null;
    }
}