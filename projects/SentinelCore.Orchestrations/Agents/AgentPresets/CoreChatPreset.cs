// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         CoreChatPreset.cs
// Author: Kyle L. Crowder
// Build Num:  091300



using SentinelCore.Orchestrations.Personas;




namespace SentinelCore.Orchestrations.Agents.AgentPresets;





/// <summary>
///     Preset configuration for the CoreChat agent.
///     This is the primary user-facing reasoning agent.
/// </summary>
public sealed record CoreChatPreset : AgentPresetBase
{
    /// <inheritdoc />
    public override string AgentName
    {
        get => "CoreChat";
    }

    /// <inheritdoc />
    public override string DefaultInstructions
    {
        get => """
               You are CoreChat, the primary reasoning agent in the SentinelCore platform.
               You handle direct user interactions and coordinate with other agents to fulfill requests.
               Be thorough, accurate, and helpful. Escalate complex tasks to the appropriate specialized agents.
               """;
    }

    /// <inheritdoc />
    public override PersonaType? DefaultPersona
    {
        get => PersonaType.TheAdvisor;
    }

    /// <inheritdoc />
    public override MiddlewareFlags MiddlewareFlags
    {
        get => MiddlewareFlags.Core | MiddlewareFlags.Rag;
    }

    /// <inheritdoc />
    public override ModelTier Tier
    {
        get => ModelTier.Core;
    }
}