// Solution: SentinelCore
// Project:   SentinelCore.Contracts
// File:         IEvidenceStore.cs
// Author: Kyle L. Crowder
// Build Num:  081602



using SentinelCore.Cfe;




namespace SentinelCore.Abstractions;





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
    Task AddAsync(string caseId, Evidence item, CancellationToken cancellationToken = default);








    /// <summary>
    ///     Gets all evidence items for the specified case.
    /// </summary>
    /// <param name="caseId">The case identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The evidence items for the case.</returns>
    Task<IReadOnlyList<Evidence>> GetByCaseIdAsync(string caseId, CancellationToken cancellationToken = default);
}