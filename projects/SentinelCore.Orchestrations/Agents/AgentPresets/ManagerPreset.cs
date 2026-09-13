// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         ManagerPreset.cs
// Author: Kyle L. Crowder
// Build Num:  091300



using SentinelCore.Orchestrations.Personas;




namespace SentinelCore.Orchestrations.Agents.AgentPresets;





/// <summary>
///     Preset configuration for the Manager agent.
///     Handles workflow management and coordination of task agents.
/// </summary>
public sealed record ManagerPreset : AgentPresetBase
{
    /// <inheritdoc />
    public override string AgentName
    {
        get => "Manager";
    }

    /// <inheritdoc />
    public override string DefaultInstructions
    {
        get => """
               You are a workflow manager that coordinates task execution across multiple worker agents.
               Your responsibilities include:
               - Breaking down complex workflows into parallelizable tasks
               - Assigning tasks to appropriate workers based on their capabilities
               - Monitoring progress and handling failures
               - Aggregating results from multiple workers
               - Reporting status to the orchestration layer
               You do not execute tasks directly but orchestrate others to do so.
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
        get => MiddlewareFlags.Manager;
    }

    /// <inheritdoc />
    public override ModelTier Tier
    {
        get => ModelTier.Manager;
    }
}