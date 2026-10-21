// Solution: SentinelCore
// Project:   SentinelCore.Contracts
// File:         IMcpServerRegistry.cs
// Author: Kyle L. Crowder
// Build Num:  100908



using Microsoft.Extensions.AI;




namespace SentinelCore.Contracts.Mcp;





public interface IMcpServerRegistry
{



    int ToolCount { get; }

    /// <summary>
    ///     Occurs when the tool cache has been updated.
    /// </summary>
    event Action? ToolCacheChanged;

    /// <summary>
    ///     Occurs when the list of registered servers has changed (servers loaded, added, or removed).
    /// </summary>
    event Action? ServersChanged;








    /// <summary>
    ///     Gets a read-only snapshot of the current tool cache.
    /// </summary>
    /// <returns>A read-only list of available AITool instances from the cache.</returns>
    IReadOnlyList<AITool> GetToolsFromCache();








    /// <summary>
    ///     Asynchronously retrieves all registered AITool instances from connected servers.
    /// </summary>
    /// <param name="token">Cancellation token to cancel the operation.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains a read-only list of available AITool
    ///     instances.
    /// </returns>
    Task<IReadOnlyList<AITool>> GetAllToolsAsync(CancellationToken token);








    Task<McpServerInfo?> GetAsync(string id, CancellationToken cancellationToken = default);








    /// <summary>
    ///     Gets a read-only list of available McpServerInfo objects asynchronously.
    /// </summary>
    /// <remarks>
    ///     The returned list is a snapshot of server information at the time of the call and is not
    ///     updated thereafter.
    /// </remarks>
    /// <param name="cancellationToken">A CancellationToken to observe while waiting for the operation to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a read-only list of McpServerInfo.</returns>
    Task<IReadOnlyList<McpServerInfo>> ListAsync(CancellationToken cancellationToken = default);








    /// <summary>
    ///     Loads persisted definitions into the in-memory registry without starting connections.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that represents the asynchronous load operation.</returns>
    Task LoadPersistedAsync(CancellationToken cancellationToken = default);








    Task RegisterAsync(McpServerDefinition definition, CancellationToken cancellationToken = default);


    Task RemoveAsync(string id, CancellationToken cancellationToken = default);


    Task StartAsync(string id, CancellationToken cancellationToken = default);








    /// <summary>
    ///     Stops the MCP server identified by the specified id and disposes its client.
    /// </summary>
    /// <remarks>
    ///     Validates id, disposes the MCP client if present, updates the server status to Stopped, clears
    ///     tool names, and logs operations. Exceptions thrown while disposing the client are caught and logged.
    /// </remarks>
    /// <param name="id">Server identifier.</param>
    /// <param name="cancellationToken">Cancellation token to observe while stopping the server.</param>
    /// <returns>A task that represents the asynchronous stop operation.</returns>
    Task StopAsync(string id, CancellationToken cancellationToken = default);








    /// <summary>
    ///     Updates the assigned agent names for the specified MCP server and persists the change.
    /// </summary>
    /// <remarks>
    ///     Validates inputs, replaces the server definition with updated assignments, persists the registry,
    ///     and logs the update.
    /// </remarks>
    /// <param name="id">Identifier of the MCP server to update.</param>
    /// <param name="assignedAgentNames">New list of agent names to assign to the server.</param>
    /// <param name="cancellationToken">Token to observe while awaiting the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the specified MCP server is not registered.</exception>
    Task UpdateAssignmentsAsync(string id, IReadOnlyList<string> assignedAgentNames, CancellationToken cancellationToken = default);
}
