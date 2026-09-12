// Solution: SentinelCore
// Project:   SentinelCore.Contracts
// File:         IMcpServerRegistry.cs
// Author: Kyle L. Crowder
// Build Num:  091200



using Microsoft.Extensions.AI;




namespace SentinelCore.Contracts.Mcp;





/// <summary>
///     Provides add/remove/start/stop/query operations for managed MCP servers.
/// </summary>
public interface IMcpServerRegistry
{

    /// <summary>
    ///     Gets runtime information for a registered MCP server.
    /// </summary>
    /// <param name="id">Unique identifier of the server.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    ///     A task that resolves to the <see cref="McpServerInfo" /> if registered; otherwise <c>null</c>.
    /// </returns>
    Task<McpServerInfo?> GetAsync(string id, CancellationToken cancellationToken = default);








    /// <summary>
    ///     Gets the tools from connected MCP servers that are available to the specified agent.
    /// </summary>
    /// <param name="agentName">The logical agent name used to filter server assignments.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    ///     A task that resolves to a read-only list of <see cref="AITool" /> instances. A server is included
    ///     when it is connected and either has no assigned agent names or its assignments include
    ///     <paramref name="agentName" />.
    /// </returns>
    Task<IReadOnlyList<AITool>> GetToolsForAgentAsync(string agentName, CancellationToken cancellationToken = default);








    /// <summary>
    ///     Lists all registered MCP servers.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that resolves to a read-only list of all registered server information.</returns>
    Task<IReadOnlyList<McpServerInfo>> ListAsync(CancellationToken cancellationToken = default);








    /// <summary>
    ///     Registers a new MCP server definition.
    /// </summary>
    /// <param name="definition">Server definition to register.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that represents the asynchronous registration operation.</returns>
    Task RegisterAsync(McpServerDefinition definition, CancellationToken cancellationToken = default);








    /// <summary>
    ///     Removes a registered MCP server and disconnects it if currently connected.
    /// </summary>
    /// <param name="id">Unique identifier of the server.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that represents the asynchronous removal operation.</returns>
    Task RemoveAsync(string id, CancellationToken cancellationToken = default);








    /// <summary>
    ///     Starts a registered MCP server and enumerates its tools.
    /// </summary>
    /// <param name="id">Unique identifier of the server.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that represents the asynchronous start operation.</returns>
    Task StartAsync(string id, CancellationToken cancellationToken = default);








    /// <summary>
    ///     Stops a connected MCP server.
    /// </summary>
    /// <param name="id">Unique identifier of the server.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that represents the asynchronous stop operation.</returns>
    Task StopAsync(string id, CancellationToken cancellationToken = default);








    /// <summary>
    ///     Replaces the per-agent assignments of a registered MCP server.
    /// </summary>
    /// <param name="id">Unique identifier of the server.</param>
    /// <param name="assignedAgentNames">
    ///     The logical agent names allowed to use the server. An empty collection makes the
    ///     server available to all agents.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that represents the asynchronous update operation.</returns>
    /// <exception cref="ArgumentException"><paramref name="id" /> is null or whitespace.</exception>
    /// <exception cref="InvalidOperationException">The server is not registered.</exception>
    Task UpdateAssignmentsAsync(string id, IReadOnlyList<string> assignedAgentNames, CancellationToken cancellationToken = default);
}