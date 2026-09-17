// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         WorkerPreset.cs
// Author: Kyle L. Crowder
// Build Num:  091418

namespace SentinelCore.Orchestrations.Agents.AgentPresets;





/// <summary>
///     Preset configuration for Worker agents (Worker1, Worker2, Worker3).
///     Task execution agents that perform specific domain-focused work.
/// </summary>
public record WorkerPreset : AgentPresetBase
{

    public override string AgentName
    {
        get => string.Empty; // Set per-instance
    }

    public override MiddlewareFlags MiddlewareFlags
    {
        get => MiddlewareFlags.Utility;
    }


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