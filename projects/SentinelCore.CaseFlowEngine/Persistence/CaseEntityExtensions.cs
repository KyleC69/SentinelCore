// Solution: SentinelCore
// Project:   SentinelCore.CaseFlowEngine
// File:         CaseEntityExtensions.cs
// Author: Kyle L. Crowder
// Build Num:  112101



using SentinelCore.CaseFlowEngine.DBEntities;
using SentinelCore.Contracts.CaseFlow;




namespace SentinelCore.CaseFlowEngine.Persistence;





public static class CaseEntityExtensions
{
    /// <summary>
    ///     Maps a DTO (Case) to its EF Entity representation.
    ///     This must be used by the CaseFlowEngine service layer.
    /// </summary>
    public static CaseEntity ToEntity(this Case dto) =>
            // Implementation to map DTO properties to CaseEntity structure.
            new()
            {

                    // Example mapping:
                    Id = dto.Id,
                    //SignalEntity
                    CaseStatus = 0,
                    UpdatedAt = DateTime.Now,
                    PatternMemory = null,
                    CaseTranscript = dto.CaseTranscript,
                    EvidenceEntities = null,
                    RemediationEntities = null,
                    InvestigationObjective = null,
                    Notes = null
                    // ... other properties ...
            };








    /// <summary>
    ///     Maps an EF <see cref="CaseEntity" /> to its contract representation.
    ///     This is the outbound half of the persistence boundary: callers outside this
    ///     project must never see an EF entity.
    /// </summary>
    /// <param name="entity">The persistence entity to map from.</param>
    /// <returns>A new <see cref="Case" /> contract object populated from <paramref name="entity" />.</returns>
    public static Case ToContract(this CaseEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new()
        {
                Id = entity.Id,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                OpenedAt = entity.CreatedAt,
                CaseTranscript = entity.CaseTranscript ?? string.Empty,
                Status = entity.CaseStatus,
                Signal = entity.SignalEntity is null ? new Signal() : entity.SignalEntity.ToContract()
        };
    }
}
