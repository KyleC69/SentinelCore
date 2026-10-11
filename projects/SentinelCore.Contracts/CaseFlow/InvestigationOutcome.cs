// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         InvestigationOutcome.cs
// Author: Kyle L. Crowder
// Build Num:  101009



using System.ComponentModel;
using System.Text.Json.Serialization;

using SentinelCore.Contracts.CaseFlow;




namespace SentinelCore.Orchestrations.Agents.Core;





public sealed class InvestigationOutcome
{

    [JsonPropertyName("confidence")]
    [Description("Confidence score (0.0 to 1.0) in the accuracy of the outcome.")]
    public double Confidence { get; init; }

    [JsonPropertyName("impact")]
    [Description("The assessed impact of the root cause on the system.")]
    public string Impact { get; init; }

    [JsonPropertyName("case")]
    [Description("The investigation case that produced this outcome.")]
    public Case Case { get; init; }

    [JsonPropertyName("remediation")]
    [Description("Suggested remediation steps to prevent recurrence or to resolve.")]
    public List<string> Remediation { get; set; }

    [JsonPropertyName("resolution")]
    [Description("The final resolution or conclusion of the investigation.")]
    public string Resolution { get; set; }

    [JsonPropertyName("rootCause")]
    [Description("The determined root cause of the signal.")]
    public string RootCause { get; set; }

    [JsonPropertyName("signal")]
    [Description("The original signal that was investigated.")]
    public Signal Signal { get; set; }

    [JsonPropertyName("caseEvidence")]
    [Description("Key factors and evidence that support this outcome.")]
    public List<EvidenceEntity> CaseEvidence { get; set; }
}