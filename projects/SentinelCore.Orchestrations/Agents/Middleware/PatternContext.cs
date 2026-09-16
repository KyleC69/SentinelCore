// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         PatternContext.cs
// Author: Kyle L. Crowder
// Build Num:  091418



namespace SentinelCore.Orchestrations.Agents.Middleware;





/// <summary>
///     Represents the context extracted from a matched pattern for injection into agent context.
/// </summary>
public sealed class PatternContext
{

    /// <summary>
    ///     Gets the case identifier this pattern is associated with.
    /// </summary>
    public required int CaseId { get; init; }

    /// <summary>
    ///     Gets optional key phrases extracted from the pattern for additional context.
    /// </summary>
    public IReadOnlyList<string> KeyPhrases { get; init; } = [];

    /// <summary>
    ///     Gets the unique identifier of the pattern.
    /// </summary>
    public required int PatternId { get; init; }

    /// <summary>
    ///     Gets the relevance score of this pattern match (0.0 to 1.0).
    /// </summary>
    public double RelevanceScore { get; init; }

    /// <summary>
    ///     Gets the summary text describing the pattern.
    /// </summary>
    public required string Summary { get; init; }

    /// <summary>
    ///     Gets optional tags associated with this pattern.
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>
    ///     Gets the timestamp when this pattern was recorded.
    /// </summary>
    public DateTime Timestamp { get; init; }
}





/// <summary>
///     Represents a pattern match result with context and relevance scoring.
/// </summary>
public sealed class PatternMatchResult
{
    /// <summary>
    ///     Gets the matched pattern context.
    /// </summary>
    public required PatternContext Context { get; init; }

    /// <summary>
    ///     Gets optional explanation of why this pattern was matched.
    /// </summary>
    public string? Explanation { get; init; }

    /// <summary>
    ///     Gets the match method used to find this result.
    /// </summary>
    public PatternMatchMethod MatchMethod { get; init; }
}





/// <summary>
///     Indicates the method used to match a pattern.
/// </summary>
public enum PatternMatchMethod
{
    /// <summary>
    ///     Matched using keyword/keyword search.
    /// </summary>
    Keyword,

    /// <summary>
    ///     Matched using semantic embedding similarity.
    /// </summary>
    Semantic,

    /// <summary>
    ///     Matched using a hybrid approach combining multiple methods.
    /// </summary>
    Hybrid
}