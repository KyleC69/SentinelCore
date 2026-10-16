








using SentinelCore.Contracts.CaseFlow;
using SentinelCore.Data.DBEntities;




namespace SentinelCore.CaseFlowEngine.Persistence;

public static class CaseEntityExtensions
{
    /// <summary>
    /// Maps a DTO (Case) to its EF Entity representation.
    /// This must be used by the CaseFlowEngine service layer.
    /// </summary>
    public static CaseEntity ToEntity(this Case dto) =>
        // Implementation to map DTO properties to CaseEntity structure.
        new CaseEntity
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
            InvestigationOutcome = null,
            Notes = null,
            // ... other properties ...
        };
}
