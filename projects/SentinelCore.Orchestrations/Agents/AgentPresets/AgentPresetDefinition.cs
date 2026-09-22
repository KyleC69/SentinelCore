// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         AgentPresetDefinition.cs
// Author: Kyle L. Crowder
// Build Num:  092200



using SentinelCore.Orchestrations.Personas;




namespace SentinelCore.Orchestrations.Agents.AgentPresets;





/// <summary>
///     Immutable declaration of an agent role's construction-time configuration.
///     This is pure data — it declares WHAT infrastructure the agent needs,
///     not HOW to invoke it. Invocation concerns (instructions, response format)
///     belong to the executor call site, not the preset.
///     <para>
///         Presets are the single source of truth for an agent's default configuration.
///         Executors may override instructions and response format at invocation time
///         via <see cref="ChatMessages" /> and <see cref="AgentRunOptions" />.
///     </para>
/// </summary>
public sealed record AgentPresetDefinition
{
    /// <summary>
    ///     Gets the unique agent name identifier. This must match the name used
    ///     in <see cref="SentinelAgentCatalog" /> and workflow executor registrations.
    /// </summary>
    public required string AgentName { get; init; }

    /// <summary>
    ///     Gets the unique, stable identifier for the agent. Suitable for lookup
    ///     and event routing. Must be globally unique.
    /// </summary>
    public required string AgentId { get; init; }

    /// <summary>
    ///     Gets the model tier for this agent, used to select the default model
    ///     from <see cref="SentinelCoreSettings" />.
    /// </summary>
    public ModelTier Tier { get; init; } = ModelTier.Utility;

    /// <summary>
    ///     Gets the default persona type, or <c>null</c> if no persona should be applied.
    ///     Executors may override this at invocation time.
    /// </summary>
    public PersonaType? DefaultPersona { get; init; }

    /// <summary>
    ///     Gets whether pattern memory injection should be enabled for this agent.
    ///     Per PL-3, pattern memory is only applied to the Core agent.
    /// </summary>
    public bool UsePatternMemory { get; init; }

    /// <summary>
    ///     Gets whether logging middleware should be applied to this agent's chat client.
    /// </summary>
    public bool UseLogging { get; init; } = true;

    /// <summary>
    ///     Gets whether RAG search tools should be included for this agent.
    /// </summary>
    public bool UseRagSearch { get; init; }

    /// <summary>
    ///     Gets the default system instructions for this agent role.
    ///     These are used when no per-call override is provided by the executor.
    ///     Executors layer instructions at call time: platform domain → preset → task.
    /// </summary>
    public string? DefaultSystemInstructions { get; init; }

    /// <summary>
    ///     Gets the tool names to include by default, or <c>null</c> for all available tools.
    /// </summary>
    public IReadOnlyList<string>? ToolSet { get; init; }








    /// <summary>
    ///     Factory method for Core reasoning agents (TheCore, CoreChat).
    ///     Core agents use the frontier model, pattern memory, and logging.
    /// </summary>
    public static AgentPresetDefinition CoreRole(string name, string id) => new()
    {
            AgentName = name,
            AgentId = id,
            Tier = ModelTier.Core,
            UsePatternMemory = true,
            UseLogging = true
    };








    /// <summary>
    ///     Factory method for Manager/orchestrator agents.
    ///     Manager agents use the manager model and logging only.
    ///     Per PL-3, the Manager must not have tools.
    /// </summary>
    public static AgentPresetDefinition ManagerRole(string name, string id) => new() { AgentName = name, AgentId = id, Tier = ModelTier.Manager, UseLogging = true };








    /// <summary>
    ///     Factory method for Utility/worker agents.
    ///     Utility agents use the utility model and logging.
    /// </summary>
    public static AgentPresetDefinition UtilityRole(string name, string id) => new() { AgentName = name, AgentId = id, Tier = ModelTier.Utility, UseLogging = true };
}





/// <summary>
///     Defines the model tier for an agent, used to select the default model
///     from <see cref="SentinelCoreSettings" />.
/// </summary>
public enum ModelTier
{
    /// <summary>
    ///     Core reasoning agent — uses the frontier/default model.
    /// </summary>
    Core,

    /// <summary>
    ///     Manager/orchestrator agent — uses the manager model.
    /// </summary>
    Manager,

    /// <summary>
    ///     Utility/worker agent — uses the utility model.
    /// </summary>
    Utility
}
