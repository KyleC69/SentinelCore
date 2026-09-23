// Solution: SentinelCore
// Project:   SentinelCore.Contracts
// File:         ISentinelAgentCatalog.cs
// Author: Kyle L. Crowder
// Build Num:  092308



namespace SentinelCore.Contracts.Mcp;





/// <summary>
///     Exposes the set of logical agent names known to the application so the
///     MCP server registry can present per-agent assignment selectors.
/// </summary>
public interface ISentinelAgentCatalog
{
    /// <summary>
    ///     Gets the display names of all logical agents that can be assigned MCP servers.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    ///     A task that resolves to a read-only list of logical agent names.
    ///     The names must match the names used by the agent factory.
    /// </returns>
    Task<IReadOnlyList<string>> GetAgentNamesAsync(CancellationToken cancellationToken = default);
}