// Solution: SentinelCoreLib
// Project:   SentinelCoreLib
// File:         AgentActivityLogEntry.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



namespace SentinelCore.Contracts;





/// <summary>
///     Pure DTO representing a single agent activity log entry.
///     Properties will be refined as the activity surface stabilizes.
/// </summary>
public sealed class AgentActivityLogEntry
{

    /// <summary>
    ///     The name of the agent that produced the activity.
    /// </summary>
    public string? AgentName { get; set; }

    /// <summary>
    ///     The activity payload as JSON.
    /// </summary>
    public string? JsonPayload { get; set; }

    /// <summary>
    ///     The timestamp of the activity.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
}