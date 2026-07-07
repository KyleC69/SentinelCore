// Solution: SentinelCoreLib
// Project:   SentinelCoreLib
// File:         CaseFile.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



using SentinelCoreLib.Application.Abstractions;
using SentinelCoreLib.Application.Abstractions.Persistence;




namespace SentinelCoreLib.Contracts;





/// <summary>
///     Pure DTO representing a case for UI consumption.
/// </summary>
public sealed class CaseFile
{
    /// <summary>
    ///     The case identifier.
    /// </summary>
    public string CaseId { get; set; } = string.Empty;

    /// <summary>
    ///     The case creation timestamp.- **Local Time** - This is the time when the case was created, represented in the local
    ///     time zone of the system where the case was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    ///     Evidence attached to the case.
    /// </summary>
    public IReadOnlyList<EvidenceItem> Evidence { get; set; } = new List<EvidenceItem>();

    /// <summary>
    ///     Pattern memory matches associated with the case.
    /// </summary>
    public IReadOnlyList<PatternMemoryMatch> PatternMatches { get; set; } = new List<PatternMemoryMatch>();

    /// <summary>
    ///     JSON-serialized case state. - unused?
    /// </summary>
    public string StateJson { get; set; } = string.Empty;

    /// <summary>
    ///     The current case status.
    /// </summary>
    public CaseStatus Status { get; set; }

    /// <summary>
    ///     The user-provided case title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    ///     The case last-updated timestamp.- **Local Time** - This is the time when the case was last updated, represented in
    ///     the local time zone of the system where the case was updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}





/// <summary>
///     Status values for a case.
/// </summary>
public enum CaseStatus
{
    /// <summary>
    ///     The case is open and being investigated.
    /// </summary>
    Open,

    /// <summary>
    ///     The case is awaiting user input.
    /// </summary>
    AwaitingInput,

    /// <summary>
    ///     The case is resolved.
    /// </summary>
    Resolved,

    /// <summary>
    ///     The case was escalated.
    /// </summary>
    Escalated,

    /// <summary>
    ///     The case was blocked by a safety rule.
    /// </summary>
    Blocked
}