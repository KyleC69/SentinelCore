// Solution: SentinelCore
// Project:   SentinelCore.Contracts
// File:         InvestigationPlan.cs
// Author: Kyle L. Crowder
// Build Num:  081312



namespace SentinelCore.Cfe;





public sealed class InvestigationPlan
{

    public string? CaseId { get; set; }
    public int Id { get; set; }

    public int PlanId { get; set; }

    public ICollection<InvestigationPlanStep> Steps { get; set; } = new List<InvestigationPlanStep>();
}