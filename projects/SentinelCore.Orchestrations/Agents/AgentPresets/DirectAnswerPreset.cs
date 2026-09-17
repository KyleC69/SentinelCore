// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         ClassifierPreset.cs
// Author: Kyle L. Crowder
// Build Num:  091418

namespace SentinelCore.Orchestrations.Agents.AgentPresets;





/// <summary>
///     Preset configuration for the Classifier agent.
///     Responsible for routing and classifying incoming signals or requests.
/// </summary>
public sealed record DirectAnswerPreset : AgentPresetBase
{

    public override string AgentName
    {
        get => "DirectAnswer";
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