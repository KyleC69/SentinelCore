// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         ClassifierPreset.cs
// Author: Kyle L. Crowder
// Build Num:  091418



using SentinelCore.Orchestrations.Personas;




namespace SentinelCore.Orchestrations.Agents.AgentPresets;





/// <summary>
///     Preset configuration for the Classifier agent.
///     Responsible for routing and classifying incoming signals or requests.
/// </summary>
public sealed record ClassifierPreset : AgentPresetBase
{

    public override string AgentName
    {
        get => "Classifier";
    }





    public override PersonaType? DefaultPersona
    {
        get => PersonaType.TheAnalyst;
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