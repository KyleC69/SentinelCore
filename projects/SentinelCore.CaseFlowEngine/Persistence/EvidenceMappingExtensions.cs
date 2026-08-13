// Solution: SentinelCore
// Project:   SentinelCore.CaseFlowEngine
// File:         EvidenceMappingExtensions.cs
// Author: Kyle L. Crowder
// Build Num:  081312



using SentinelCore.CaseFlow;




namespace SentinelCore.CaseFlowEngine.Persistence;





/// <summary>
///     Provides extension methods that map between <see cref="Evidence" /> contract
///     objects and <see cref="EvidenceEntity" /> persistence objects.
/// </summary>
public static class EvidenceMappingExtensions
{
    /// <summary>
    ///     Maps an <see cref="Evidence" /> DTO to a new <see cref="EvidenceEntity" />.
    /// </summary>
    /// <param name="evidence">The source evidence DTO to map from.</param>
    /// <returns>A new <see cref="EvidenceEntity" /> populated with values from <paramref name="evidence" />.</returns>
    public static EvidenceEntity ToEntity(this Evidence evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);

        return new EvidenceEntity
        {
                Id = evidence.Id,
                EvidenceId = evidence.EvidenceId,
                ContentJson = evidence.ContentJson,
                Provenance = evidence.Provenance,
                Source = evidence.Source,
                Timestamp = evidence.Timestamp,
                Type = evidence.Type
        };
    }
}