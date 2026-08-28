// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         CoreRoutingDecision.cs
// Author: Kyle L. Crowder
// Build Num:  082808



using System.Text.Json.Serialization;




namespace SentinelCore.Workflows;





public enum NextStep
{

    /// <summary>
    ///     If signal is not noise, and may indicate system failure, intrusion, safety issue, or other critical event, then
    ///     immediate escalation to human operator is required.
    ///     This is the highest priority and most critical decision point in the decision tree.
    /// </summary>
    RedAlert,

    /// <summary>
    ///     The AI has determined that the signal is not noise, but cannot be answered directly, and requires further
    ///     investigation of the host system by the worker agents.
    ///     This is a normal case decision point in the decision tree, and is the most common. This decision initiates a
    ///     sub-workflow, the magnetic investigation workflow.
    ///     The sub-workflow dispatches the appropriate worker agents to investigate the host system and gather more
    ///     information about the signal, and then returns the results
    ///     to TheCore AI for further analysis.
    /// </summary>
    Investigate,

    /// <summary>
    ///     The signal is not noise, and the AI has determined that it cannot be answered directly, but cannot yet determine
    ///     the proper area to investigate,
    ///     and requires more information from the user to determine the proper area to investigate.
    /// </summary>
    MoreInformationRequired,

    /// <summary>
    ///     The signal is not noise, and the AI has determined that it cannot be answered directly, but cannot yet determine
    ///     the proper area to investigate.
    ///     Similar to <see cref="MoreInformationRequired" />, but this is a special edge case that may be triggered from any
    ///     step in any sub-workflow.
    ///     This may come from a system error or performance bottleneck, safety issue, or other non-critical event that
    ///     requires human intervention to resolve. This is a special case that indicates it does need user intervention
    ///     for the case to transition.
    /// </summary>
    EscalateToHumanOperator,
    DirectAnswer
}





/// <summary>
///     Represents the decision made by the AI based on its interpretation of the signal, and is used to route the signal
///     to the appropriate next step in the workflow.
/// </summary>
public class CoreRoutingDecision
{

    /// <summary>
    ///     Confidence score of the model's interpretation of the signal.
    /// </summary>
    [JsonPropertyName("confidenceScore")]
    public double ConfidenceScore { get; set; }

    //The next step in the workflow that should be activated.
    [JsonPropertyName("nextstep")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public NextStep NextStep { get; set; }
}





public class SafetyPrerequisites
{
    public bool ConfidenceThresholdMet { get; init; } = false;
    public bool EvidenceRequired { get; init; } = false;
    public double RequiredConfidence { get; init; } = 0.5;
}