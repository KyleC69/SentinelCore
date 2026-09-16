// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         IRagSearchService.cs
// Author: Kyle L. Crowder
// Build Num:  091418



namespace SentinelCore.Orchestrations.Rag;





/// <summary>
///     Represents a single result from a RAG search operation.
/// </summary>
/// <param name="Id">Unique identifier for the knowledge item.</param>
/// <param name="Title">Title or name of the knowledge item.</param>
/// <param name="Content">The content/body of the knowledge item.</param>
/// <param name="Source">Source system or origin of the knowledge item.</param>
/// <param name="RelevanceScore">Relevance score between 0.0 and 1.0.</param>
/// <param name="Metadata">Additional metadata associated with the item.</param>
public sealed record RagSearchResult(string Id, string Title, string Content, string? Source, double RelevanceScore, IReadOnlyDictionary<string, string>? Metadata = null);





/// <summary>
///     Defines the contract for RAG (Retrieval-Augmented Generation) search operations.
/// </summary>
public interface IRagSearchService
{

    /// <summary>
    ///     Checks if the knowledge base contains any indexed content.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if content is indexed; otherwise, false.</returns>
    Task<bool> HasIndexedContentAsync(CancellationToken cancellationToken = default);








    /// <summary>
    ///     Indexes a new document into the knowledge base.
    /// </summary>
    /// <param name="id">Unique identifier for the document.</param>
    /// <param name="title">Title of the document.</param>
    /// <param name="content">Content to index.</param>
    /// <param name="source">Source system or origin.</param>
    /// <param name="metadata">Optional metadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task IndexDocumentAsync(string id, string title, string content, string? source = null, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default);








    /// <summary>
    ///     Determines if a query is likely relevant to the knowledge base based on keyword matching.
    /// </summary>
    /// <param name="query">The query to evaluate.</param>
    /// <returns>True if the query appears relevant to indexed content; otherwise, false.</returns>
    bool IsQueryRelevant(string query);








    /// <summary>
    ///     Searches the knowledge base for items matching the query.
    /// </summary>
    /// <param name="query">The search query string.</param>
    /// <param name="maxResults">Maximum number of results to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of search results ordered by relevance.</returns>
    Task<IReadOnlyList<RagSearchResult>> SearchAsync(string query, int maxResults = 5, CancellationToken cancellationToken = default);








    /// <summary>
    ///     Searches the knowledge base using a vector embedding (when available).
    /// </summary>
    /// <param name="embedding">The query embedding vector.</param>
    /// <param name="maxResults">Maximum number of results to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of search results ordered by relevance.</returns>
    Task<IReadOnlyList<RagSearchResult>> SearchByEmbeddingAsync(float[] embedding, int maxResults = 5, CancellationToken cancellationToken = default);
}