// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         CoreChatPreset.cs
// Author: Kyle L. Crowder
// Build Num:  091418

namespace SentinelCore.Orchestrations.Agents.AgentPresets;





/// <summary>
///     Preset configuration for the CoreChat agent.
///     This is the primary user-facing reasoning agent.
/// </summary>
public sealed record CoreChatPreset : AgentPresetBase
{

    public override string AgentName
    {
        get => "CoreChat";
    }




    public override MiddlewareFlags MiddlewareFlags
    {
        get => MiddlewareFlags.Core | MiddlewareFlags.Rag;
    }


    public override ModelTier Tier
    {
        get => ModelTier.Core;
    }
}
