// Solution: SentinelCoreLib
// Project:   SentinelCoreLib
// File:         IPatternMemoryStore.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



namespace SentinelCoreLib.Application.Abstractions.Persistence;





/// <summary>
///     Stores and retrieves pattern memory entries for semantic recall.
/// </summary>
public interface IPatternMemoryStore
{

    /// <summary>
    ///     Searches pattern memory entries by cosine similarity to the provided embedding.
    /// </summary>
    /// <param name="embedding">The query embedding vector.</param>
    /// <param name="topK">The maximum number of results to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Matching entries ordered by similarity descending.</returns>
    Task<IReadOnlyList<PatternMemoryMatch>> SearchAsync(ReadOnlyMemory<float> embedding, int topK, CancellationToken cancellationToken = default);








    /// <summary>
    ///     Stores a pattern memory entry.
    /// </summary>
    /// <param name="entry">The entry to store.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StoreAsync(PatternMemoryEntry entry, CancellationToken cancellationToken = default);
}





/// <summary>
///     A stored pattern memory entry.
/// </summary>
public sealed class PatternMemoryEntry
{

    /// <summary>
    ///     Initializes a new instance of the <see cref="PatternMemoryEntry" /> class.
    /// </summary>
    public PatternMemoryEntry(string entryId, string? caseId, string category, string description, ReadOnlyMemory<float> embedding, string metadataJson, DateTimeOffset? timestamp = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entryId);
        ArgumentException.ThrowIfNullOrWhiteSpace(category);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentException.ThrowIfNullOrWhiteSpace(metadataJson);

        EntryId = entryId;
        CaseId = caseId;
        Category = category;
        Description = description;
        Embedding = embedding;
        MetadataJson = metadataJson;
        Timestamp = timestamp ?? DateTimeOffset.UtcNow;
    }








    /// <summary>
    ///     Gets the case identifier associated with the entry, if any.
    /// </summary>
    public string? CaseId { get; }

    /// <summary>
    ///     Gets the entry category (e.g. "tactic", "artifact", "observation").
    /// </summary>
    public string Category { get; }

    /// <summary>
    ///     Gets the human-readable description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    ///     Gets the embedding vector.
    /// </summary>
    public ReadOnlyMemory<float> Embedding { get; }

    /// <summary>
    ///     Gets the entry identifier.
    /// </summary>
    public string EntryId { get; }

    /// <summary>
    ///     Gets the JSON-serialized metadata.
    /// </summary>
    public string MetadataJson { get; }

    /// <summary>
    ///     Gets the timestamp when the entry was recorded.
    /// </summary>
    public DateTimeOffset Timestamp { get; }
}





/// <summary>
///     A pattern memory search result with similarity score.
/// </summary>
public sealed class PatternMemoryMatch
{

    /// <summary>
    ///     Initializes a new instance of the <see cref="PatternMemoryMatch" /> class.
    /// </summary>
    public PatternMemoryMatch(PatternMemoryEntry entry, float similarity)
    {
        ArgumentNullException.ThrowIfNull(entry);
        Entry = entry;
        Similarity = similarity;
    }








    /// <summary>
    ///     Gets the matched entry.
    /// </summary>
    public PatternMemoryEntry Entry { get; }

    /// <summary>
    ///     Gets the cosine similarity score.
    /// </summary>
    public float Similarity { get; }
}