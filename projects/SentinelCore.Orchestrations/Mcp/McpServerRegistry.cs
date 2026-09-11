// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         McpServerRegistry.cs
// Author: Kyle L. Crowder
// Build Num:  091112



using System.Collections.Concurrent;

using Microsoft.Extensions.Logging;

using ModelContextProtocol.Client;

using SentinelCore.Contracts.Mcp;




namespace SentinelCore.Orchestrations.Mcp;





/// <summary>
///     In-memory registry of MCP servers with persistence backed by an
///     <see cref="IMcpServerRegistryStore" />.
/// </summary>
public sealed class McpServerRegistry : IMcpServerRegistry
{
    private readonly IMcpConnectionFactory _connectionFactory;
    private readonly ConcurrentDictionary<string, McpServerEntry> _entries = new();
    private readonly ILogger<McpServerRegistry> _logger;
    private readonly IMcpServerRegistryStore _store;








    /// <summary>
    ///     Initializes a new instance of the <see cref="McpServerRegistry" /> class.
    /// </summary>
    /// <param name="store">The persistence store for server definitions.</param>
    /// <param name="connectionFactory">The factory used to establish MCP connections.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    public McpServerRegistry(IMcpServerRegistryStore store, IMcpConnectionFactory connectionFactory, ILogger<McpServerRegistry> logger)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(connectionFactory);
        ArgumentNullException.ThrowIfNull(logger);

        _store = store;
        _connectionFactory = connectionFactory;
        _logger = logger;
    }








    /// <inheritdoc />
    public Task<McpServerInfo?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        if (_entries.TryGetValue(id, out McpServerEntry? entry))
        {
            return Task.FromResult<McpServerInfo?>(entry.ToInfo());
        }

        return Task.FromResult<McpServerInfo?>(null);
    }








    /// <inheritdoc />
    public async Task<IReadOnlyList<AITool>> GetToolsForAgentAsync(string agentName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentName);

        List<AITool> tools = [];

        foreach (McpServerEntry entry in _entries.Values)
        {
            if (entry.Status != McpServerStatus.Connected || entry.Client is null)
            {
                continue;
            }

            bool assigned = entry.Definition.AssignedAgentNames.Count == 0 || entry.Definition.AssignedAgentNames.Contains(agentName, StringComparer.OrdinalIgnoreCase);

            if (!assigned)
            {
                continue;
            }

            try
            {
                IList<McpClientTool> serverTools = await entry.Client.ListToolsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

                foreach (McpClientTool serverTool in serverTools)
                {
                    tools.Add(serverTool);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to list tools from MCP server {ServerId} for agent {AgentName}.", entry.Definition.Id, agentName);
            }
        }

        return tools.AsReadOnly();
    }








    /// <inheritdoc />
    public Task<IReadOnlyList<McpServerInfo>> ListAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<McpServerInfo> infos = _entries.Values.Select(e => e.ToInfo()).ToList().AsReadOnly();

        return Task.FromResult(infos);
    }








    /// <inheritdoc />
    public async Task RegisterAsync(McpServerDefinition definition, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (string.IsNullOrWhiteSpace(definition.Id))
        {
            throw new ArgumentException("MCP server definition must have an Id.", nameof(definition));
        }

        _entries[definition.Id] = new McpServerEntry(definition);
        await PersistAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Registered MCP server {ServerId} ({DisplayName}).", definition.Id, definition.DisplayName);
    }








    /// <inheritdoc />
    public async Task RemoveAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        if (_entries.TryRemove(id, out McpServerEntry? entry))
        {
            await StopAsync(id, cancellationToken).ConfigureAwait(false);
            await PersistAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Removed MCP server {ServerId}.", id);
        }
    }








    /// <inheritdoc />
    public async Task StartAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        if (!_entries.TryGetValue(id, out McpServerEntry? entry))
        {
            throw new InvalidOperationException($"MCP server '{id}' is not registered.");
        }

        if (entry.Status is not (McpServerStatus.Stopped or McpServerStatus.Error))
        {
            return;
        }

        entry.Status = McpServerStatus.Starting;
        entry.LastError = null;

        try
        {
            McpClient client = await _connectionFactory.ConnectAsync(entry.Definition, cancellationToken).ConfigureAwait(false);

            IList<McpClientTool> tools = await client.ListToolsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

            entry.Client = client;
            entry.ToolNames = tools.Select(t => t.Name).ToList();
            entry.Status = McpServerStatus.Connected;

            _logger.LogInformation("MCP server {ServerId} connected with {ToolCount} tool(s).", id, entry.ToolNames.Count);
        }
        catch (Exception ex)
        {
            entry.Status = McpServerStatus.Error;
            entry.LastError = ex.Message;
            _logger.LogError(ex, "Failed to start MCP server {ServerId}.", id);
        }
    }








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
    public async Task StopAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        if (!_entries.TryGetValue(id, out McpServerEntry? entry))
        {
            return;
        }

        McpClient? client = entry.Client;
        entry.Client = null;

        if (client is not null)
        {
            try
            {
                await client.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing MCP client for server {ServerId}.", id);
            }
        }

        entry.Status = McpServerStatus.Stopped;
        entry.ToolNames = Array.Empty<string>();
        _logger.LogInformation("Stopped MCP server {ServerId}.", id);
    }








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
    public async Task UpdateAssignmentsAsync(string id, IReadOnlyList<string> assignedAgentNames, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(assignedAgentNames);

        if (!_entries.TryGetValue(id, out McpServerEntry? entry))
        {
            throw new InvalidOperationException($"MCP server '{id}' is not registered.");
        }

        McpServerDefinition current = entry.Definition;
        McpServerDefinition updated = new(current.Id, current.DisplayName, current.TransportType, current.CommandOrEndpoint, current.Arguments, current.WorkingDirectory, current.EnvironmentVariables, current.OAuthSettings, assignedAgentNames);
        _entries[id] = new McpServerEntry(updated) { Status = entry.Status, ToolNames = entry.ToolNames, LastError = entry.LastError, Client = entry.Client };

        await PersistAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Updated agent assignments for MCP server {ServerId}.", id);
    }








    /// <summary>
    ///     Loads persisted definitions into the in-memory registry without starting connections.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that represents the asynchronous load operation.</returns>
    public async Task LoadPersistedAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<McpServerDefinition> definitions = await _store.LoadAsync(cancellationToken).ConfigureAwait(false);

        foreach (McpServerDefinition definition in definitions)
        {
            _entries[definition.Id] = new McpServerEntry(definition);
        }

        _logger.LogInformation("Loaded {Count} persisted MCP server definition(s).", definitions.Count);
    }








    private async Task PersistAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<McpServerDefinition> definitions = _entries.Values.Select(e => e.Definition).ToList();

        await _store.SaveAsync(definitions, cancellationToken).ConfigureAwait(false);
    }








    private sealed class McpServerEntry
    {
        private McpClient? _client;








        public McpServerEntry(McpServerDefinition definition)
        {
            Definition = definition;
            Status = McpServerStatus.Stopped;
            ToolNames = Array.Empty<string>();
        }








        public McpClient? Client
        {
            get => _client;
            set => _client = value;
        }

        public McpServerDefinition Definition { get; }
        public string? LastError { get; set; }
        public McpServerStatus Status { get; set; }
        public IReadOnlyList<string> ToolNames { get; set; }








        public McpServerInfo ToInfo()
        {
            return new McpServerInfo(Definition, Status, ToolNames, LastError);
        }
    }
}