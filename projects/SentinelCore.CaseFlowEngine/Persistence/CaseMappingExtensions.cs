// Solution: SentinelCore
// Project:   SentinelCore.CaseFlowEngine
// File:         CaseMappingExtensions.cs
// Author: Kyle L. Crowder
// Build Num:  081602



using SentinelCore.Contracts;




namespace SentinelCore.Cfe.Persistence;





/// <summary>
///     Provides extension methods that map between <see cref="Case" /> contract
///     objects and <see cref="CaseEntity" /> persistence objects.
/// </summary>
public static class CaseMappingExtensions
{
    /// <summary>
    ///     Maps a <see cref="Case" /> DTO to a new <see cref="CaseEntity" />.
    /// </summary>
    /// <param name="case">The source case DTO to map from.</param>
    /// <returns>A new <see cref="CaseEntity" /> populated with values from <paramref name="case" />.</returns>
    public static CaseEntity ToEntity(this Case @case)
    {
        ArgumentNullException.ThrowIfNull(@case);

        return new CaseEntity
        {
                Id = @case.Id,
                CaseId = @case.CaseId,
                Status = (int)@case.Status,
                InitiatingSignal = @case.InitiatingSignal,
                CreatedAt = @case.CreatedAt,
                UpdatedAt = @case.UpdatedAt,
                PlanId = @case.PlanId,
                PatternMemoryId = @case.PatternMemoryId
        };
    }





    /// <summary>
    ///     Maps a <see cref="CaseEntity" /> persistence object to a <see cref="Case" /> contract object.
    /// </summary>
    public static Case ToCase(this CaseEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new Case
        {
            Id = entity.Id,
            CaseId = entity.CaseId,
            Status = (CaseStatus)entity.Status,
            InitiatingSignal = entity.InitiatingSignal,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            PlanId = entity.PlanId,
            PatternMemoryId = entity.PatternMemoryId,
        };
    }
}