// Solution: SentinelCore
// Project:   SentinelCore.UI
// File:         IModelConfigGate.cs
// Author: Kyle L. Crowder
// Build Num:  100310



using System.Net.Sockets;

using SentinelCore.Contracts.Contracts;
using SentinelCore.Contracts.Mcp;
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
    ///     Gets the names of the catalog agents that have no model configuration.
    /// </summary>
    IReadOnlyList<string> UnconfiguredAgents { get; }








    /// <summary>
    ///     Builds a user-facing message describing the missing configuration,
    ///     or an empty string when configuration is complete.
    /// </summary>
    /// <returns>A message listing the unconfigured agents, or empty when complete.</returns>
    string BuildGateMessage();
}





/// <summary>
///     Default <see cref="IModelConfigGate" /> that evaluates the catalog agents
///     against <see cref="SentinelCoreSettings.AgentModels" /> and the role tiers
///     via <see cref="IAgentProfileBuilder.TryGetModel" />.
/// </summary>
public sealed class ModelConfigGate : IModelConfigGate
{

    private readonly ISentinelAgentCatalog _agentCatalog;

    private readonly SentinelCoreSettings _settings;








    /// <summary>
    ///     Creates a gate over the catalog agents and the current settings.
    /// </summary>
    /// <param name="agentCatalog">The catalog of logical agent names.</param>
    /// <param name="profileBuilder">The profile builder used to resolve each agent's model.</param>
    /// <param name="settings">The live SentinelCore settings.</param>
    public ModelConfigGate(ISentinelAgentCatalog agentCatalog, IAgentProfileBuilder profileBuilder, SentinelCoreSettings settings)
    {
        ArgumentNullException.ThrowIfNull(profileBuilder);
        _agentCatalog = agentCatalog ?? throw new ArgumentNullException(nameof(agentCatalog));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }








    public string BuildGateMessage()
    {
        if (!IsOllamaAvailable)
        {
            return "Ollama service is not available. Please ensure Ollama is running and accessible at the configured endpoint.";
        }

        IReadOnlyList<string> missing = UnconfiguredAgents;

        if (missing.Count == 0)
        {
            return string.Empty;
        }

        return "Model configuration is incomplete. Open the Model Configuration page and set a " + $"provider, model, and endpoint for: {string.Join(", ", missing)}.";
    }








    public bool IsConfigurationComplete
    {
        get => IsOllamaAvailable && UnconfiguredAgents.Count == 0;
    }

    public bool IsOllamaAvailable
    {
        get
        {
            try
            {
                using TcpClient client = new();
                IAsyncResult result = client.BeginConnect("127.0.0.1", 11434, null, null);
                bool success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(2));
                return success;
            }
            catch
            {
                return false;
            }
        }
    }


    public IReadOnlyList<string> UnconfiguredAgents
    {
        get
        {
            List<string> missing = [];

            foreach (string agentName in _agentCatalog.GetAgentNamesAsync().GetAwaiter().GetResult())
                if (!_settings.AgentModels.ContainsKey(agentName))
                {
                    missing.Add(agentName);
                }

            return missing;
        }
    }
}