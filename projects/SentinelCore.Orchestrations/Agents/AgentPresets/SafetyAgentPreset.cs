// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         SafetyAgentPreset.cs
// Author: Kyle L. Crowder
// Build Num:  091300



using SentinelCore.Orchestrations.Personas;




namespace SentinelCore.Orchestrations.Agents.AgentPresets;





/// <summary>
///     Preset configuration for the SafetyAgent.
///     Evaluates content for safety concerns and policy violations.
/// </summary>
public sealed record SafetyAgentPreset : AgentPresetBase
{
    /// <inheritdoc />
    public override string AgentName
    {
        get => "SafetyAgent";
    }

    /// <inheritdoc />
    public override string DefaultInstructions
    {
        get => """
               You are a safety evaluation agent. Your role is to assess content for potential safety concerns,
               policy violations, and harmful outputs. Evaluate inputs and outputs rigorously.
               When you identify a concern, provide a clear classification (Low, Medium, High, Critical)
               along with the specific rule or policy that was triggered.
               Never allow harmful content to pass through. When in doubt, escalate to the highest severity.
               """;
    }

    /// <inheritdoc />
    public override PersonaType? DefaultPersona
    {
        get => PersonaType.TheCritic;
    }

    /// <inheritdoc />
    public override MiddlewareFlags MiddlewareFlags
    {
        get => MiddlewareFlags.Safety;
    }

    /// <inheritdoc />
    public override ModelTier Tier
    {
        get => ModelTier.Utility;
    }
}