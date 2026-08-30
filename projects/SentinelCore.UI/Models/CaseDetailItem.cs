// Solution: SentinelCore
// Project:   SentinelCore.UI
// File:         CaseDetailItem.cs
// Author: Kyle L. Crowler
// Build Num:  083003



using SentinelCore.Cfe;


namespace SentinelCore.UI.Models;


/// <summary>
///     Represents an individual case in the drill-down detail grid.
/// </summary>
public sealed class CaseDetailItem
{
    /// <summary>
    ///     The unique identifier of the case.
    /// </summary>
    public Guid CaseId { get; set; }

    /// <summary>
    ///     The date and time the case was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    ///     The current status of the case.
    /// </summary>
    public CaseStatus Status { get; set; }

    /// <summary>
    ///     The date and time the case was last updated, if applicable.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
