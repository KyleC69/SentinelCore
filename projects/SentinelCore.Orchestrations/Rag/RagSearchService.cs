// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         RagSearchService.cs
// Author: Kyle L. Crowder
// Build Num:  091112



using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using SentinelCore.Contracts.Contracts;




namespace SentinelCore.Orchestrations.Rag;





/// <summary>
///     Provides RAG search functionality with keyword/fuzzy search fallback.
///     Vector search interface is provided for future embedding provider integration.
/// </summary>
public sealed class RagSearchService : IRagSearchService
{
    private readonly List<RagSearchResult> _inMemoryIndex = new();
    private readonly object _indexLock = new();
#pragma warning disable S1144 // Unused private field
#pragma warning disable S1144 // Unused private field
    private bool _isInitialized;
#pragma warning restore S1144
#pragma warning restore S1144
    private readonly ILogger<RagSearchService> _logger;
    private readonly RagSearchOptions _options;








    /// <summary>
    ///     Initializes a new instance of the <see cref="RagSearchService" /> class.
    /// </summary>
    /// <param name="options">RAG search configuration options.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public RagSearchService(IOptions<RagSearchOptions> options, ILogger<RagSearchService> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options.Value;
        _logger = logger;
    }








    /// <inheritdoc />
    public Task<bool> HasIndexedContentAsync(CancellationToken cancellationToken = default)
    {
        lock (_indexLock)
        {
            return Task.FromResult(_inMemoryIndex.Count > 0);
        }
    }








    /// <inheritdoc />
    public Task IndexDocumentAsync(string id, string title, string content, string? source = null, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Document ID cannot be null or empty", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Document content cannot be null or empty", nameof(content));
        }

        lock (_indexLock)
        {
            // Remove existing document with same ID if present
            _inMemoryIndex.RemoveAll(r => r.Id == id);

            RagSearchResult result = new(Id: id, Title: title, Content: content, Source: source, RelevanceScore: 1.0, Metadata: metadata);

            _inMemoryIndex.Add(result);
            _isInitialized = true;
        }

        _logger.LogDebug("Indexed document: {Id}, Title: {Title}", id, title);
        return Task.CompletedTask;
    }








    /// <summary>
    ///     Determines if a query is likely relevant to the knowledge base based on keywords.
    /// </summary>
    /// <param name="query">The query to evaluate.</param>
    /// <returns>True if the query appears relevant; otherwise, false.</returns>
    public bool IsQueryRelevant(string query)
    {
        if (!_options.AutoInjectEnabled || string.IsNullOrWhiteSpace(query))
        {
            return false;
        }

        string lowerQuery = query.ToLowerInvariant();

        // Check for relevance keywords
        foreach (string keyword in _options.RelevanceKeywords)
            if (lowerQuery.Contains(keyword.ToLowerInvariant()))
            {
                return true;
            }

        // If no keywords match and we have indexed content, do a quick search to check
        // if there are any potential matches
        if (_inMemoryIndex.Count > 0)
        {
            List<RagSearchResult> results = PerformKeywordSearch(query, 1);
            return results.Count > 0 && results[0].RelevanceScore >= _options.RelevanceThreshold;
        }

        return false;
    }








    /// <inheritdoc />
    public async Task<IReadOnlyList<RagSearchResult>> SearchAsync(string query, int maxResults = 5, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            _logger.LogWarning("RAG search called with empty query");
            return Array.Empty<RagSearchResult>();
        }

        if (!_options.Enabled)
        {
            _logger.LogDebug("RAG search is disabled");
            return Array.Empty<RagSearchResult>();
        }

        int effectiveMaxResults = Math.Min(maxResults, _options.MaxResults);
        _logger.LogDebug("Performing RAG search for query: {Query}, maxResults: {MaxResults}", query, effectiveMaxResults);

        // Simulate async operation for future embedding provider integration
        await Task.Yield();

        List<RagSearchResult> results;
        lock (_indexLock)
        {
            results = PerformKeywordSearch(query, effectiveMaxResults);
        }

        _logger.LogDebug("RAG search returned {Count} results", results.Count);
        return results;
    }








    /// <inheritdoc />
    public Task<IReadOnlyList<RagSearchResult>> SearchByEmbeddingAsync(float[] embedding, int maxResults = 5, CancellationToken cancellationToken = default)
    {
        if (!_options.VectorSearchEnabled)
        {
            _logger.LogDebug("Vector search is not enabled");
            return Task.FromResult<IReadOnlyList<RagSearchResult>>(Array.Empty<RagSearchResult>());
        }

        // TODO: Implement actual vector search when embedding provider is configured
        _logger.LogWarning("Vector search by embedding is not yet implemented");
        return Task.FromResult<IReadOnlyList<RagSearchResult>>(Array.Empty<RagSearchResult>());
    }








    /// <summary>
    ///     Calculates relevance score based on keyword matching and fuzzy similarity.
    /// </summary>
    private static double CalculateRelevanceScore(RagSearchResult result, string[] queryTerms)
    {
        string titleLower = result.Title.ToLowerInvariant();
        string contentLower = result.Content.ToLowerInvariant();

        double titleMatches = 0;
        double contentMatches = 0;

        foreach (string term in queryTerms)
        {
            // Title matches are weighted higher
            if (titleLower.Contains(term))
            {
                titleMatches += 2;
            }
            else if (FuzzyMatch(term, titleLower))
            {
                titleMatches += 1;
            }

            if (contentLower.Contains(term))
            {
                contentMatches++;
            }
            else if (FuzzyMatch(term, contentLower))
            {
                contentMatches += 0.5;
            }
        }

        // Normalize scores
        double maxPossibleScore = queryTerms.Length * 3; // 2 for title + 1 for content
        double rawScore = (titleMatches + contentMatches) / maxPossibleScore;

        return Math.Min(1.0, rawScore);
    }








    /// <summary>
    ///     Simple fuzzy matching using substring presence and Levenshtein-like distance.
    /// </summary>
    private static bool FuzzyMatch(string term, string text)
    {
        if (string.IsNullOrEmpty(term) || string.IsNullOrEmpty(text))
        {
            return false;
        }

        // Check for partial match (term is substring of any word in text)
        string[] words = text.Split(new[] { ' ', ',', '.', '?', '!', ';', ':', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string word in words)
            if (word.Length >= term.Length && (word.StartsWith(term, StringComparison.OrdinalIgnoreCase) || word.EndsWith(term, StringComparison.OrdinalIgnoreCase) || LevenshteinDistance(word, term) <= Math.Max(1, term.Length / 3)))
            {
                return true;
            }

        return false;
    }








    /// <summary>
    ///     Calculates Levenshtein distance between two strings.
    /// </summary>
    private static int LevenshteinDistance(string source, string target)
    {
        if (string.IsNullOrEmpty(source))
        {
            return string.IsNullOrEmpty(target) ? 0 : target.Length;
        }

        if (string.IsNullOrEmpty(target))
        {
            return source.Length;
        }

        int sourceLength = source.Length;
        int targetLength = target.Length;

        int[,] distance = new int[sourceLength + 1, targetLength + 1];

        for (int i = 0; i <= sourceLength; distance[i, 0] = i++)
        {
        }

        for (int j = 0; j <= targetLength; distance[0, j] = j++)
        {
        }

        for (int i = 1; i <= sourceLength; i++)
        {
            for (int j = 1; j <= targetLength; j++)
            {
                int cost = target[j - 1] == source[i - 1] ? 0 : 1;

                distance[i, j] = Math.Min(Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1), distance[i - 1, j - 1] + cost);
            }
        }

        return distance[sourceLength, targetLength];
    }








    /// <summary>
    ///     Performs keyword-based search with fuzzy matching.
    /// </summary>
    private List<RagSearchResult> PerformKeywordSearch(string query, int maxResults)
    {
        if (_inMemoryIndex.Count == 0)
        {
            return new List<RagSearchResult>();
        }

        string[] queryTerms = query.ToLowerInvariant().Split(new[] { ' ', ',', '.', '?', '!', ';', ':', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);

        List<(RagSearchResult Result, double Score)> scoredResults = new();

        foreach (RagSearchResult result in _inMemoryIndex)
        {
            double score = CalculateRelevanceScore(result, queryTerms);
            if (score > 0)
            {
                scoredResults.Add((result, score));
            }
        }

        return scoredResults.OrderByDescending(x => x.Score).Take(maxResults).Select(x => new RagSearchResult(x.Result.Id, x.Result.Title, x.Result.Content, x.Result.Source, x.Score, x.Result.Metadata)).ToList();
    }
}