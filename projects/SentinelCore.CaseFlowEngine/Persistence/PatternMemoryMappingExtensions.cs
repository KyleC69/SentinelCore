// Solution: SentinelCore
// Project:   SentinelCore.CaseFlowEngine
// File:         PatternMemoryMappingExtensions.cs
// Author: Kyle L. Crowder
// Build Num:  081602



namespace SentinelCore.Cfe.Persistence;





/// <summary>
///     Provides extension methods that map between <see cref="PatternMemory" /> contract
///     objects and <see cref="PatternMemoryEntity" /> persistence objects.
/// </summary>
public static class PatternMemoryMappingExtensions
{
    /// <summary>
    ///     Maps a <see cref="PatternMemory" /> DTO to a new <see cref="PatternMemoryEntity" />.
    /// </summary>
    /// <param name="patternMemory">The source pattern memory DTO to map from.</param>
    /// <returns>A new <see cref="PatternMemoryEntity" /> populated with values from <paramref name="patternMemory" />.</returns>
    public static PatternMemoryEntity ToEntity(this PatternMemory patternMemory)
    {
        ArgumentNullException.ThrowIfNull(patternMemory);

        return new PatternMemoryEntity
        {
                Id = patternMemory.Id,
                PatternId = patternMemory.PatternId,
                SignalEmbedding = patternMemory.SignalEmbedding,
                SummaryEmbedding = patternMemory.SummaryEmbedding,
                Summary = patternMemory.Summary,
                Timestamp = patternMemory.Timestamp
        };
    }
}