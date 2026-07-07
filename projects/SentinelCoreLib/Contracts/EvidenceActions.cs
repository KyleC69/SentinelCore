// Solution: SentinelCoreLib
// Project:   SentinelCoreLib
// File:         EvidenceActions.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



using SentinelCoreLib.Contracts;





public class EvidenceActions
{

    public string? CaseId { get; set; }
    public List<EvidenceOperation> EvidenceOperations { get; set; } = new();








    public InvestigationPlan ToInvestigationPlan(EvidenceActions evidenceActions)
    {
        InvestigationPlan plan = new() { CaseId = evidenceActions.CaseId ?? string.Empty, Steps = new List<InvestigationPlanStep>() };

        foreach (EvidenceOperation operation in evidenceActions.EvidenceOperations)
        {
            InvestigationPlanStep step = new() { OperationId = operation.OperationId ?? string.Empty, Surface = operation.Surface ?? string.Empty, Instruction = operation.Instruction ?? string.Empty, Result = operation.Result ?? string.Empty };

            plan.Steps.Add(step);
        }

        return plan;
    }








    public class EvidenceOperation
    {
        public string? Instruction { get; set; }
        public string? OperationId { get; set; }
        public string? Result { get; set; }
        public string? Surface { get; set; }
    }
}