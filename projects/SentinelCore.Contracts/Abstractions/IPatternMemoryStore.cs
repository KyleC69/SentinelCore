// Solution: SentinelCore
// Project:   SentinelCore.Contracts
// File:         IPatternMemoryStore.cs
// Author: Kyle L. Crowder
// Build Num:  081312



namespace SentinelCore.Abstractions;





/// <summary>
///     Stores and retrieves pattern memory entries for semantic recall.
/// </summary>
public interface IPatternMemoryStore
{

    /// <summary>
    ///     Retrieves pattern memory entries associated with a specific case.
    /// </summary>
    /// <param name="caseId">The case identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Pattern memory entries for the specified case.</returns>
    Task<IReadOnlyList<PatternMemoryResult>> GetByCaseIdAsync(string caseId, CancellationToken cancellationToken = default);








    /// <summary>
    ///     Searches pattern memory entries by cosine similarity to the provided embedding.
    /// </summary>
    /// <param name="embedding">The query embedding vector.</param>
    /// <param name="topK">The maximum number of results to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Matching entries ordered by similarity descending.</returns>
    Task<IReadOnlyList<PatternMemoryResult>> SearchAsync(float[] embedding, int topK = 10, CancellationToken cancellationToken = default);








    /// <summary>
    ///     Stores a pattern memory entry.
    /// </summary>
    /// <param name="caseId">The case identifier to associate the entry with.</param>
    /// <param name="summary">The summary text of the pattern.</param>
    /// <param name="signalEmbedding">The signal embedding vector.</param>
    /// <param name="summaryEmbedding">The summary embedding vector.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task StoreAsync(string caseId, string summary, float[] signalEmbedding, float[] summaryEmbedding, CancellationToken cancellationToken = default);
}