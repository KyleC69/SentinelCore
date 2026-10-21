// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         McpServerRegistry.cs
// Author: Kyle L. Crowder
// Build Num:  100908



#nullable enable

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

using Microsoft.Extensions.Logging;

using ModelContextProtocol.Client;

using SentinelCore.Contracts.Mcp;




namespace SentinelCore.Orchestrations.Mcp;





/// <summary>
///     In-memory registry of MCP servers with persistence backed by an
///     <see cref="IMcpServerRegistryStore" />.
/// </summary>
public sealed class McpServerRegistry : IMcpServerRegistry, IDisposable, IAsyncDisposable
{
    private readonly IMcpConnectionFactory _connectionFactory;
    private readonly ConcurrentDictionary<string, McpServerEntry> _entries = new();
    private readonly IMcpServerRegistryStore _store;
    private readonly ILogger<McpServerRegistry> _logger;
    private readonly SemaphoreSlim _toolCacheLock = new(1, 1);
    private readonly List<AITool> _aiToolCache = new();
    private bool _disposed;

    /// <summary>
    ///     Occurs when the tool cache has been updated.
    /// </summary>
    public event Action? ToolCacheChanged;

    /// <summary>
    ///     Occurs when the list of registered servers has changed (servers loaded, added, or removed).
    /// </summary>
    public event Action? ServersChanged;








    /// <summary>
    ///     Initializes a new instance of the <see cref="McpServerRegistry" /> class.
    /// </summary>
    /// <param name="store">The persistence store for server definitions.</param>
    /// <param name="connectionFactory">The factory used to establish MCP connections.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public McpServerRegistry(IMcpServerRegistryStore store, IMcpConnectionFactory connectionFactory, ILogger<McpServerRegistry> logger)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }








    /// <inheritdoc />
    public IReadOnlyList<AITool> GetToolsFromCache()
    {
        lock (_aiToolCache)
        {
            return _aiToolCache.AsReadOnly();
        }
    }








    /// <inheritdoc />
    public async Task<IReadOnlyList<AITool>> GetAllToolsAsync(CancellationToken cancellationToken = default)
    {
        var tools = new List<AITool>();

        foreach (var entry in _entries.Values)
        {
            if (entry.Status != McpServerStatus.Connected || entry.Client is null)
            {
                continue;
            }

            try
            {
                var serverTools = await entry.Client.ListToolsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                tools.AddRange(serverTools);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to list tools from MCP server {ServerId}.", entry.Definition.Id);
            }
        }

        return tools.AsReadOnly();
    }








    /// <inheritdoc />
    public Task<McpServerInfo?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        if (_entries.TryGetValue(id, out var entry))
        {
            return Task.FromResult<McpServerInfo?>(entry.ToInfo());
        }

        return Task.FromResult<McpServerInfo?>(null);
    }








    /// <inheritdoc />
    public Task<IReadOnlyList<McpServerInfo>> ListAsync(CancellationToken cancellationToken = default)
    {
        var infos = _entries.Values.Select(e => e.ToInfo()).ToList();
        return Task.FromResult<IReadOnlyList<McpServerInfo>>(infos);
    }








    /// <inheritdoc />
    public int ToolCount
    {
        get
        {
            lock (_aiToolCache)
            {
                return _aiToolCache.Count;
            }
        }
    }








    /// <inheritdoc />
    public async Task LoadPersistedAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var definitions = await _store.LoadAsync(cancellationToken).ConfigureAwait(false);

            foreach (var definition in definitions)
            {
                ValidateDefinition(definition);
                _entries[definition.Id] = new McpServerEntry(definition);
            }

            _logger.LogInformation("Loaded {Count} persisted MCP server definition(s).", definitions.Count);
            _logger.LogDebug("Loaded {Count} persisted MCP server definition(s) into server entry cache.", _entries.Count);

            OnServersChanged();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load persisted MCP server definitions.");
            throw;
        }
    }








    /// <inheritdoc />
    public async Task RegisterAsync(McpServerDefinition definition, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(definition);
        ValidateDefinition(definition);

        if (_entries.ContainsKey(definition.Id))
        {
            throw new InvalidOperationException($"MCP server with ID '{definition.Id}' is already registered.");
        }

        _entries[definition.Id] = new McpServerEntry(definition);

        try
        {
            await PersistAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Registered MCP server {ServerId} ({DisplayName}).", definition.Id, definition.DisplayName);
            OnServersChanged();
        }
        catch (OperationCanceledException)
        {
            _entries.TryRemove(definition.Id, out _);
            throw;
        }
        catch (Exception ex)
        {
            _entries.TryRemove(definition.Id, out _);
            _logger.LogError(ex, "Failed to persist MCP server {ServerId} after registration.", definition.Id);
            throw;
        }
    }








    /// <inheritdoc />
    public async Task RemoveAsync(string id, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        if (_entries.TryRemove(id, out var entry))
        {
            try
            {
                await StopAsync(id, cancellationToken).ConfigureAwait(false);
                await PersistAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogInformation("Removed MCP server {ServerId}.", id);
                OnServersChanged();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove MCP server {ServerId}.", id);
                throw;
            }
        }
    }








    /// <inheritdoc />
    public async Task StartAsync(string id, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        if (!_entries.TryGetValue(id, out var entry))
        {
            throw new InvalidOperationException($"MCP server '{id}' is not registered.");
        }

        if (entry.Status is not (McpServerStatus.Stopped or McpServerStatus.Error))
        {
            _logger.LogDebug("MCP server {ServerId} is already in status {Status}, skipping start.", id, entry.Status);
            return;
        }

        entry.Status = McpServerStatus.Starting;
        entry.LastError = null;

        try
        {
            var client = await _connectionFactory.ConnectAsync(entry.Definition, cancellationToken).ConfigureAwait(false);

            var tools = await client.ListToolsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

            entry.Client = client;
            entry.ToolNames = tools.Select(t => t.Name).ToList().AsReadOnly();
            entry.Status = McpServerStatus.Connected;

            await RefreshToolCacheAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("MCP server {ServerId} connected with {ToolCount} tool(s).", id, entry.ToolNames.Count);
        }
        catch (OperationCanceledException)
        {
            entry.Status = McpServerStatus.Stopped;
            entry.LastError = "Operation cancelled";
            throw;
        }
        catch (Exception ex)
        {
            entry.Status = McpServerStatus.Error;
            entry.LastError = ex.Message;
            _logger.LogError(ex, "Failed to start MCP server {ServerId}.", id);
            throw;
        }
    }








    /// <inheritdoc />
    public async Task StopAsync(string id, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        if (!_entries.TryGetValue(id, out var entry))
        {
            _logger.LogDebug("MCP server {ServerId} not found for stop operation.", id);
            return;
        }

        var client = entry.Client;
        entry.Client = null;

        if (client is not null)
        {
            try
            {
                await client.DisposeAsync().ConfigureAwait(false);
                _logger.LogDebug("Disposed MCP client for server {ServerId}.", id);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing MCP client for server {ServerId}.", id);
            }
        }

        entry.Status = McpServerStatus.Stopped;
        entry.ToolNames = Array.Empty<string>();
        entry.LastError = null;

        await RefreshToolCacheAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Stopped MCP server {ServerId}.", id);
    }








    /// <inheritdoc />
    public async Task UpdateAssignmentsAsync(string id, IReadOnlyList<string> assignedAgentNames, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(assignedAgentNames);

        if (!_entries.TryGetValue(id, out var entry))
        {
            throw new InvalidOperationException($"MCP server '{id}' is not registered.");
        }

        var current = entry.Definition;
        var updated = new McpServerDefinition(current.Id, current.DisplayName, current.TransportType, current.CommandOrEndpoint, current.Arguments, current.WorkingDirectory, current.EnvironmentVariables, current.OAuthSettings, assignedAgentNames);

        _entries[id] = new McpServerEntry(updated) { Status = entry.Status, ToolNames = entry.ToolNames, LastError = entry.LastError, Client = entry.Client };

        try
        {
            await PersistAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Updated agent assignments for MCP server {ServerId}.", id);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist updated assignments for MCP server {ServerId}.", id);
            throw;
        }
    }








    /// <summary>
    ///     Refreshes the tool cache from all connected servers.
    /// </summary>
    private async Task RefreshToolCacheAsync(CancellationToken cancellationToken = default)
    {
        await _toolCacheLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _aiToolCache.Clear();
            var tools = await GetAllToolsAsync(cancellationToken).ConfigureAwait(false);
            _aiToolCache.AddRange(tools);
            OnToolCacheChanged();
        }
        finally
        {
            _toolCacheLock.Release();
        }
    }








    /// <summary>
    ///     Persists the registered MCP server definitions.
    /// </summary>
    private async Task PersistAsync(CancellationToken cancellationToken)
    {
        var definitions = _entries.Values.Select(e => e.Definition).ToList();
        await _store.SaveAsync(definitions, cancellationToken).ConfigureAwait(false);
    }








    /// <summary>
    ///     Validates an MCP server definition for required fields.
    /// </summary>
    private static void ValidateDefinition(McpServerDefinition definition)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(definition.Id);
        ArgumentException.ThrowIfNullOrWhiteSpace(definition.DisplayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(definition.CommandOrEndpoint);

        if (definition.TransportType == McpServerTransportType.Http)
        {
            if (!Uri.TryCreate(definition.CommandOrEndpoint, UriKind.Absolute, out _))
            {
                throw new ArgumentException($"Invalid HTTP endpoint URI: '{definition.CommandOrEndpoint}'", nameof(definition.CommandOrEndpoint));
            }
        }
    }








    private void OnToolCacheChanged()
    {
        ToolCacheChanged?.Invoke();
    }








    private void OnServersChanged()
    {
        ServersChanged?.Invoke();
    }








    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(McpServerRegistry));
        }
    }








    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }








    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore().ConfigureAwait(false);
        Dispose(disposing: false);
        GC.SuppressFinalize(this);
    }








    private void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _toolCacheLock.Dispose();
        }

        _disposed = true;
    }








    private async ValueTask DisposeAsyncCore()
    {
        var cancellationToken = CancellationToken.None;

        foreach (var entry in _entries.Values)
        {
            if (entry.Client is not null)
            {
                try
                {
                    await entry.Client.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error disposing MCP client for server {ServerId} during registry disposal.", entry.Definition.Id);
                }
            }
        }

        _entries.Clear();
        _aiToolCache.Clear();
    }








    /// <summary>
    /// Tracks the definition and runtime state of an MCP server, including its client, status, available tools, and
    /// most recent error.
    /// </summary>
    private sealed class McpServerEntry
    {
        private McpClient? _client;








        public McpServerEntry(McpServerDefinition definition)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
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
