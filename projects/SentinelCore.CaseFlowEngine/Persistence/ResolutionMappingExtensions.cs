// Solution: SentinelCore
// Project:   SentinelCore.Cfe
// File:         ResolutionMappingExtensions.cs
// Author: Kyle L. Crowder
// Build Num:  081312



using SentinelCore.Cfe;




namespace SentinelCore.Cfe.Persistence;





/// <summary>
///     Provides extension methods that map between <see cref="Resolution" /> contract
///     objects and <see cref="ResolutionEntity" /> persistence objects.
/// </summary>
public static class ResolutionMappingExtensions
{
    /// <summary>
    ///     Maps a <see cref="Resolution" /> DTO to a new <see cref="ResolutionEntity" />.
    /// </summary>
    /// <param name="resolution">The source resolution DTO to map from.</param>
    /// <returns>A new <see cref="ResolutionEntity" /> populated with values from <paramref name="resolution" />.</returns>
    public static ResolutionEntity ToEntity(this Resolution resolution)
    {
        ArgumentNullException.ThrowIfNull(resolution);

        return new ResolutionEntity
        {
                Id = resolution.Id,
                CaseRecordId = resolution.CaseRecordId,
                RawJsonContent = resolution.RawJsonContent,
                Notes = resolution.Notes,
                Verified = resolution.Verified
        };
    }
}