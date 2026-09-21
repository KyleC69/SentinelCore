// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         MiddlewareFlags.cs
// Author: Kyle L. Crowder
// Build Num:  091418



namespace SentinelCore.Orchestrations.Agents.AgentPresets;





/// <summary>
///     Defines middleware components that can be applied to an agent.
/// </summary>
[Flags]
public enum MiddlewareFlags
{
    /// <summary>
    ///     No middleware applied.
    /// </summary>
    None = 0,

    /// <summary>
    ///     Safety engine middleware for content evaluation.
    /// </summary>
    Safety = 1 << 0,

    /// <summary>
    ///     Pattern memory middleware for semantic pattern matching.
    /// </summary>
    PatternMemory = 1 << 1,

    /// <summary>
    ///     RAG middleware for knowledge base queries.
    /// </summary>
    Rag = 1 << 2,

    /// <summary>
    ///     Event publishing middleware for agent activity tracking.
    /// </summary>
    Events = 1 << 3,

    /// <summary>
    ///     Logging middleware for trace output.
    /// </summary>
    Logging = 1 << 4,


    ContextProvider = 1 << 5,

    /// <summary>
    ///     Common middleware combination for core reasoning agents.
    /// </summary>
    Core = ContextProvider | Safety | PatternMemory | Events | Logging,

    /// <summary>
    ///     Common middleware combination for utility agents.
    /// </summary>
    Utility = Safety | Events | Logging,

    /// <summary>
    ///     Minimal middleware for manager/orchestration agents.
    /// </summary>
    Manager = Events | Logging,

    /// <summary>
    ///     All middleware enabled.
    /// </summary>
    All = Safety | PatternMemory | Rag | Events | Logging
}
