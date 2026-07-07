// Solution: SentinelCoreLib
// Project:   SentinelCoreLib
// File:         CaseRecord.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



using SentinelCoreLib.Application.Abstractions.Persistence;




namespace SentinelCoreLib.Application.Abstractions;





/// <summary>
///     Persisted representation of a case.
/// </summary>
public sealed class CaseRecord
{

    public CaseRecord(string caseIdValue, string title, CaseStatus status, DateTimeOffset createdAt, DateTimeOffset updatedAt, string stateJson)
    {
    }








    public CaseRecord(string promptSignalText)
    {
        InitiatingPrompt = promptSignalText;
        CaseId = Guid.NewGuid().ToString();
        Title = "New Case";
        Status = CaseStatus.Open;
        CreatedAt = DateTime.Now;
        UpdatedAt = CreatedAt;
        StateJson = "{}";
    }








    /// <summary>
    ///     Gets the case identifier.
    /// </summary>
    public string CaseId { get; }

    /// <summary>
    ///     Gets the case creation timestamp.
    /// </summary>
    public DateTimeOffset CreatedAt { get; }

    /// <summary>
    ///     This is the initial prompt or signal that is sent to The Core to initiate the case. It is stored here for reference
    ///     and auditing purposes.
    /// </summary>
    public string InitiatingPrompt { get; init; }

    /// <summary>
    ///     Gets the JSON-serialized case state.
    /// </summary>
    public string StateJson { get; private set; }

    /// <summary>
    ///     Gets the current case status.
    /// </summary>
    public CaseStatus Status { get; private set; }

    /// <summary>
    ///     Gets the user-provided case title.
    /// </summary>
    public string Title { get; }

    /// <summary>
    ///     Gets the case last-updated timestamp.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; private set; }








    /// <summary>
    ///     Updates the case status and state.
    /// </summary>
    public void Update(CaseStatus status)
    {
        Status = status;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}