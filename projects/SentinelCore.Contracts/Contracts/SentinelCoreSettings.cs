// Solution: SentinelCore
// Project:   SentinelCore.Contracts
// File:         SentinelCoreSettings.cs
// Author: Kyle L. Crowder
// Build Num:  081312



using Microsoft.Extensions.Logging;




namespace SentinelCore.Contracts;





/// <summary>
///     All configurable options exposed to the UI and passed to <c>AddSentinelCore</c>
///     to initialize the library's internal runtime options.
/// </summary>
public sealed class SentinelCoreSettings
{
    /// <summary>
    ///     Model options for the Core investigative agent.
    /// </summary>
    /// <summary>
    ///     Default model options used when no specialized model is configured.
    /// </summary>
    public ModelProfile? DefaultModel { get; set; }

    /// <summary>
    ///     Default utility model options used when no specialized utility model is configured.
    /// </summary>
    public ModelProfile? DefaultUtilityModel { get; set; }

    /// <summary>
    ///     The orchestration pattern used to coordinate agents.
    /// </summary>
    public OrchestrationType OrchestrationType { get; set; }

    /// <summary>
    ///     Directory containing skill definitions. Deprecated in the new implementation;
    ///     skills are now strongly-typed configuration classes.
    /// </summary>
    public string SkillsDirectory { get; set; } = string.Empty;

    /// <summary>
    ///     SQL Server connection string for persistence.
    /// </summary>
    public string? SqlConnectionString { get; set; }

    /// <summary>
    ///     Enables agent trace logging.
    /// </summary>
    public bool TraceEnabled { get; set; }

    /// <summary>
    ///     Minimum log level emitted when agent tracing is enabled.
    /// </summary>
    public LogLevel TraceLogLevel { get; set; } = LogLevel.Trace;
}