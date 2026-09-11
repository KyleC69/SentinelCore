// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         IMcpConnectionFactory.cs
// Author: Kyle L. Crowder
// Build Num:  091112



using ModelContextProtocol.Client;

using SentinelCore.Contracts.Mcp;




namespace SentinelCore.Orchestrations.Mcp;





/// <summary>
///     Creates and configures <see cref="McpClient" /> instances from a <see cref="McpServerDefinition" />.
/// </summary>
public interface IMcpConnectionFactory
{
    /// <summary>
    ///     Creates a connected <see cref="McpClient" /> for the supplied server definition.
    /// </summary>
    /// <param name="definition">The server definition describing transport, command, endpoint, and OAuth settings.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    ///     A task that resolves to a connected <see cref="McpClient" /> instance.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="definition" /> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">The transport type is unsupported.</exception>
    Task<McpClient> ConnectAsync(McpServerDefinition definition, CancellationToken cancellationToken = default);
}