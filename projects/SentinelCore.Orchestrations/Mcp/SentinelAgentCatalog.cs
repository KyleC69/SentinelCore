// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         SentinelAgentCatalog.cs
// Author: Kyle L. Crowder
// Build Num:  091112



using SentinelCore.Contracts.Mcp;
using SentinelCore.Orchestrations.Agents;




namespace SentinelCore.Orchestrations.Mcp;





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








    /// <inheritdoc />
    public Task<IReadOnlyList<string>> GetAgentNamesAsync(CancellationToken cancellationToken = default)
    {
        // Names are intentionally stable and match the names used when building profiles in
        // TheCoreWorkflow and the CoreChat agent. Add new entries here when new agent roles
        // or workflow agents are introduced.
        IReadOnlyList<string> names = new[] { "CoreChat", "Classifier", "TheCore", "SafetyAgent", "Manager", "Worker1", "Worker2", "Worker3", "CaseGenerator" };

        return Task.FromResult(names);
    }
}