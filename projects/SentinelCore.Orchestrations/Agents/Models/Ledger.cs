// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         Ledger.cs
// Author: Kyle L. Crowder
// Build Num:  082808



using System.Text.Json.Serialization;

using SentinelCore.Workflows;




namespace SentinelCore.Agents.Models;





public sealed class EvidenceItem
{

    [JsonPropertyName("condition")] public string? Condition { get; set; }

    [JsonPropertyName("confidenceImpact")] public double? ConfidenceImpact { get; set; }

    [JsonPropertyName("property")] public string Property { get; set; } = string.Empty;

    [JsonPropertyName("source")] public string Source { get; set; } = string.Empty;

    [JsonPropertyName("value")] public object Value { get; set; } = default!;
}





public sealed class InvestigationStep
{

    [JsonPropertyName("action")] public string Action { get; set; } = string.Empty;

    [JsonPropertyName("agent")] public string Agent { get; set; } = string.Empty;

    [JsonPropertyName("confidenceDelta")] public double? ConfidenceDelta { get; set; }

    [JsonPropertyName("evidence")] public List<EvidenceItem>? Evidence { get; set; }

    [JsonPropertyName("input")] public object? Input { get; set; }

    [JsonPropertyName("output")] public object? Output { get; set; }

    [JsonPropertyName("timestamp")] public DateTime Timestamp { get; set; }
}





public sealed class InvestigationLedger
{

    [JsonPropertyName("hypothesis")] public SignalHypothesis Hypothesis { get; set; } = default!;

    [JsonPropertyName("signalId")] public string SignalId { get; set; } = string.Empty;

    [JsonPropertyName("steps")] public List<InvestigationStep> Steps { get; set; } = new();
}





public sealed class AgentCapabilities
{
    [JsonPropertyName("agentName")] public string AgentName { get; set; } = string.Empty;

    [JsonPropertyName("domains")] public List<string> Domains { get; set; } = new();

    [JsonPropertyName("supportedTypes")] public List<DirectiveType> SupportedTypes { get; set; } = new();

    [JsonPropertyName("tools")] public List<string> Tools { get; set; } = new();
}





public sealed class CoreDirective
{
    [JsonPropertyName("hypothesis")] public SignalHypothesis Hypothesis { get; set; } = default!;

    [JsonPropertyName("intent")] public DirectiveIntent Intent { get; set; }

    [JsonPropertyName("notes")] public string? Notes { get; set; }

    [JsonPropertyName("scope")] public DirectiveScope Scope { get; set; }

    [JsonPropertyName("type")] public DirectiveType Type { get; set; }

    [JsonPropertyName("urgency")] public DirectiveUrgency Urgency { get; set; }
}





public enum DirectiveIntent
{
    ValidateHypothesis, GatherContext, ExecuteProcedure, SummarizeState
}





public enum DirectiveType
{
    Investigative, Procedural, Contextual
}





public enum DirectiveScope
{
    Narrow, Moderate, Broad
}





public enum DirectiveUrgency
{
    Routine, Elevated, Critical
}