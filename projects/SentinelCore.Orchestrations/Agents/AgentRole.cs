// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         AgentRole.cs
// Author: Kyle L. Crowder
// Build Num:  082808



namespace SentinelCore.Agents;





/// <summary>
///     Identifies the role an agent plays within the SentinelCore architecture.
///     Determines which event channels are raised and which middleware is applied.
/// </summary>
public enum AgentRole
{
    /// <summary>
    ///     The Core reasoning agent — application lifetime, frontier model.
    /// </summary>
    Core,

    /// <summary>
    ///     The Magnetic Orchestration Manager — workflow lifetime, no tools.
    /// </summary>
    Manager,

    /// <summary>
    ///     A general purpose task oriented agent — per-task lifetime, general toolbelt.
    /// </summary>
    Utility

}