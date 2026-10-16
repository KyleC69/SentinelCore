// Solution: SentinelCore
// Project:   SentinelCore.CaseFlowEngine
// File:         EvidenceMappingExtensions.cs
// Author: Kyle L. Crowder
// Build Num:  100908



using SentinelCore.Contracts.CaseFlow;
using SentinelCore.Data.DBEntities;




namespace SentinelCore.CaseFlowEngine.Persistence;





/// <summary>
///     Provides extension methods that map between <see cref="Evidence" /> contract
///     objects and <see cref="EvidenceEntity" /> persistence objects.
/// </summary>
public static class EvidenceMappingExtensions
{



    /// <summary>
    /// Maps the DTO instance to the corresponding EF Entity.
    /// </summary>
    /// <returns>The mapped EF Entity.</returns>
    public static EvidenceEntity ToEntity(this Evidence evidence)
    {
        // Implementation logic will go here, mapping all public properties
        // to the fields of the EvidenceToEntity structure.
        return new EvidenceEntity
        {

            // Example mapping:
            Id = evidence.Id,
            CollectorAgent = evidence.CollectorAgent,
            SourcePath = evidence.SourcePath,
            SourceType = evidence.SourceType,
            Observation = evidence.Observation,
            ObservedAt = evidence.ObservedAt,
            CreatedAt = evidence.CreatedAt,
            Confidence = evidence.Confidence,
            CaseId = evidence.CaseId,
            CaseEntity = evidence.Case.ToEntity(),
            // Other properties...
        };
    }

}