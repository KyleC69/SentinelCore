// Solution: SentinelCoreLib
// Project:   SentinelCoreLib
// File:         InvestigationWorkItem.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



namespace SentinelCoreLib.Domain.Contracts;





/// <summary>
///     A work item emitted by the CFE for the orchestrator to execute via the Core agent.
///     This is the unit of work that "The Core" is responsible for processing, and it is the only type of work item that
///     "The Core" will accept.
/// </summary>
public sealed class InvestigationWorkItem
{

    /// <summary>
    ///     Initializes a new instance of the <see cref="InvestigationWorkItem" /> class.
    /// </summary>
    public InvestigationWorkItem(CaseId caseId, string message, bool isBlocked = false, string? blockReason = null)
    {
        CaseId = caseId ?? throw new ArgumentNullException(nameof(caseId));
        Message = message ?? throw new ArgumentNullException(nameof(message));
        IsBlocked = isBlocked;
        BlockReason = blockReason;
    }








    /// <summary>
    ///     Gets the blocking reason, if any.
    /// </summary>
    public string? BlockReason { get; }

    /// <summary>
    ///     Gets the case identifier.
    /// </summary>
    public CaseId CaseId { get; }

    /// <summary>
    ///     Gets a value indicating whether the work item was blocked by safety rules.
    /// </summary>
    public bool IsBlocked { get; }

    /// <summary>
    ///     Gets the user message to process.
    /// </summary>
    public string Message { get; }
}