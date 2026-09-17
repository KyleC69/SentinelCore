// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         AgentPreset.cs
// Author: Kyle L. Crowder
// Build Num:  091418

namespace SentinelCore.Orchestrations.Agents.AgentPresets;





/// <summary>
///     Base class for agent preset configurations.
///     Defines default instructions, middleware, and tool sets for named agents.
/// </summary>
public abstract class AgentPreset
{
    /// <summary>
    ///     Gets the unique name of this agent.
    /// </summary>
    public abstract string AgentName { get; }
    /// <summary>
    /// Unique, stable identifier for the agent.
    /// </summary>
    /// <remarks>Must be a non-empty, stable value for the agent's lifetime and suitable for lookup and event
    /// routing. Should be globally unique (for example, a GUID or a qualified name).</remarks>
    public abstract string AgentId { get; }







    /// <summary>
    ///     Gets the role tier for model resolution (Core, Manager, Utility).
    ///     Used to select the default model from SentinelCoreSettings.
    /// </summary>
    public virtual ModelTier ModelTier { get; } = ModelTier.Utility;

    /// <summary>
    ///     Gets the response format type for structured output, or null for default.
    /// </summary>
    public virtual Type? ResponseFormat { get; } = null;

    /// <summary>
    ///     Gets whether event publishing should be enabled.
    /// </summary>
    public virtual bool UseEventPublishing { get; } = true;

    /// <summary>
    ///     Gets whether logging should be enabled.
    /// </summary>
    public virtual bool UseLogging { get; } = true;

    /// <summary>
    ///     Gets whether pattern memory should be enabled.
    /// </summary>
    public virtual bool UsePatternMemory { get; } = false;

    /// <summary>
    ///     Gets whether RAG search should be enabled.
    /// </summary>
    public virtual bool UseRagSearch { get; } = false;

    /// <summary>
    ///     Gets whether safety middleware should be applied.
    /// </summary>
    public virtual bool UseSafety { get; } = true;
}





/// <summary>
///     Defines the model tier for an agent, used to select default model from settings.
/// </summary>
public enum ModelTier
{
    /// <summary>
    ///     Core reasoning agent - uses frontier/default model.
    /// </summary>
    Core,

    /// <summary>
    ///     Manager/orchestrator agent - uses manager model.
    /// </summary>
    Manager,

    /// <summary>
    ///     Utility/worker agent - uses utility model.
    /// </summary>
    Utility
}