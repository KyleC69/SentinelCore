// Solution: SentinelCoreLib
// Project:   SentinelCoreLib
// File:         InvestigationPlanStep.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



namespace SentinelCoreLib.Contracts;





public class InvestigationPlanStep
{

    /// <summary>
    ///     Indicates whether the task was completed successfully.
    /// </summary>
    public bool CompletedSuccessfully { get; set; } = false;

    /// <summary>
    ///     The instruction for the operation.
    /// </summary>
    public string Instruction { get; set; } = string.Empty;

    /// <summary>
    ///     Indicates whether the target property was missing.
    /// </summary>
    public bool IsTargetPropertyMissing { get; set; } = false;

    /// <summary>
    ///     The unique identifier for the operation associated with this investigation step.
    /// </summary>
    public string OperationId { get; set; } = string.Empty;

    /// <summary>
    ///     The result of the operation.
    /// </summary>
    public string Result { get; set; } = string.Empty;

    /// <summary>
    ///     The surface on which the operation is performed.
    /// </summary>
    public string Surface { get; set; } = string.Empty;

    /// <summary>
    ///     Indicates whether the task was blocked by operating system (access denied) or Sentinel safety rules.
    /// </summary>
    public bool TaskBlocked { get; set; } = false;
}