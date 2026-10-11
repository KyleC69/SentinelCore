// Solution: SentinelCore
// Project:   SentinelCore.UI
// File:         ModelConfigDocument.cs
// Author: Kyle L. Crowder
// Build Num:  100908



using System.Text.Json.Serialization;

using SentinelCore.Contracts.Contracts;
using SentinelCore.Orchestrations.Agents.AgentPresets;




namespace SentinelCore.UI.Services;




/// <summary>
/// Represents a configuration document for models, containing mappings
/// between model tiers and their respective profiles.
/// </summary>
/// <remarks>
/// This class is used to store and manage model configuration data, 
/// including details about model tiers and their associated profiles.
/// It is primarily utilized by services that handle model configuration
/// persistence and retrieval.
/// </remarks>
public sealed class ModelConfigDocument
{

    /// <summary>
    ///     Model profiles keyed by <see cref="ModelTier" />. This is the single
    ///     source of truth consulted by <see cref="SentinelCore.Orchestrations.Agents.IModelResolver" />
    ///     when constructing agents from an <see cref="AgentPresetDefinition" />.
    /// </summary>
    [JsonPropertyName("tierModels")]
    public IDictionary<ModelTier, ModelProfile> TierModels { get; set; } = new Dictionary<ModelTier, ModelProfile>();
}