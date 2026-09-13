// Solution: SentinelCore
// Project:   SentinelCore.UI
// File:         ModelConfigDocument.cs
// Author: Kyle L. Crowder
// Build Num:  091300



using System.Text.Json.Serialization;

using SentinelCore.Contracts.Contracts;




namespace SentinelCore.UI.Services;





/// <summary>
///     The persisted model configuration document: one <see cref="ModelProfile" />
///     per logical agent name, matching the cards on the Model Configuration page.
///     Agents with no entry are unconfigured and fail the factory gate.
/// </summary>
public sealed class ModelConfigDocument
{
    /// <summary>
    ///     Per-agent model profiles keyed by logical agent name.
    /// </summary>
    [JsonPropertyName("agentModels")]
    public IDictionary<string, ModelProfile> AgentModels { get; set; } = new Dictionary<string, ModelProfile>(StringComparer.OrdinalIgnoreCase);
}