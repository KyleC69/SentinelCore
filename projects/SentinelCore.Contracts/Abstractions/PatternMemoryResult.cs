// Solution: SentinelCore
// Project:   SentinelCore.Contracts
// File:         PatternMemoryResult.cs
// Author: Kyle L. Crowder
// Build Num:  081602



namespace SentinelCore.Abstractions;





/// <summary>
///     A read-only result returned by <see cref="IPatternMemoryStore" /> queries.
///     This is a clean DTO that does not carry EF Core persistence attributes.
/// </summary>
public sealed class PatternMemoryResult
{

    /// <summary>
    ///     Gets the case identifier this entry is associated with.
    /// </summary>
    public int CaseId { get; init; }

    /// <summary>
    ///     Gets the pattern memory entry identifier.
    /// </summary>
    public int PatternId { get; init; }

    /// <summary>
    ///     Gets the signal embedding vector, if available.
    /// </summary>
    public float[]? SignalEmbedding { get; init; }

    /// <summary>
    ///     Gets the summary text of the pattern.
    /// </summary>
    public string Summary { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the summary embedding vector, if available.
    /// </summary>
    public float[]? SummaryEmbedding { get; init; }

    /// <summary>
    ///     Gets the timestamp of when this pattern was recorded.
    /// </summary>
    public DateTime Timestamp { get; init; }
}