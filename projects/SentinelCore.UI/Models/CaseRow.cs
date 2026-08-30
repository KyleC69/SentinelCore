// Solution: SentinelCore
// Project:   SentinelCore.UI
// File:         CaseRow.cs
// Author: Kyle L. Crowler
// Build Num:  083003



using SentinelCore.Cfe;


namespace SentinelCore.UI.Models;


/// <summary>
///     Represents a row in the case summary grid (one per status).
/// </summary>
public sealed class CaseRow
{
    /// <summary>
    ///     The number of cases in this status.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    ///     The case status this row represents.
    /// </summary>
    public CaseStatus Status { get; set; }
}
