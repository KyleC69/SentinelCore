// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         SignalHypothesis.cs
// Author: Kyle L. Crowder
// Build Num:  092308



using System.ComponentModel;
using System.Text.Json.Serialization;




namespace SentinelCore.Orchestrations.Workflows;





/// <summary>
///     Represents a signal classification hypothesis used by the classifier agent to determine
///     the appropriate next step in the workflow.
/// </summary>
public sealed class SignalHypothesis
{

    /// <summary>
    ///     Confidence score between 0.0 and 1.0 indicating the classifier's certainty.
    /// </summary>
    [JsonPropertyName("confidenceScore")]
    public double ConfidenceScore { get; set; }

    /// <summary>
    ///     The classifier's hypothesis about the nature of the signal.
    /// </summary>
    [JsonPropertyName("hypothesis")]
    [Description("What do you think the signal is trying to indicate? What is the root cause for the signal.")]
    public string? Hypothesis { get; set; }

    /// <summary>
    ///     The recommended next step based on the signal classification.
    /// </summary>
    [JsonPropertyName("nextStep")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public NextStep NextStep { get; set; }

    /// <summary>
    ///     Original prompt/signal that was classified.
    /// </summary>
    [JsonPropertyName("origPrompt")]
    public string OrigPrompt { get; set; } = string.Empty;

    /// <summary>
    ///     Models justification for decisions made by the classifier.
    /// </summary>
    [JsonPropertyName("reasoning")]
    [Description("The reason behind your choice for next step")]
    public string? Reasoning { get; set; }

    /// <summary>
    ///     The affected subsystem or category identified by the classifier.
    /// </summary>
    [JsonPropertyName("subSystem")]
    public string? SubSystem { get; set; }
}





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
