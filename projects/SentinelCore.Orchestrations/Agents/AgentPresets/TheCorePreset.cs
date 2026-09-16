// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         TheCorePreset.cs
// Author: Kyle L. Crowder
// Build Num:  091418



using SentinelCore.Orchestrations.Personas;




namespace SentinelCore.Orchestrations.Agents.AgentPresets;





/// <summary>
///     Preset configuration for the TheCore orchestration agent.
///     This is the main orchestration engine that coordinates all other agents.
/// </summary>
public sealed record TheCorePreset : AgentPresetBase
{
    /// <inheritdoc />
    public override string AgentName
    {
        get => "TheCore";
    }

    /// <inheritdoc />
    public override string DefaultInstructions
    {
        get => """
               You are TheCore, the central orchestration engine for SentinelCore.
               You coordinate multiple specialized agents to accomplish complex workflows.
               Your responsibilities include:
               - Decomposing user requests into subtasks
               - Delegating tasks to appropriate specialized agents
               - Aggregating results and synthesizing responses
               - Managing conversation context and state
               - Ensuring safety and compliance throughout the workflow
               Always maintain oversight of the overall workflow and adapt as needed.
               """;
    }

    /// <inheritdoc />
    public override PersonaType? DefaultPersona
    {
        get => PersonaType.TheManager;
    }

    /// <inheritdoc />
    public override MiddlewareFlags MiddlewareFlags
    {
        get => MiddlewareFlags.Core;
    }

    /// <inheritdoc />
    public override ModelTier Tier
    {
        get => ModelTier.Core;
    }
}