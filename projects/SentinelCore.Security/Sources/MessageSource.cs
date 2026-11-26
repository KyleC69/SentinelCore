// Solution: SentinelCore
// Project:   SentinelCore.Security
// File:         MessageSource.cs
// Author: Kyle L. Crowder
// Build Num:  112518



namespace SentinelCore.Security.Sources;





/// <summary>
///     A message source in the fixed Sentinel source registry. The pair of
///     <see cref="SourceId" /> and <see cref="SourceType" /> is the provenance stamped
///     onto every message the source produces.
/// </summary>
/// <param name="SourceId">The locked source identifier (GUID).</param>
/// <param name="SourceType">The locked source type.</param>
/// <param name="DisplayName">Human-readable name used for operator display.</param>
public sealed record MessageSource(Guid SourceId, SourceType SourceType, string DisplayName)
{

    /// <summary>
    ///     Creates and validates a message source.
    /// </summary>
    /// <param name="sourceId">The locked source identifier (GUID).</param>
    /// <param name="sourceType">The locked source type.</param>
    /// <param name="displayName">Human-readable name used for operator display.</param>
    /// <returns>The validated message source.</returns>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="sourceId" /> is empty, the source type is not part
    ///     of the fixed list, or <paramref name="displayName" /> is empty or whitespace.
    /// </exception>
    public static MessageSource Create(Guid sourceId, SourceType sourceType, string displayName)
    {
        if (sourceId == Guid.Empty)
        {
            throw new ArgumentException("A non-empty source identifier is required.", nameof(sourceId));
        }

        if (!SourceTypes.IsDefined(sourceType))
        {
            throw new ArgumentException("The source type is not part of the fixed source type list.", nameof(sourceType));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("A display name is required.", nameof(displayName));
        }

        return new(sourceId, sourceType, displayName);
    }
}
