// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         SafetySeverity.cs
// Author: Kyle L. Crowder
// Build Num:  081602



namespace SentinelCore.SafetyEngine;





/// <summary>
///     Represents the severity level of a safety rule violation.
/// </summary>
public enum SafetySeverity
{
    /// <summary>No violation — the prompt passed the rule.</summary>
    None = 0,

    /// <summary>A low-severity violation — informational, does not block.</summary>
    Low = 1,

    /// <summary>A medium-severity violation — warning, may be logged but does not block.</summary>
    Medium = 2,

    /// <summary>A high-severity violation — the prompt is blocked.</summary>
    High = 3,

    /// <summary>A critical violation — the prompt is blocked and may trigger alerts.</summary>
    Critical = 4
}





/// <summary>
///     Represents the action a safety rule recommends for a given prompt.
/// </summary>
public enum SafetyAction
{
    /// <summary>The prompt is allowed to proceed to the AI model.</summary>
    Allow = 0,

    /// <summary>The prompt is blocked and will not reach the AI model.</summary>
    Block = 1,

    /// <summary>The prompt is allowed but flagged for monitoring.</summary>
    Warn = 2
}