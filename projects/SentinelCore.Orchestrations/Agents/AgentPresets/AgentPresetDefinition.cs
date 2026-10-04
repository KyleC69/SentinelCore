// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         AgentPresetDefinition.cs
// Author: Kyle L. Crowder
// Build Num:  100310



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
    ///     Gets the unique, stable identifier for the agent. Suitable for lookup
    ///     and event routing. Must be globally unique. Each role preset has these set but can be overridden
    /// </summary>
    public required string AgentId { get; init; }

    /// <summary>
    ///     Gets the unique agent name identifier. This must match the name used
    ///     in <see cref="SentinelAgentCatalog" /> and workflow executor registrations.
    /// </summary>
    public required string AgentName { get; init; }

    /// <summary>
    ///     Gets the default persona type, or <c>null</c> if no persona should be applied.
    ///     Executors may override this at invocation time.
    /// </summary>
    public PersonaType? DefaultPersona { get; init; }

    /// <summary>
    ///    
    ///   The innermost core system instructions are always applied, these must not be overridden ever.
    ///     They are to be attached at the base chat client level and are not to be overridden by any other system instructions. They carry the most weight in the reasoning.
    ///    These are intentionally broad but targeted to ground the agent in how the platform is currently configured and what it is expected to do.
    ///     This improves the agent's ability to reason about the platform and its capabilities, and to make decisions that are consistent with the platform's intended use and ultimately speeds up performance.
    ///     Without these the agent is more likely to hallucinate, may stall, and may make decisions that are inconsistent with the platform's intended use.
    /// </summary>
    public string? DefaultSystemInstructions { get; } = AgentInstructionConstants.CURRENT_PLATFORM_DOMAIN_S;

    /// <summary>
    ///     Gets the model tier for this agent, used to select the default model
    ///     from <see cref="SentinelCoreSettings" />. Tiers allows you to set different default models for different agent roles, and to change the default model for a role without changing the agent's preset.
    /// </summary>
    public ModelTier Tier { get; init; } = ModelTier.Utility;

    /// <summary>
    ///     Gets the tool names to include by default, or <c>null</c> for all available tools.
    /// </summary>
    public IReadOnlyList<string>? ToolSet { get; init; }

    /// <summary>
    ///     Gets whether logging middleware should be applied to this agent's chat client.
    /// </summary>
    public bool UseLogging { get; init; } = true;

    /// <summary>
    ///     Gets whether pattern memory injection should be enabled for this agent.
    ///     Per PL-3, pattern memory is only applied to the Core agent.
    /// </summary>
    public bool UsePatternMemory { get; init; }

    /// <summary>
    ///     Gets whether RAG search tools should be included for this agent.
    /// </summary>
    public bool UseRagSearch { get; init; }

    public bool UseContextEnrichers { get; init; }








    /// <summary>
    ///     Factory method for Core reasoning agents (TheCore, CoreChat).
    ///     Core agents use the frontier model, pattern memory, and logging.
    /// </summary>
    public static AgentPresetDefinition CoreRole(string? name = null, string? id = null)
    {
        return new AgentPresetDefinition
        {
            AgentName = name ?? "TheCore",
            AgentId = id ?? "TheCore",
            Tier = ModelTier.Core,
            UsePatternMemory = false,
            UseLogging = true,
            UseRagSearch = false,
            UseContextEnrichers = false
        };
    }








    /// <summary>
    ///     Factory method for Manager/orchestrator agents.
    ///     Manager agents use the manager model and logging only.
    ///     Per PL-3, the Manager must not have tools.
    /// </summary>
    public static AgentPresetDefinition ManagerRole(string? name = null, string? id = null)
    {
        return new AgentPresetDefinition
        {
            AgentName = name ?? "GroupManagerAgent",
            AgentId = id ?? "GroupManagerAgent",
            DefaultPersona = null,
            Tier = ModelTier.Manager,
            ToolSet = null,
            UseLogging = true,
            UsePatternMemory = false,
            UseRagSearch = false,
            UseContextEnrichers = false
        };
    }








    /// <summary>
    ///     Factory method for Utility/worker agents.
    ///     Utility agents use the utility model and logging.
    /// </summary>
    public static AgentPresetDefinition UtilityRole(string? name = null, string? id = null)
    {
        return new AgentPresetDefinition
        {
            AgentId = id ?? "UtilityAgent",
            AgentName = name ?? "UtilityAgent",
            DefaultPersona = null,
            Tier = ModelTier.Utility,
            ToolSet = null,
            UseLogging = true,
            UsePatternMemory = false,
            UseRagSearch = false,
            UseContextEnrichers = false
        };
    }








    public static AgentPresetDefinition ClassifierRole(string? id = null, string? name = null)
    {
        return new AgentPresetDefinition
        {
            AgentId = id ?? "ClassifierAgent",
            AgentName = name ?? "ClassifierAgent",
            DefaultPersona = null,
            Tier = ModelTier.Utility,
            ToolSet = null,
            UseLogging = true,
            UsePatternMemory = false,
            UseRagSearch = false,
            UseContextEnrichers = false
        };
    }








    public static AgentPresetDefinition SafetyAgent(string? id = null, string? name = null)
    {
        return new AgentPresetDefinition
        {
            AgentId = id ?? "SafetyAgent",
            AgentName = name ?? "SafetyAgent",
            DefaultPersona = null,
            Tier = ModelTier.Utility,
            ToolSet = null,
            UseLogging = true,
            UsePatternMemory = false,
            UseRagSearch = false,
            UseContextEnrichers = false
        };
    }
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