// Solution: SentinelCoreLib
// Project:   SentinelCoreLib
// File:         IEvidenceStore.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



namespace SentinelCoreLib.Application.Abstractions.Persistence;





/// <summary>
///     Stores and retrieves evidence items for cases.
/// </summary>
public interface IEvidenceStore
{
    /// <summary>
    ///     Appends an evidence item for the specified case.
    /// </summary>
    /// <param name="caseId">The case identifier.</param>
    /// <param name="item">The evidence item to store.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddAsync(string caseId, EvidenceItem item, CancellationToken cancellationToken = default);








    /// <summary>
    ///     Gets all evidence items for the specified case.
    /// </summary>
    /// <param name="caseId">The case identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The evidence items for the case.</returns>
    Task<IReadOnlyList<EvidenceItem>> GetByCaseIdAsync(string caseId, CancellationToken cancellationToken = default);
}