// Solution: SentinelCore
// Project:   SentinelCore.CaseFlowEngine
// File:         InvestigationPlanStepMappingExtensions.cs
// Author: Kyle L. Crowder
// Build Num:  080801



using SentinelCore.CaseFlow;




namespace SentinelCore.CaseFlowEngine.Persistence;





/// <summary>
///     Provides extension methods that map between <see cref="InvestigationPlanStep" /> contract
///     objects and <see cref="InvestigationPlanStepsEntity" /> persistence objects.
/// </summary>
public static class InvestigationPlanStepMappingExtensions
{
    /// <summary>
    ///     Maps an <see cref="InvestigationPlanStep" /> DTO to a new <see cref="InvestigationPlanStepsEntity" />.
    /// </summary>
    /// <param name="step">The source step DTO to map from.</param>
    /// <returns>A new <see cref="InvestigationPlanStepsEntity" /> populated with values from <paramref name="step" />.</returns>
    public static InvestigationPlanStepsEntity ToEntity(this InvestigationPlanStep step)
    {
        ArgumentNullException.ThrowIfNull(step);

        return new InvestigationPlanStepsEntity
        {
                Id = step.Id,
                StepId = step.StepId,
                PlanId = step.PlanId,
                Surface = step.Surface,
                Instruction = step.Instruction,
                Result = step.Result,
                CompletedSuccessfully = step.CompletedSuccessfully,
                TaskBlocked = step.TaskBlocked,
                IsTargetPropertyMissing = step.IsTargetPropertyMissing
        };
    }
}