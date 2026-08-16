// Solution: SentinelCore
// Project:   SentinelCore.Contracts
// File:         ActivityType.cs
// Author: Kyle L. Crowder
// Build Num:  081602



namespace SentinelCore.Events;





/// <summary>
///     Categorises the kind of activity being reported through
///     <see cref="SentinelOutputEventArgs" />.
///     This replaces the previous per-channel event model with a single
///     unified output event whose <see cref="ActivityType" /> discriminator
///     tells the UI which category the payload belongs to.
/// </summary>
public enum ActivityType
{
    /// <summary>
    ///     Core agent reasoning or text output.
    /// </summary>
    Core,

    /// <summary>
    ///     Core agent reasoning/thinking output.
    /// </summary>
    Reasoning,

    /// <summary>
    ///     Tool/function-call result output.
    /// </summary>
    Tooling,

    /// <summary>
    ///     Magnetic Manager orchestration output.
    /// </summary>
    Manager,

    /// <summary>
    ///     Domain or composite agent participant output.
    /// </summary>
    Participant,

    /// <summary>
    ///     Magnetic workflow tooling output.
    /// </summary>
    WorkflowTooling,

    /// <summary>
    ///     Orchestration lifecycle event.
    /// </summary>
    Orchestration,

    /// <summary>
    ///     System-level informational or warning message.
    /// </summary>
    System
}