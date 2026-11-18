// Solution: SentinelCore
// Project:   SentinelCore.UI
// File:         IModelConfigGate.cs
// Author: Kyle L. Crowder
// Build Num:  111504



using System.Net.Sockets;

using SentinelCore.Contracts.Contracts;
using SentinelCore.Contracts.Mcp;
using SentinelCore.Orchestrations.Abstractions;
using SentinelCore.Orchestrations.Agents;




namespace SentinelCore.UI.Services;





/// <summary>
///     Evaluates whether every catalog agent has a usable model configuration.
///     UI features that build agents gate on this (e.g. chat send) and surface
///     the gate message when configuration is incomplete.
/// </summary>
public interface IModelConfigGate
{
    /// <summary>
    ///     Gets a value indicating whether every catalog agent resolves a model
    ///     profile (per-agent entry or role tier).
    /// </summary>
    bool IsConfigurationComplete { get; }

    /// <summary>
    ///     Gets a value indicating whether the Ollama service is reachable and responding.
    /// </summary>
    bool IsOllamaAvailable { get; }








    /// <summary>
    ///     Gets a value indicating whether every registered MCP server is connected.
    /// </summary>
    /// <remarks>
    ///     A disconnected server leaves the agent toolsets incomplete, so this blocks agent execution the
    ///     same way an unreachable Ollama service does. It is never terminal: repairing the server clears
    ///     the gate on the next evaluation.
    /// </remarks>
    bool AreMcpServersHealthy => true;

    /// <summary>
    ///     Gets the display names of the MCP servers that are not connected.
    /// </summary>
    IReadOnlyList<string> UnhealthyMcpServers => [];

    /// <summary>
    ///     Returns the non-blocking readiness warnings, currently the catalogue tools that no tool domain
    ///     claims.
    /// </summary>
    /// <remarks>
    ///     These are surfaced in the banner but never disable an action. An unclaimed tool is still reachable
    ///     through <see cref="AgentToolSelection.All" /> and by explicit name, so losing it is impossible; the
    ///     warning exists so the vocabulary gets extended instead of silently drifting.
    /// </remarks>
    /// <returns>The warning messages, or an empty list when there is nothing to report.</returns>
    IReadOnlyList<string> GetReadinessWarnings() => [];

    /// <summary>
    ///     Builds a user-facing message describing the missing configuration,
    ///     or an empty string when configuration is complete.
    /// </summary>
    /// <returns>A message listing the unconfigured agents, or empty when complete.</returns>
    string BuildGateMessage();
}





/// <summary>
///     Default <see cref="IModelConfigGate" /> that evaluates the agent presets
///     against <see cref="SentinelCoreSettings.AgentModels" /> and the role tiers
/// </summary>
public sealed class ModelConfigGate : IModelConfigGate
{

    private readonly IModelResolver _modelResolver;
    private readonly IMcpServerRegistry? _mcpServerRegistry;

    // Removed unused settings field as it was never referenced.








    /// <summary>
    ///     Creates a gate over the catalog agents and the current settings.
    /// </summary>
    /// <param name="agentCatalog">The catalog of logical agent names.</param>
    /// <param name="modelResolver">Resolver used to determine model configuration for each agent.</param>
    /// <param name="mcpServerRegistry">
    ///     Optional MCP registry. When supplied, a disconnected server blocks agent execution; when absent the
    ///     gate degrades to the model-only check.
    /// </param>
    public ModelConfigGate(IModelResolver modelResolver, IMcpServerRegistry? mcpServerRegistry = null)
    {
        _modelResolver = modelResolver ?? throw new ArgumentNullException(nameof(modelResolver));
        _mcpServerRegistry = mcpServerRegistry;
    }








    /// <summary>
    ///     Gets the display names of the MCP servers that are not connected.
    /// </summary>
    public IReadOnlyList<string> UnhealthyMcpServers
    {
        get
        {
            if (_mcpServerRegistry is null)
            {
                return [];
            }

            return
            [
                    .. _mcpServerRegistry.GetServersSnapshot()
                            .Where(server => server.Status is not McpServerStatus.Connected)
                            .Select(server => server.Definition.DisplayName)
            ];
        }
    }








    /// <summary>
    ///     Gets a value indicating whether every registered MCP server is connected.
    /// </summary>
    public bool AreMcpServersHealthy
    {
        get => UnhealthyMcpServers.Count == 0;
    }








    /// <summary>
    ///     Returns the non-blocking readiness warnings.
    /// </summary>
    /// <returns>The warning messages, or an empty list when there is nothing to report.</returns>
    public IReadOnlyList<string> GetReadinessWarnings()
    {
        if (_mcpServerRegistry is null)
        {
            return [];
        }

        IReadOnlyList<string> unclassified = AgentToolDomainCatalog.Unclassified(_mcpServerRegistry.GetToolsFromCache().Select(tool => tool.Name));

        if (unclassified.Count == 0)
        {
            return [];
        }

        return
        [
                $"{unclassified.Count} MCP tool(s) are not claimed by any tool domain and can only be reached by name or through the All selection: {string.Join(", ", unclassified)}."
        ];
    }








    /// <summary>
    ///     Gets the names of the catalog agents that have no model configuration.
    /// </summary>
    public IReadOnlyList<string>? UnconfiguredAgents { get; }








    /// <summary>
    ///     Builds the blocking banner message: the Ollama failure first, then the disconnected MCP servers,
    ///     then incomplete model configuration.
    /// </summary>
    /// <returns>The message to display, or an empty string when every blocking condition is satisfied.</returns>
    public string BuildGateMessage()
    {
        if (!IsOllamaAvailable)
        {
            return "Ollama service is not available. Please ensure Ollama is running and accessible at the configured endpoint.";
        }

        IReadOnlyList<string> unhealthy = UnhealthyMcpServers;

        if (unhealthy.Count > 0)
        {
            return "These MCP servers are not connected, so the agent toolsets are incomplete: " + $"{string.Join(", ", unhealthy)}. Start or repair them and retry; no agent action can run until they are reachable.";
        }

        IReadOnlyList<string> missing = new List<string>();
        missing = UnconfiguredAgents ?? new List<string>();

        if (missing.Count == 0)
        {
            return string.Empty;
        }

        return "Model configuration is incomplete. Open the Model Configuration page and set a " + $"provider, model, and endpoint for: {string.Join(", ", missing)}.";
    }








    /// <summary>
    ///     Gets a value indicating whether agent execution may proceed: the model service is reachable and
    ///     every registered MCP server is connected.
    /// </summary>
    public bool IsConfigurationComplete
    {
        get => IsOllamaAvailable && AreMcpServersHealthy;
    }

    /// <summary>
    ///     Gets a value indicating whether the Ollama service accepts a TCP connection on the configured host.
    /// </summary>
    public bool IsOllamaAvailable
    {
        get
        {
            try
            {
                string ollamaHost = Environment.GetEnvironmentVariable("OLLAMA_HOST", EnvironmentVariableTarget.User) ?? "http://127.0.0.1:11434";
                Uri ollamaUri = new(ollamaHost);
                using TcpClient client = new();
                IAsyncResult result = client.BeginConnect(ollamaUri.Host, ollamaUri.Port, null, null);
                bool success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(2));
                return success;
            }
            catch
            {
                return false;
            }
        }
    }
}
