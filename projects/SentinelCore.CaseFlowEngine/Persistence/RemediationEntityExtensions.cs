using SentinelCore.Contracts.CaseFlow;
using SentinelCore.Data.DBEntities;




namespace SentinelCore.CaseFlowEngine.Persistence;

public static class RemediationEntityExtensions
{
    /// <summary>
    /// Maps a DTO to its EF Entity representation.
    /// This must be used by the CaseFlowEngine service layer.
    /// </summary>
    public static RemediationEntity ToEntity(this Remediation dto)
    {
        return new RemediationEntity
        {
            Id = dto.Id,
            CaseId = dto.CaseID,
            Case = new CaseEntity
            {
                Id = dto.Case.Id,
                CaseStatus = dto.Case.Status,
                CreatedAt = dto.Case.CreatedAt,
                UpdatedAt = dto.Case.UpdatedAt,
                CaseTranscript = dto.Case.CaseTranscript,
                Notes = dto.Case.CaseTranscript // Assuming Notes maps to CaseTranscript
            },
            Summary = dto.Summary,
            Command = dto.Command,
            Confidence = dto.Confidence,
            Impact = dto.Impact,
            Notes = dto.Notes
        };
    }
}
