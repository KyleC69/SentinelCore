// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         IPatternMatcher.cs
// Author: Kyle L. Crowder
// Build Num:  091112



namespace SentinelCore.Orchestrations.Agents.Middleware;





/// <summary>
///     Defines the contract for matching user prompts/signals against stored patterns in pattern memory.
/// </summary>
public interface IPatternMatcher
{
    /// <summary>
    ///     Searches for relevant patterns based on a user prompt/signal.
    /// </summary>
    /// <param name="signal">The user signal or prompt text to match against.</param>
    /// <param name="topK">Maximum number of results to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task containing matched pattern contexts ordered by relevance.</returns>
    Task<IReadOnlyList<PatternMatchResult>> SearchAsync(string signal, int topK = 5, CancellationToken cancellationToken = default);








    /// <summary>
    ///     Searches for relevant patterns using a semantic embedding vector.
    /// </summary>
    /// <param name="embedding">The embedding vector for semantic search.</param>
    /// <param name="topK">Maximum number of results to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task containing matched pattern contexts ordered by similarity.</returns>
    Task<IReadOnlyList<PatternMatchResult>> SearchByEmbeddingAsync(float[] embedding, int topK = 5, CancellationToken cancellationToken = default);
}