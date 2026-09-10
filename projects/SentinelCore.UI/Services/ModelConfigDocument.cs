// Solution: SentinelCore
// Project:   SentinelCore.UI
// File:         ModelConfigDocument.cs
// Author: Kyle L. Crowler
// Build Num:  091003



using System.Text.Json.Serialization;

using SentinelCore.Contracts;





namespace SentinelCore.UI.Services;





/// <summary>
///     The persisted model configuration document. One <see cref="ModelProfile" />
///     per configuration slot; <c>null</c> slots fall back at runtime
///     (MagManager → TheCore, Utility → engine defaults).
/// </summary>
public sealed class ModelConfigDocument
{
    /// <summary>
    ///     The model profile for the TheCore reasoning agent slot.
    /// </summary>
    [JsonPropertyName("theCore")]
    public ModelProfile? TheCore { get; set; }

    /// <summary>
    ///     The model profile for the Magnetic Orchestration Manager slot.
    /// </summary>
    [JsonPropertyName("magManager")]
    public ModelProfile? MagManager { get; set; }

    /// <summary>
    ///     The model profile for the Utility catch-all slot (all remaining agents).
    /// </summary>
    [JsonPropertyName("utility")]
    public ModelProfile? Utility { get; set; }
}