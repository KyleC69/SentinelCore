// Solution: SentinelCoreLib
// Project:   SentinelCoreLib
// File:         SentinelCoreSettings.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



using Microsoft.Extensions.Logging;




namespace SentinelCore.Contracts;





/// <summary>
///     All configurable options exposed to the UI and passed to <c>AddSentinelCore</c>
///     to initialize the library's internal runtime options.
/// </summary>
public sealed class SentinelCoreSettings
{

    /// <summary>
    ///     Enables agent trace logging.
    /// </summary>
    public bool AgentTraceEnabled { get; set; }

    /// <summary>
    ///     Minimum log level emitted when agent tracing is enabled.
    /// </summary>
    public LogLevel AgentTraceLogLevel { get; set; } = LogLevel.Trace;

    /// <summary>
    ///     Model options for the Core investigative agent.
    /// </summary>
    public ModelSettings CoreModel { get; set; } = new();

    /// <summary>
    ///     Model options for domain and dynamic agents.
    /// </summary>
    public ModelSettings DomainModel { get; set; } = new();

    /// <summary>
    ///     Model options for the Manager orchestration agent.
    /// </summary>
    public ModelSettings ManagerModel { get; set; } = new();

    /// <summary>
    ///     Directory containing skill definitions. Deprecated in the new implementation;
    ///     skills are now strongly-typed configuration classes.
    /// </summary>
    public string SkillsDirectory { get; set; } = string.Empty;

    /// <summary>
    ///     SQL Server connection string for persistence.
    /// </summary>
    public string? SqlConnectionString { get; set; }
}





/// <summary>
///     Configuration for a single model endpoint used by an agent.
/// </summary>
public sealed class ModelSettings
{

    /// <summary>
    ///     Ollama endpoint
    /// </summary>
    public string Endpoint { get; set; } = "http://127.0.0.1:11434";

    /// <summary>
    ///     The Ollama model identifier (e.g. "llama3.2").
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    ///     Sampling temperature for the model.
    /// </summary>
    public float Temperature { get; set; } = 0.1f;
}