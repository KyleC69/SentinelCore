// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         WorkerPreset.cs
// Author: Kyle L. Crowder
// Build Num:  091418



using SentinelCore.Orchestrations.Personas;




namespace SentinelCore.Orchestrations.Agents.AgentPresets;





/// <summary>
///     Preset configuration for Worker agents (Worker1, Worker2, Worker3).
///     Task execution agents that perform specific domain-focused work.
/// </summary>
public record WorkerPreset : AgentPresetBase
{
    /// <inheritdoc />
    public override string AgentName
    {
        get => string.Empty; // Set per-instance
    }

    /// <inheritdoc />
    public override string DefaultInstructions
    {
        get => """
               You are a task execution agent that performs specific investigative or analytical work.
               You receive focused tasks from the workflow manager and execute them using available tools.
               Report findings clearly and concisely. If a task is beyond your capabilities,
               indicate this and suggest alternatives.
               """;
    }

    /// <inheritdoc />
    public override PersonaType? DefaultPersona
    {
        get => PersonaType.TheAnalyst;
    }

    /// <inheritdoc />
    public override MiddlewareFlags MiddlewareFlags
    {
        get => MiddlewareFlags.Utility;
    }

    /// <inheritdoc />
    public override ModelTier Tier
    {
        get => ModelTier.Utility;
    }
}





/// <summary>
///     Preset for Worker1 agent.
/// </summary>
public sealed record Worker1Preset : WorkerPreset
{
    public override string AgentName
    {
        get => "Worker1";
    }
}





/// <summary>
///     Preset for Worker2 agent.
/// </summary>
public sealed record Worker2Preset : WorkerPreset
{
    public override string AgentName
    {
        get => "Worker2";
    }
}





/// <summary>
///     Preset for Worker3 agent.
/// </summary>
public sealed record Worker3Preset : WorkerPreset
{
    public override string AgentName
    {
        get => "Worker3";
    }
}