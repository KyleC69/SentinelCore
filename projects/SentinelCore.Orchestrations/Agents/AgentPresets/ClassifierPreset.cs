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
    /// <inheritdoc />
    public override string AgentName
    {
        get => "Classifier";
    }

    /// <inheritdoc />
    public override string DefaultInstructions
    {
        get => """
               You are a classifier agent that analyzes incoming signals and routes them to appropriate handlers.
               Evaluate the input against known categories and provide a classification with confidence.
               When classification is uncertain, indicate ambiguity and suggest alternative categories.
               Return ONLY valid JSON that matches the SignalHypothesis shape.
               Do not include markdown fences, prose, explanations, or any text before/after the JSON object.
               The first character of your response must be '{' and the last character must be '}'.
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