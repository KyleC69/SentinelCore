// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         SignalHypothesis.cs
// Author: Kyle L. Crowder
// Build Num:  091300



using System.Text.Json.Serialization;




namespace SentinelCore.Orchestrations.Workflows;





/// <summary>
///     Represents a signal classification hypothesis used by the classifier agent to determine
///     the appropriate next step in the workflow.
/// </summary>
public sealed class SignalHypothesis
{
    /// <summary>
    ///     The affected subsystem or category identified by the classifier.
    /// </summary>
    [JsonPropertyName("category")]
    public string? Category { get; set; }

    /// <summary>
    ///     The classifier's hypothesis about the nature of the signal.
    /// </summary>
    [JsonPropertyName("hypothesis")]
    public string? Hypothesis { get; set; }

    /// <summary>
    ///     Confidence score between 0.0 and 1.0 indicating the classifier's certainty.
    /// </summary>
    [JsonPropertyName("initialConfidenceScore")]
    public double InitialConfidenceScore { get; set; }

    /// <summary>
    ///     The recommended next step based on the signal classification.
    /// </summary>
    [JsonPropertyName("nextStep")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public NextStep NextStep { get; set; }

    /// <summary>
    ///     Original prompt/signal that was classified.
    /// </summary>
    [JsonPropertyName("ogprompt")]
    public string OGPrompt { get; set; } = string.Empty;

    /// <summary>
    ///     Models justification for decisions made by the classifier.
    /// </summary>
    [JsonPropertyName("reasoning")]
    public string? Reasoning { get; set; }
}