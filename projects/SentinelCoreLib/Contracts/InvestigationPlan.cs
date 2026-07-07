// Solution: SentinelCoreLib
// Project:   SentinelCoreLib
// File:         InvestigationPlan.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



namespace SentinelCoreLib.Contracts;





/// <summary>
///     Pure DTO representing the plan handed to the Magnetic Workflow Manager
///     by the Core agent. Each step in collection is one domain specific task to be performed.
/// </summary>
public class InvestigationPlan
{

    /// <summary>
    ///     The case identifier associated with this plan.
    /// </summary>
    public string CaseId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    ///     The list of tasks to execute.Each step is a domain specific task to be performed.
    /// </summary>
    public IList<InvestigationPlanStep> Steps { get; set; } = new List<InvestigationPlanStep>();
}