// Solution: SentinelCoreLib
// Project:   SentinelCoreLib
// File:         ICaseRepository.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



namespace SentinelCoreLib.Application.Abstractions.Persistence;





/// <summary>
///     Stores and retrieves case records.
/// </summary>
public interface ICaseRepository
{
    /// <summary>
    ///     Creates a new case record.
    /// </summary>
    /// <param name="caseRecord">The case to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CreateAsync(CaseRecord caseRecord, CancellationToken cancellationToken = default);








    /// <summary>
    ///     Gets a case by identifier, or null if not found.
    /// </summary>
    /// <param name="caseId">The case identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The case record, or null.</returns>
    Task<CaseRecord?> GetByIdAsync(string caseId, CancellationToken cancellationToken = default);








    /// <summary>
    ///     Lists all cases ordered by creation time descending.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All case records.</returns>
    Task<IReadOnlyList<CaseRecord>> ListAsync(CancellationToken cancellationToken = default);








    /// <summary>
    ///     Updates an existing case record.
    /// </summary>
    /// <param name="caseRecord">The case to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateAsync(CaseRecord caseRecord, CancellationToken cancellationToken = default);
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