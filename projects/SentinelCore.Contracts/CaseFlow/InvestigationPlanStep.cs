// Solution: SentinelCore
// Project:   SentinelCore.Contracts
// File:         InvestigationPlanStep.cs
// Author: Kyle L. Crowder
// Build Num:  081312



namespace SentinelCore.Cfe;





public sealed class InvestigationPlanStep
{
    public bool CompletedSuccessfully { get; set; }
    public int Id { get; set; }

    public string Instruction { get; set; } = null!;

    /// <summary>
    ///     If the target of the task is not found this bit must be flipped.
    /// </summary>
    public bool IsTargetPropertyMissing { get; set; }

    public string OperationId { get; set; } = null!;

    public InvestigationPlan Plan { get; set; } = null!;

    public int PlanId { get; set; }

    public string Result { get; set; } = null!;

    /// <summary>
    ///     Links this step to the plan it was created in.
    /// </summary>
    public int StepId { get; set; }

    /// <summary>
    ///     Domain or Surface that the task applies to.
    /// </summary>
    public string Surface { get; set; } = null!;

    public bool TaskBlocked { get; set; }
}