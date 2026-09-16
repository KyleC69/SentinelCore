// Solution: SentinelCore
// Project:   SentinelCore.Tests
// File:         FakeMcpServerRegistry.cs
// Author: Kyle L. Crowder
// Build Num:  091418



using SentinelCore.Contracts.Mcp;




namespace SentinelCore.Tests.TestInfrastructure;





/// <summary>
///     A no-op <see cref="IMcpServerRegistry" /> suitable for test scenarios that do not exercise
///     MCP server tooling.
/// </summary>
public sealed class FakeMcpServerRegistry : IMcpServerRegistry
{

    /// <inheritdoc />
    public Task<McpServerInfo?> GetAsync(string serverId, CancellationToken cancellationToken = default) =>
            Task.FromResult<McpServerInfo?>(null);








    /// <inheritdoc />
    public Task<IReadOnlyList<AITool>> GetToolsForAgentAsync(string agentName, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AITool>>([]);








    /// <inheritdoc />
    public Task<IReadOnlyList<McpServerInfo>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<McpServerInfo>>([]);








    /// <inheritdoc />
    public Task RegisterAsync(McpServerDefinition definition, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;








    /// <inheritdoc />
    public Task RemoveAsync(string serverId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;








    /// <inheritdoc />
    public Task StartAsync(string serverId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;








    /// <inheritdoc />
    public Task StopAsync(string serverId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;








    /// <inheritdoc />
    public Task UpdateAssignmentsAsync(string serverId, IReadOnlyList<string> assignedAgentNames, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
}