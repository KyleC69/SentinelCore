// Solution: SentinelCore
// Project:   SentinelCore.Contracts
// File:         InvestigationPlan.cs
// Author: Kyle L. Crowder
// Build Num:  080801



namespace SentinelCore.CaseFlow;





public sealed class InvestigationPlan
{

    public string? CaseId { get; set; }
    public int Id { get; set; }

    public int PlanId { get; set; }

    public ICollection<InvestigationPlanStep> Steps { get; set; } = new List<InvestigationPlanStep>();
}