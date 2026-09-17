// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         SafetyAgentPreset.cs
// Author: Kyle L. Crowder
// Build Num:  091418

namespace SentinelCore.Orchestrations.Agents.AgentPresets;





/// <summary>
///     Preset configuration for the SafetyAgent.
///     Evaluates content for safety concerns and policy violations.
/// </summary>
public sealed record SafetyAgentPreset : AgentPresetBase
{

    public override string AgentName
    {
        get => "SafetyAgent";
    }





    public override MiddlewareFlags MiddlewareFlags
    {
        get => MiddlewareFlags.Safety;
    }


    public override ModelTier Tier
    {
        get => ModelTier.Utility;
    }
}