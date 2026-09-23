// Solution: SentinelCore
// Project:   SentinelCore.Contracts
// File:         SentinelCoreSettings.cs
// Author: Kyle L. Crowder
// Build Num:  092308



using Microsoft.Extensions.Logging;




namespace SentinelCore.Contracts.Contracts;





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
    public IDictionary<string, ModelProfile> AgentModels { get; set; } = new Dictionary<string, ModelProfile>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     Default model options used when no specialized model is configured.
    /// </summary>
    public ModelProfile? DefaultModel { get; set; }

    /// <summary>
    ///     Default utility model options used when no specialized utility model is configured.
    /// </summary>
    public ModelProfile? DefaultUtilityModel { get; set; }

    /// <summary>
    ///     Model for the Magnetic Orchestration Manager agent. When <c>null</c>, the
    ///     Manager falls back to <see cref="DefaultModel" />.
    /// </summary>
    public ModelProfile? ManagerModel { get; set; }

    /// <summary>
    ///     The orchestration pattern used to coordinate agents.
    /// </summary>
    public OrchestrationType OrchestrationType { get; set; }

    /// <summary>
    ///     RAG (Retrieval-Augmented Generation) search configuration.
    ///     When null, RAG search is disabled.
    /// </summary>
    public RagSearchOptions? RagSearch { get; set; }

    /// <summary>
    ///     Safety engine configuration settings.
    /// </summary>
    public SafetyEngineSettings SafetyEngine { get; set; } = new();

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





/// <summary>
///     Configuration options for the Safety Engine.
/// </summary>
public sealed class SafetyEngineSettings
{

    /// <summary>
    ///     Custom message to return when a prompt is blocked.
    /// </summary>
    public string? BlockedResponseMessage { get; set; }

    /// <summary>
    ///     Custom blocklist regex patterns.
    /// </summary>
    public IReadOnlyList<string> CustomBlocklistPatterns { get; set; } = Array.Empty<string>();

    /// <summary>
    ///     Custom blocklist terms to use in addition to the default rules.
    /// </summary>
    public IReadOnlyList<string> CustomBlocklistTerms { get; set; } = Array.Empty<string>();

    /// <summary>
    ///     Whether to enable output sanitization.
    /// </summary>
    public bool EnableOutputSanitization { get; set; } = true;

    /// <summary>
    ///     Whether to enable rate limiting.
    /// </summary>
    public bool EnableRateLimiting { get; set; } = true;

    /// <summary>
    ///     Whether the safety engine is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    ///     Maximum requests per agent per minute (when rate limiting is enabled).
    /// </summary>
    public int MaxRequestsPerMinute { get; set; } = 100;

    /// <summary>
    ///     Whether to stop evaluating rules as soon as one returns High severity or higher.
    /// </summary>
    public bool StopOnFirstBlock { get; set; } = true;

    /// <summary>
    ///     Whether to treat rule evaluation errors as blocks (fail-safe).
    /// </summary>
    public bool TreatRuleErrorsAsBlocks { get; set; } = true;
}





/// <summary>
///     Configuration options for RAG (Retrieval-Augmented Generation) search functionality.
/// </summary>
public sealed class RagSearchOptions
{

    /// <summary>
    ///     Enables automatic context injection for relevant queries.
    /// </summary>
    public bool AutoInjectEnabled { get; set; } = true;

    /// <summary>
    ///     Whether to include the RAG context injector in the agent middleware pipeline.
    /// </summary>
    public bool ContextInjectorEnabled { get; set; } = true;

    /// <summary>
    ///     Default options instance with standard settings.
    /// </summary>
    public static RagSearchOptions Default { get; } = new();

    /// <summary>
    ///     Enables or disables RAG search functionality globally.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    ///     List of agent roles that have RAG search enabled.
    ///     When empty, RAG is available to all agents.
    ///     Example values: "TheCore", "Manager", "Worker1"
    /// </summary>
    public IReadOnlyList<string> EnabledForAgentRoles { get; set; } = Array.Empty<string>();

    /// <summary>
    ///     Maximum total context size in characters for auto-injection.
    /// </summary>
    public int MaxContextSize { get; set; } = 4000;

    /// <summary>
    ///     Maximum number of results to return from a search.
    /// </summary>
    public int MaxResults { get; set; } = 5;

    /// <summary>
    ///     Keywords that indicate a query is likely relevant to the knowledge base.
    ///     Used for auto-injection decision making.
    /// </summary>
    public IReadOnlyList<string> RelevanceKeywords { get; set; } = new[] { "knowledge", "documentation", "policy", "procedure", "guideline", "manual", "reference", "specification", "standard", "best practice" };

    /// <summary>
    ///     Minimum relevance score threshold (0.0 to 1.0) for auto-injection.
    ///     Results below this threshold will not be automatically injected.
    /// </summary>
    public double RelevanceThreshold { get; set; } = 0.5;

    /// <summary>
    ///     Whether the RAG tool (on-demand search) is enabled for agents.
    ///     When false, agents can only use auto-injected context.
    /// </summary>
    public bool ToolEnabled { get; set; } = true;

    /// <summary>
    ///     Indicates whether vector search is available (requires embedding provider).
    /// </summary>
    public bool VectorSearchEnabled { get; set; } = false;
}