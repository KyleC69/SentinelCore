// Solution: SentinelCore
// Project:   SentinelCore.Contracts
// File:         SentinelCoreSettings.cs
// Author: Kyle L. Crowder
// Build Num:  082808



using Microsoft.Extensions.Logging;




namespace SentinelCore.Contracts;





/// <summary>
///     All configurable options exposed to the UI and passed to <c>AddSentinelCore</c>
///     to initialize the library's internal runtime options.
/// </summary>
public sealed class SentinelCoreSettings
{
    /// <summary>
    ///     Per-agent model profiles keyed by logical agent name (e.g. "TheCore",
    ///     "Manager", "Worker1"). The Model Configuration page owns this map — an
    ///     agent with no entry here falls back to its role tier, and an agent with
    ///     no configuration anywhere fails the factory gate with a descriptive error.
    /// </summary>
    public IDictionary<string, ModelProfile> AgentModels { get; set; } =
        new Dictionary<string, ModelProfile>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     Default model options used when no specialized model is configured.
    /// </summary>
    public ModelProfile? DefaultModel { get; set; }

    /// <summary>
    ///     Model for the Magnetic Orchestration Manager agent. When <c>null</c>, the
    ///     Manager falls back to <see cref="DefaultModel" />.
    /// </summary>
    public ModelProfile? ManagerModel { get; set; }

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
