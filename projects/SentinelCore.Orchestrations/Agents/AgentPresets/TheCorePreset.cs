// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         TheCorePreset.cs
// Author: Kyle L. Crowder
// Build Num:  091418

namespace SentinelCore.Orchestrations.Agents.AgentPresets;





/// <summary>
///     Preset configuration for the TheCore orchestration agent.
///     This is the main orchestration engine that coordinates all other agents.
/// </summary>
public sealed record TheCorePreset : AgentPresetBase
{

    public override string AgentName
    {
        get => "TheCore";
    }





    public override MiddlewareFlags MiddlewareFlags
    {
        get => MiddlewareFlags.Core;
    }


    public override ModelTier Tier
    {
        get => ModelTier.Core;
    }
}