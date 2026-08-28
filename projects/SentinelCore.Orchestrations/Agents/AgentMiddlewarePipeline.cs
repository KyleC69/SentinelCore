// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         AgentMiddlewarePipeline.cs
// Author: Kyle L. Crowder
// Build Num:  082808



namespace SentinelCore.Agents;





/// <summary>
///     Predefined middleware pipeline flags for different agent roles.
///     These flags determine which client wrappers and builder-level middleware
///     are applied during agent construction in <see cref="SentinelAgentFactory" />.
///     <para>
///         Client wrappers (safety → logging → events) are applied to <c>IChatClient</c>
///         before constructing the <c>ChatClientAgent</c>.
///         Builder-level middleware (pattern memory, null safety) are applied via
///         <c>ChatClientAgent.Builder</c> after construction.
///     </para>
/// </summary>
public sealed class AgentMiddlewarePipeline
{

    /// <summary>
    ///     Core agent pipeline: logging + events + safety + pattern memory + null safety.
    /// </summary>
    public static readonly AgentMiddlewarePipeline Core = new()
    {
            UseLogging = true,
            UseEvents = true,
            UseSafety = true,
            UsePatternMemory = true,
            UseNullSafety = true
    };

    /// <summary>
    ///     Default pipeline: logging + events + safety.
    /// </summary>
    public static readonly AgentMiddlewarePipeline Default = new()
    {
            UseLogging = true,
            UseEvents = true,
            UseSafety = true,
            UsePatternMemory = false,
            UseNullSafety = false
    };

    /// <summary>
    ///     Domain/Worker pipeline: logging + events + safety.
    /// </summary>
    public static readonly AgentMiddlewarePipeline Domain = new()
    {
            UseLogging = true,
            UseEvents = true,
            UseSafety = true,
            UsePatternMemory = false,
            UseNullSafety = false
    };

    /// <summary>
    ///     Manager/Orchestrator pipeline: logging + events only (no tools, no safety).
    /// </summary>
    public static readonly AgentMiddlewarePipeline Manager = new()
    {
            UseLogging = true,
            UseEvents = true,
            UseSafety = false,
            UsePatternMemory = false,
            UseNullSafety = false
    };

    /// <summary>
    ///     Minimal pipeline: logging only.
    /// </summary>
    public static readonly AgentMiddlewarePipeline Minimal = new()
    {
            UseLogging = true,
            UseEvents = false,
            UseSafety = false,
            UsePatternMemory = false,
            UseNullSafety = false
    };

    /// <summary>
    ///     Enable event publishing via <c>EventPublishingChatClient</c> wrapper.
    /// </summary>
    public bool UseEvents { get; init; }

    /// <summary>
    ///     Enable trace logging via <c>LoggingChatClient</c> wrapper.
    /// </summary>
    public bool UseLogging { get; init; }

    /// <summary>
    ///     Enable null-safety context provider (Core agents only).
    ///     Applied as builder-level middleware on <c>ChatClientAgent</c>.
    /// </summary>
    public bool UseNullSafety { get; init; }

    /// <summary>
    ///     Enable pattern memory context injection (Core agents only).
    ///     Applied as builder-level middleware on <c>ChatClientAgent</c>.
    /// </summary>
    public bool UsePatternMemory { get; init; }

    /// <summary>
    ///     Enable model output safety/cleaning via <c>ModelNoiseSafety</c> wrapper.
    /// </summary>
    public bool UseSafety { get; init; }
}