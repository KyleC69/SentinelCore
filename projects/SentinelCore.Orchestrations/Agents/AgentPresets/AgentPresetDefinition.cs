// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         AgentPresetDefinition.cs
// Author: Kyle L. Crowder
// Build Num:  100908



using SentinelCore.Contracts.Contracts;
using SentinelCore.Orchestrations.Agents.Models;




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
    public required string AgentId { get; set; }

    /// <summary>
    ///     Gets the unique agent name identifier. This must match the name used
    ///   
    /// </summary>
    public required string AgentName { get; set; }

    /// <summary>
    ///     The innermost core system instructions are always applied, these must not be overridden ever.
    ///     They are to be attached at the base chat client level and are not to be overridden by any other system
    ///     instructions. They carry the most weight in the reasoning.
    ///     These are intentionally broad but targeted to ground the agent in how the platform is currently configured and what
    ///     it is expected to do.
    ///     This improves the agent's ability to reason about the platform and its capabilities, and to make decisions that are
    ///     consistent with the platform's intended use and ultimately speeds up performance.
    ///     Without these the agent is more likely to hallucinate, may stall, and may make decisions that are inconsistent with
    ///     the platform's intended use.
    /// </summary>
    public string? DefaultSystemInstructions { get; set; }



    public string Description { get; set; }

    /// <summary>
    ///     Gets the model profile for this agent, which defines the specific model configuration to be used.
    /// </summary>
    public ModelProfile ModelProfile { get; set; }

    public string? TaskInstructions { get; set; }

    /// <summary>
    ///     Gets the model tier for this agent, used to select the default model
    ///     from <see cref="SentinelCoreSettings" />. Tiers allows you to set different default models for different agent
    ///     roles, and to change the default model for a role without changing the agent's preset.
    /// </summary>
    public ModelTier Tier { get; set; } = ModelTier.Utility;

    /// <summary>
    ///     Gets the tool names to include by default, or <c>null</c> for all available tools.
    /// </summary>
    public IReadOnlyList<AITool>? ToolSet { get; set; }

    public bool UseContextEnrichers { get; init; }

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

    public bool UseTools { get; set; }








    /// <summary>
    ///     Defines the aggregator role for an agent, responsible for collecting and collating information while performing
    ///     auxiliary tasks and supporting core workflow operations.
    /// </summary>
    /// <returns></returns>
    private static AgentPresetDefinition AggregatorRole()
    {
        return new AgentPresetDefinition
        {
            AgentId = "AggregatorAgent",
            AgentName = "AggregatorAgent",
            ModelProfile = null,
            Description = "Utility agent responsible for collecting and collating information while performing auxiliary tasks and supporting core workflow operations.",
            UseTools = false,
            TaskInstructions = null,
            Tier = ModelTier.Utility,
            ToolSet = null,
            UseLogging = true,
            UsePatternMemory = false,
            UseRagSearch = false,
            UseContextEnrichers = false,
            DefaultSystemInstructions = AgentInstructionConstants.CURRENT_PLATFORM_DOMAIN_S
        };
    }








    public static AgentPresetDefinition ClassifierRole(string? id = null, string? name = null)
    {
        return new AgentPresetDefinition
        {
            AgentId = id ?? "ClassifierAgent",
            AgentName = name ?? "ClassifierAgent",
            ModelProfile = null,
            Description = "Agent responsible for classifying inputs. Primary usage is in complex workflow switching.",
            Tier = ModelTier.Utility,
            ToolSet = null,
            UseLogging = true,
            UsePatternMemory = false,
            UseRagSearch = false,
            UseContextEnrichers = false,
            UseTools = false,
            TaskInstructions = null,
            DefaultSystemInstructions = AgentInstructionConstants.CURRENT_PLATFORM_DOMAIN_S + "\n" + AgentInstructionConstants.CLASSIFIER_INSTRUCTIONS
        };
    }








    /// <summary>
    ///     Factory method for Core reasoning agents (TheCore, CoreChat).
    ///     Core agents use the frontier model, pattern memory, and logging.
    /// </summary>
    public static AgentPresetDefinition CoreRole(string? name = null, string? id = null)
    {
        return new AgentPresetDefinition
        {
            AgentName = name ?? "TheCore",
            ModelProfile = null,
            AgentId = id ?? "TheCore",
            Description = "Core reasoning agent responsible for advanced analytical tasks and operator support.",
            Tier = ModelTier.Core,
            ToolSet = null,
            UsePatternMemory = true,
            UseLogging = true,
            UseRagSearch = true,
            UseContextEnrichers = true,
            UseTools = true,
            TaskInstructions = null,
            DefaultSystemInstructions = AgentInstructionConstants.CURRENT_PLATFORM_DOMAIN_S + "\n" + AgentInstructionConstants.SENTINEL_CORE_INSTRUCTIONS
        };
    }








    internal static AgentPresetDefinition FromName(AgentPreset presetName)
    {
        return presetName switch
        {
            AgentPreset.CoreAgent => CoreRole(),
            AgentPreset.ManagerAgent => ManagerRole(),
            AgentPreset.SafetyAgent => SafetyAgent(),
            AgentPreset.UtilityAgent => UtilityRole(),
            AgentPreset.ClassifierAgent => ClassifierRole(),
            AgentPreset.WorkerAgent => WorkerRole(),
            AgentPreset.AggregatorAgent => AggregatorRole(),
            _ => throw new ArgumentOutOfRangeException(nameof(presetName), presetName, null)
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
            ModelProfile = null,
            AgentId = id ?? "GroupManagerAgent",
            Description = "Manager/orchestrator agent responsible for coordinating Magentic Group Orchestration workflows.",
            Tier = ModelTier.Manager,
            ToolSet = null,
            UseLogging = true,
            UsePatternMemory = false,
            UseRagSearch = true,
            UseContextEnrichers = false,
            UseTools = false,
            TaskInstructions = null,
            DefaultSystemInstructions = AgentInstructionConstants.CURRENT_PLATFORM_DOMAIN_S + "\n" + AgentInstructionConstants.MAG_MANAGER_INSTRUCTIONS
        };
    }








    public static AgentPresetDefinition SafetyAgent(string? id = null, string? name = null)
    {
        return new AgentPresetDefinition
        {
            AgentId = id ?? "SafetyAgent",
            AgentName = name ?? "SafetyAgent",
            ModelProfile = null,
            Description = "Utility agent responsible for ensuring safety and compliance within workflows.",
            Tier = ModelTier.Utility,
            ToolSet = null,
            UseLogging = true,
            UsePatternMemory = false,
            UseRagSearch = false,
            UseContextEnrichers = false,
            UseTools = false,
            TaskInstructions = null,
            DefaultSystemInstructions = AgentInstructionConstants.CURRENT_PLATFORM_DOMAIN_S + "\n" + AgentInstructionConstants.SAFETY_AGENT_INSTRUCTIONS
        };
    }








    /// <summary>
    ///     Factory method for General-purpose utility agents.
    ///     Utility agents use the utility model and logging.
    /// </summary>
    public static AgentPresetDefinition UtilityRole(string? name = null, string? id = null)
    {
        return new AgentPresetDefinition
        {
            AgentId = id ?? "UtilityAgent",
            AgentName = name ?? "UtilityAgent",
            ModelProfile = null,
            Description = "Utility agent responsible for performing auxiliary tasks and supporting core workflow operations.",
            Tier = ModelTier.Utility,
            ToolSet = null,
            UseLogging = true,
            UsePatternMemory = false,
            UseTools = false,
            TaskInstructions = null,
            UseRagSearch = false,
            UseContextEnrichers = false,
            DefaultSystemInstructions = AgentInstructionConstants.CURRENT_PLATFORM_DOMAIN_S
        };
    }








    private static AgentPresetDefinition WorkerRole(string? name = null, string? id = null)
    {
        return new AgentPresetDefinition
        {
            AgentId = id ?? "GenericWorkerAgent",
            AgentName = name ?? "GenericWorkerAgent",
            ModelProfile = null,
            Description = "All purpose expert Windows Forensic Investigator. " +
                          "This agent is responsible for performing investigative tasks and supporting core workflow operations.",
            UseTools = false,
            TaskInstructions = null,
            Tier = ModelTier.Worker,
            ToolSet = null,
            UseLogging = true,
            UsePatternMemory = false,
            UseRagSearch = false,
            UseContextEnrichers = false,
            DefaultSystemInstructions = AgentInstructionConstants.CURRENT_PLATFORM_DOMAIN_S
        };
    }
}