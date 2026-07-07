// Solution: SentinelCoreLib
// Project:   SentinelCoreLib
// File:         ModelOptions.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



namespace SentinelCoreLib.Infrastructure;





/// <summary>
///     Configuration for a single model endpoint used by an agent.
/// </summary>
public sealed class ModelOptions
{
    /// <summary>
    ///     The Ollama model identifier (e.g. "kimi-k2.7-code:cloud").
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    ///     Sampling temperature for the model.
    /// </summary>
    public float Temperature { get; set; } = 0.1f;
}