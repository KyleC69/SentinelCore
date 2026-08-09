// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         SignalHypothesis.cs
// Author: Kyle L. Crowder
// Build Num:  080801



using System.Text.Json.Serialization;

using Newtonsoft.Json;




namespace SentinelCore.Workflows;





public sealed class SignalHypothesis
{
    [JsonPropertyName("category")] public string? Category { get; set; }

    [JsonPropertyName("hypothesis")] public string? Hypothesis { get; set; }

    [JsonPropertyName("initialConfidenceScore")]
    public double InitialConfidenceScore { get; set; }

    [JsonPropertyName("nextStep")]
    [System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter))]
    public NextStep NextStep { get; set; }

    /// <summary>
    ///     Original prompt/signal
    /// </summary>
    [JsonProperty("ogprompt")]
    public string OGPrompt { get; set; } = string.Empty;

    /// <summary>
    ///     Models justification for decisions.
    /// </summary>
    [JsonPropertyName("reasoning")]
    public string? Reasoning { get; set; }
}
