// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         SentinelAgentCatalog.cs
// Author: Kyle L. Crowder
// Build Num:  091300



using SentinelCore.Contracts.Mcp;




namespace SentinelCore.Orchestrations.Agents;





/// <summary>
///     Provides the set of logical agent names used by the SentinelCore orchestration layer.
///     These names match the values passed to <see cref="SentinelAgentFactory" />
///     via <see cref="AgentProfile.AgentName" />.
/// </summary>
public sealed class SentinelAgentCatalog : ISentinelAgentCatalog
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SentinelAgentCatalog" /> class.
    /// </summary>
    public SentinelAgentCatalog()
    {
    }








    /// <summary>
    ///     Represents the built-in AI Agents in the application.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<IReadOnlyList<string>> GetAgentNamesAsync(CancellationToken cancellationToken = default)
    {
        // Names are intentionally stable and match the names used when building profiles in
        // TheCoreWorkflow and the TheCore agent. Add new entries here when new agents
        // or workflow agents are introduced.
        IReadOnlyList<string> names = new[] { "TheCore", "Manager", "Worker1", "Worker2", "Worker3", "Classifier", "SafetyAgent" };

        return Task.FromResult(names);
    }
}