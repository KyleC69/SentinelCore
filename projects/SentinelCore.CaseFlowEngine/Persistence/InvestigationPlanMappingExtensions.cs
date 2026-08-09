// Solution: SentinelCore
// Project:   SentinelCore.CaseFlowEngine
// File:         InvestigationPlanMappingExtensions.cs
// Author: Kyle L. Crowder
// Build Num:  080801



using SentinelCore.CaseFlow;




namespace SentinelCore.CaseFlowEngine.Persistence;





/// <summary>
///     Provides extension methods that map between <see cref="InvestigationPlan" /> contract
///     objects and <see cref="InvestigationPlanEntity" /> persistence objects.
/// </summary>
public static class InvestigationPlanMappingExtensions
{
    /// <summary>
    ///     Maps an <see cref="InvestigationPlan" /> DTO to a new <see cref="InvestigationPlanEntity" />.
    /// </summary>
    /// <param name="plan">The source plan DTO to map from.</param>
    /// <returns>A new <see cref="InvestigationPlanEntity" /> populated with values from <paramref name="plan" />.</returns>
    public static InvestigationPlanEntity ToEntity(this InvestigationPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        return new InvestigationPlanEntity { Id = plan.Id, PlanId = plan.PlanId };
    }
}