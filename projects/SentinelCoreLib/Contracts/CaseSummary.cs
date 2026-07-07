// Solution: SentinelCoreLib
// Project:   SentinelCoreLib
// File:         CaseSummary.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



using SentinelCoreLib.Contracts;




namespace SentinelCore.Contracts;





/// <summary>
///     Pure DTO summarizing a case for list views.
/// </summary>
public sealed class CaseSummary
{
    /// <summary>
    ///     The case identifier.
    /// </summary>
    public string CaseId { get; set; } = string.Empty;

    /// <summary>
    ///     The case creation timestamp.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    ///     The current case status.
    /// </summary>
    public CaseStatus Status { get; set; }

    /// <summary>
    ///     A short preview of the current state or outcome.
    /// </summary>
    public string? SummaryText { get; set; }

    /// <summary>
    ///     The user-provided case title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    ///     The case last-updated timestamp.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }
}