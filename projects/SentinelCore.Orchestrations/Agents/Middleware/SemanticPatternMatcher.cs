// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         SemanticPatternMatcher.cs
// Author: Kyle L. Crowder
// Build Num:  091418



using System.Text.RegularExpressions;

using Microsoft.Extensions.Logging;

using SentinelCore.Contracts.Abstractions;




namespace SentinelCore.Orchestrations.Agents.Middleware;





/// <summary>
///     Provides pattern matching capabilities using keyword-based search with stubs for vector embedding support.
/// </summary>
public sealed class SemanticPatternMatcher : IPatternMatcher
{
    private readonly ILogger<SemanticPatternMatcher> _logger;
    private readonly IPatternMemoryStore _patternStore;

    // Common stop words to ignore in keyword matching
    private static readonly HashSet<string> StopWords =
    [
            "a", "an", "the", "is", "are", "was", "were", "be", "been", "being",
            "have", "has", "had", "do", "does", "did", "will", "would", "could",
            "should", "may", "might", "must", "shall", "can", "need", "dare",
            "ought", "used", "to", "of", "in", "for", "on", "with", "at", "by",
            "from", "as", "into", "through", "during", "before", "after",
            "above", "below", "between", "under", "again", "further", "then",
            "once", "here", "there", "when", "where", "why", "how", "all",
            "each", "few", "more", "most", "other", "some", "such", "no", "nor",
            "not", "only", "own", "same", "so", "than", "too", "very", "just",
            "and", "but", "or", "if", "because", "until", "while", "although",
            "this", "that", "these", "those", "it", "its", "i", "me", "my",
            "we", "our", "you", "your", "he", "she", "they", "them", "their",
            "what", "which", "who", "whom", "please", "help", "thanks", "thank"
    ];








    public SemanticPatternMatcher(IPatternMemoryStore patternStore, ILogger<SemanticPatternMatcher> logger)
    {
        ArgumentNullException.ThrowIfNull(patternStore);
        ArgumentNullException.ThrowIfNull(logger);

        _patternStore = patternStore;
        _logger = logger;
    }








    /// <inheritdoc />
    public async Task<IReadOnlyList<PatternMatchResult>> SearchAsync(string signal, int topK = 5, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(signal);

        if (string.IsNullOrWhiteSpace(signal))
        {
            _logger.LogDebug("Empty signal provided, returning empty results");
            return [];
        }

        // TODO: Implement full embedding-based search when valid case data is available
        // For now, perform keyword-based matching as a fallback

        string[] signalKeywords = ExtractKeywords(signal);

        if (signalKeywords.Length == 0)
        {
            _logger.LogDebug("No keywords extracted from signal: {Signal}", signal);
            return [];
        }

        // TODO: When vector embeddings are available, use SearchByEmbeddingAsync instead
        // For now, perform a basic keyword matching against stored summaries

        _logger.LogDebug("Searching pattern memory with {KeywordCount} keywords: {Keywords}", signalKeywords.Length, string.Join(", ", signalKeywords.Take(10)));

        // Placeholder: Return empty results until valid embeddings/case data is available
        // Real implementation will query _patternStore and perform cosine similarity
        _logger.LogInformation("TODO: Implement full keyword matching against IPatternMemoryStore when case data is available");

        return [];
    }








    /// <inheritdoc />
    public async Task<IReadOnlyList<PatternMatchResult>> SearchByEmbeddingAsync(float[] embedding, int topK = 5, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(embedding);

        if (embedding.Length == 0)
        {
            _logger.LogDebug("Empty embedding provided, returning empty results");
            return [];
        }

        _logger.LogDebug("Searching pattern memory by embedding with topK={TopK}", topK);

        try
        {
            // Use the pattern store's built-in embedding search
            IReadOnlyList<PatternMemoryResult> storeResults = await _patternStore.SearchAsync(embedding, topK, cancellationToken).ConfigureAwait(false);

            if (storeResults.Count == 0)
            {
                _logger.LogDebug("No matching patterns found for the provided embedding");
                return [];
            }

            List<PatternMatchResult> results =
            [
                    .. storeResults.Select(r => new PatternMatchResult
                    {
                            Context = new PatternContext
                            {
                                    PatternId = r.PatternId,
                                    CaseId = r.CaseId,
                                    Summary = r.Summary,
                                    Timestamp = r.Timestamp,
                                    RelevanceScore = 0.9 // TODO: Calculate actual similarity score
                            },
                            MatchMethod = PatternMatchMethod.Semantic,
                            Explanation = "Matched via semantic embedding similarity"
                    })
            ];

            _logger.LogDebug("Found {ResultCount} matching patterns", results.Count);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error searching pattern memory by embedding, returning empty results");
            return [];
        }
    }








    /// <summary>
    ///     Extracts significant keywords from the input text, filtering out stop words.
    /// </summary>
    private static string[] ExtractKeywords(string text)
    {
        // Convert to lowercase and split on non-alphanumeric characters
        string[] words = Regex.Split(text.ToLowerInvariant(), @"[^a-zA-Z0-9]+").Where(w => !string.IsNullOrWhiteSpace(w) && w.Length > 2).ToArray();

        // Filter out stop words
        string[] keywords = words.Where(w => !StopWords.Contains(w)).Distinct().ToArray();

        return keywords;
    }
}