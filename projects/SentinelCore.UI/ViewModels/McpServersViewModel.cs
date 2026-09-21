// Solution: SentinelCore
// Project:   SentinelCore.UI
// File:         McpServersViewModel.cs
// Author: Kyle L. Crowder
// Build Num:  091418



using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Input;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.Extensions.Logging;

using SentinelCore.Contracts.Mcp;
using SentinelCore.UI.Models;
using SentinelCore.UI.Services;




namespace SentinelCore.UI.ViewModels;





/// <summary>
///     View-model for the MCP Servers management page.
///     Allows users to view, add, remove, start, and stop MCP servers that
///     supply tools to Sentinel agents. Agent assignments can be configured
///     so that only selected agents receive a server's tools.
/// </summary>
public sealed partial class McpServersViewModel : ObservableObject, INavigationAware, IDisposable
{
    private readonly ISentinelAgentCatalog _agentCatalog;

    /// <summary>
    ///     Guards <see cref="LoadSelectedServerAssignmentsAsync" /> against stale
    ///     results when the selection changes while a load is in flight.
    /// </summary>
    private int _assignmentLoadVersion;

    [ObservableProperty] private ObservableCollection<string> _availableAgents = [];

    private readonly IDialogService _dialogService;
    private readonly IDispatcherService _dispatcher;

    /// <summary>
    ///     Tracks whether this view-model has been disposed.
    /// </summary>
    private bool _disposed;

    private readonly IFolderBrowserService _folderBrowser;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddServerCommand))]
    private bool _isAddingServer;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddServerCommand))]
    [NotifyCanExecuteChangedFor(nameof(StartServerCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopServerCommand))]
    [NotifyCanExecuteChangedFor(nameof(RemoveServerCommand))]
    [NotifyCanExecuteChangedFor(nameof(UpdateAssignmentsCommand))]
    private bool _isBusy;

    private readonly CancellationTokenSource _lifecycleCts = new();
    private readonly ILogger<McpServersViewModel> _logger;

    [ObservableProperty] private ObservableCollection<AgentAssignmentRow> _newServerAgentAssignments = [];

    [ObservableProperty] private string _newServerArgumentsText = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddServerCommand))]
    private string _newServerCommandOrEndpoint = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddServerCommand))]
    private string _newServerDisplayName = string.Empty;

    [ObservableProperty] private string _newServerEnvironmentText = string.Empty;

    [ObservableProperty] private McpServerTransportType _newServerTransportType = McpServerTransportType.Stdio;

    [ObservableProperty] private string _newServerWorkingDirectory = string.Empty;

    private readonly IMcpServerRegistry _registry;

    [ObservableProperty] private string _resultMessage = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartServerCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopServerCommand))]
    [NotifyCanExecuteChangedFor(nameof(RemoveServerCommand))]
    [NotifyCanExecuteChangedFor(nameof(UpdateAssignmentsCommand))]
    private McpServerRow? _selectedServer;

    [ObservableProperty] private ObservableCollection<AgentAssignmentRow> _selectedServerAgentAssignments = [];

    [ObservableProperty] private ObservableCollection<McpServerRow> _servers = [];

    [ObservableProperty] private bool _showResult;








    /// <summary>
    ///     Creates a new <see cref="McpServersViewModel" /> with required dependencies.
    /// </summary>
    /// <param name="registry">The MCP server registry.</param>
    /// <param name="agentCatalog">The catalog of logical agent names.</param>
    /// <param name="logger">The logger for this view-model.</param>
    /// <param name="dispatcher">The dispatcher service for thread-affinity marshaling.</param>
    /// <param name="dialogService">The dialog service for destructive-action confirmations.</param>
    /// <param name="folderBrowser">The folder browser service for the working-directory picker.</param>
    public McpServersViewModel(IMcpServerRegistry registry, ISentinelAgentCatalog agentCatalog, ILogger<McpServersViewModel> logger, IDispatcherService dispatcher, IDialogService dialogService, IFolderBrowserService folderBrowser)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _agentCatalog = agentCatalog ?? throw new ArgumentNullException(nameof(agentCatalog));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _folderBrowser = folderBrowser ?? throw new ArgumentNullException(nameof(folderBrowser));

        HookCommand(AddServerCommand);
        HookCommand(StartServerCommand);
        HookCommand(StopServerCommand);
        HookCommand(RemoveServerCommand);
        HookCommand(UpdateAssignmentsCommand);
    }








    public McpServersViewModel()
    {
    }








    /// <summary>
    ///     Gets a value indicating whether a server is selected, driving the
    ///     assignment editor visibility.
    /// </summary>
    public bool HasSelectedServer
    {
        get => SelectedServer is not null;
    }

    /// <summary>
    ///     Gets a value indicating whether the add-server form targets a stdio server,
    ///     controlling the enabled state of the stdio-only input fields.
    /// </summary>
    public bool IsStdioTransport
    {
        get => NewServerTransportType == McpServerTransportType.Stdio;
    }

    /// <summary>
    ///     Gets the supported transport types for the add-server combo box.
    /// </summary>
    public IReadOnlyList<McpServerTransportType> TransportTypes { get; } = Enum.GetValues<McpServerTransportType>().ToList();









    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        try
        {
            _lifecycleCts.Cancel();
            _lifecycleCts.Dispose();
        }
        catch (ObjectDisposedException)
        {
            // Already disposed; ignore.
        }
    }








    /// <summary>
    ///     Invoked when navigation leaves the current view or page.
    /// </summary>
    /// <remarks>
    ///     Use to release resources or persist transient state before navigation. Avoid long-running
    ///     work on the UI thread; perform asynchronous operations where supported. When overriding, call the base
    ///     implementation.
    /// </remarks>
    public void OnNavigatedFrom()
    {
    }









    public void OnNavigatedTo(object? parameter)
    {
        _ = LoadAsync(_lifecycleCts.Token);
    }








    [RelayCommand(CanExecute = nameof(CanAddServer))]
    private async Task AddServerAsync(CancellationToken token)
    {
        SetBusyState(true);
        ShowResult = false;

        try
        {
            string id = Guid.NewGuid().ToString("N");
            IReadOnlyList<string> arguments = ParseArguments(NewServerArgumentsText);
            IReadOnlyDictionary<string, string> environment = ParseEnvironment(NewServerEnvironmentText);
            IReadOnlyList<string> assignedAgents = NewServerAgentAssignments.Where(r => r.IsAssigned).Select(r => r.AgentName).ToList();

            McpServerDefinition definition = new(id, NewServerDisplayName.Trim(), NewServerTransportType, NewServerCommandOrEndpoint.Trim(), arguments, string.IsNullOrWhiteSpace(NewServerWorkingDirectory) ? null : NewServerWorkingDirectory.Trim(), environment.Count > 0 ? environment : null, assignedAgentNames: assignedAgents.Count > 0 ? assignedAgents : null);

            await _registry.RegisterAsync(definition, token);

            _logger.LogInformation("Registered MCP server {ServerId} ({DisplayName}) via UI", id, definition.DisplayName);

            ResetAddForm();
            IsAddingServer = false;
            ResultMessage = $"Server '{definition.DisplayName}' added.";
            ShowResult = true;

            await LoadAsync(token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add MCP server");
            ResultMessage = $"Error adding server: {ex.Message}";
            ShowResult = true;
        }
        finally
        {
            SetBusyState(false);
        }
    }








    /// <summary>
    ///     Applies freshly loaded rows and agent names to the bound collections,
    ///     preserving the selected server when it still exists.
    /// </summary>
    /// <param name="rows">The freshly mapped server rows.</param>
    /// <param name="agentNames">The freshly loaded agent names.</param>
    private void ApplyLoadedData(List<McpServerRow> rows, List<string> agentNames)
    {
        string? selectedId = SelectedServer?.Id;

        Servers.Clear();
        foreach (McpServerRow row in rows)
        {
            Servers.Add(row);
        }

        AvailableAgents.Clear();
        foreach (string agent in agentNames)
        {
            AvailableAgents.Add(agent);
        }

        NewServerAgentAssignments.Clear();
        foreach (string agent in agentNames)
        {
            NewServerAgentAssignments.Add(new AgentAssignmentRow { AgentName = agent });
        }

        // Restore the selection when the server still exists; otherwise clear it.
        SelectedServer = selectedId is null ? null : Servers.FirstOrDefault(s => s.Id == selectedId);
    }








    /// <summary>
    ///     Opens the folder picker and stores the selected path as the new
    ///     server's working directory (stdio only).
    /// </summary>
    [RelayCommand]
    private void BrowseWorkingDirectory()
    {
        string? folder = _folderBrowser.BrowseFolder("Select the server working directory", NewServerWorkingDirectory);

        if (!string.IsNullOrWhiteSpace(folder))
        {
            NewServerWorkingDirectory = folder;
        }
    }








    private bool CanAddServer() =>
            !IsBusy && !string.IsNullOrWhiteSpace(NewServerDisplayName) && !string.IsNullOrWhiteSpace(NewServerCommandOrEndpoint) && IsAddingServer;








    private bool CanRemoveServer() =>
            !IsBusy && SelectedServer is not null;








    private bool CanStartServer() =>
            !IsBusy && SelectedServer is not null && SelectedServer.Status is not McpServerStatus.Starting && SelectedServer.Status is not McpServerStatus.Connected;








    private bool CanStopServer() =>
            !IsBusy && SelectedServer is not null && SelectedServer.Status is not McpServerStatus.Stopped && SelectedServer.Status is not McpServerStatus.Starting;








    [RelayCommand]
    private void CancelAddServer()
    {
        IsAddingServer = false;
        ResetAddForm();
        ShowResult = false;
    }








    private void HookCommand(IRelayCommand command)
    {
        command.CanExecuteChanged += (s, e) =>
        {
            if (_dispatcher.CheckAccess())
            {
                CommandManager.InvalidateRequerySuggested();
            }
            else
            {
                _dispatcher.Invoke(CommandManager.InvalidateRequerySuggested);
            }
        };
    }








    /// <summary>
    ///     Loads the server list and agent catalog into the bound collections.
    ///     The collections are mutated in place (never reassigned) so the
    ///     <see cref="SelectedServer" /> binding survives a refresh.
    /// </summary>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A task that represents the asynchronous load operation.</returns>
    private async Task LoadAsync(CancellationToken token)
    {
        IReadOnlyList<McpServerInfo> servers;
        IReadOnlyList<string> agents;

        try
        {
            servers = await _registry.ListAsync(token).ConfigureAwait(false);
            agents = await _agentCatalog.GetAgentNamesAsync(token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load MCP servers");
            ResultMessage = $"Error loading servers: {ex.Message}";
            ShowResult = true;
            return;
        }

        List<McpServerRow> rows = servers.Select(MapToRow).ToList();
        List<string> agentNames = agents.ToList();

        if (_dispatcher.CheckAccess())
        {
            ApplyLoadedData(rows, agentNames);
        }
        else
        {
            await _dispatcher.InvokeAsync(() => ApplyLoadedData(rows, agentNames)).ConfigureAwait(false);
        }
    }








    /// <summary>
    ///     Populates the assignment editor for the currently selected server from the registry.
    ///     A version guard discards stale results when the selection changes mid-load.
    /// </summary>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A task that represents the asynchronous load operation.</returns>
    private async Task LoadSelectedServerAssignmentsAsync(CancellationToken token)
    {
        McpServerRow? selected = SelectedServer;
        if (selected is null)
        {
            return;
        }

        int version = ++_assignmentLoadVersion;

        try
        {
            McpServerInfo? info = await _registry.GetAsync(selected.Id, token).ConfigureAwait(false);
            if (info is null)
            {
                return;
            }

            IReadOnlyList<string> agents = await _agentCatalog.GetAgentNamesAsync(token).ConfigureAwait(false);

            // Discard the result when the selection changed while loading.
            if (version != _assignmentLoadVersion || !ReferenceEquals(SelectedServer, selected))
            {
                return;
            }

            ObservableCollection<AgentAssignmentRow> rows = [];
            foreach (string agent in agents)
            {
                rows.Add(new AgentAssignmentRow { AgentName = agent, IsAssigned = info.Definition.AssignedAgentNames.Count == 0 || info.Definition.AssignedAgentNames.Contains(agent, StringComparer.OrdinalIgnoreCase) });
            }

            if (_dispatcher.CheckAccess())
            {
                SelectedServerAgentAssignments = rows;
            }
            else
            {
                await _dispatcher.InvokeAsync(() => SelectedServerAgentAssignments = rows).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load agent assignments for MCP server {ServerId}", selected.Id);
        }
    }








    private static McpServerRow MapToRow(McpServerInfo server)
    {
        return new McpServerRow
        {
            Id = server.Definition.Id,
            DisplayName = server.Definition.DisplayName,
            TransportType = server.Definition.TransportType,
            CommandOrEndpoint = server.Definition.CommandOrEndpoint,
            Status = server.Status,
            ToolNamesText = string.Join(", ", server.ToolNames),
            AssignedAgentsText = server.Definition.AssignedAgentNames.Count > 0 ? string.Join(", ", server.Definition.AssignedAgentNames) : "All agents",
            LastError = server.LastError
        };
    }








    /// <summary>
    ///     Raises change notification for <see cref="IsStdioTransport" /> when the
    ///     selected transport type changes.
    /// </summary>
    /// <param name="oldValue">The previously selected transport type.</param>
    /// <param name="newValue">The newly selected transport type.</param>
    partial void OnNewServerTransportTypeChanged(McpServerTransportType oldValue, McpServerTransportType newValue)
    {
        this.OnPropertyChanged(nameof(IsStdioTransport));
    }








    /// <summary>
    ///     Reloads the assignment editor whenever the selected server changes.
    /// </summary>
    /// <param name="oldValue">The previously selected server row.</param>
    /// <param name="newValue">The newly selected server row.</param>
    partial void OnSelectedServerChanged(McpServerRow? oldValue, McpServerRow? newValue)
    {
        this.OnPropertyChanged(nameof(HasSelectedServer));

        if (newValue is null)
        {
            if (_dispatcher.CheckAccess())
            {
                SelectedServerAgentAssignments.Clear();
            }
            else
            {
                _dispatcher.Invoke(() => SelectedServerAgentAssignments.Clear());
            }

            return;
        }

        _ = LoadSelectedServerAssignmentsAsync(_lifecycleCts.Token);
    }








    private static IReadOnlyList<string> ParseArguments(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        List<string> result = [];
        StringBuilder current = new();
        bool inQuotes = false;

        foreach (char c in text)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (char.IsWhiteSpace(c) && !inQuotes)
            {
                if (current.Length > 0)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }

                continue;
            }

            current.Append(c);
        }

        if (current.Length > 0)
        {
            result.Add(current.ToString());
        }

        return result;
    }








    private static IReadOnlyDictionary<string, string> ParseEnvironment(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new Dictionary<string, string>();
        }

        Dictionary<string, string> result = new(StringComparer.OrdinalIgnoreCase);

        foreach (string raw in text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            string line = raw.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
            {
                continue;
            }

            int separator = line.IndexOf('=');
            if (separator < 0)
            {
                continue;
            }

            string name = line[..separator].Trim();
            string value = line[(separator + 1)..].Trim();
            result[name] = value;
        }

        return result;
    }








    [RelayCommand]
    private async Task RefreshAsync(CancellationToken token)
    {
        SetBusyState(true);
        ShowResult = false;

        try
        {
            await LoadAsync(token).ConfigureAwait(false);
        }
        finally
        {
            SetBusyState(false);
        }
    }








    [RelayCommand(CanExecute = nameof(CanRemoveServer))]
    private async Task RemoveServerAsync(CancellationToken token)
    {
        if (SelectedServer is null)
        {
            return;
        }

        // Destructive action — confirm before removing the server registration.
        bool confirmed = _dialogService.Confirm("Remove MCP Server", $"Remove '{SelectedServer.DisplayName}'? Its tools will no longer be available to assigned agents.", "Remove", isDestructive: true);

        if (!confirmed)
        {
            return;
        }

        SetBusyState(true);
        ShowResult = false;

        try
        {
            await _registry.RemoveAsync(SelectedServer.Id, token);
            _logger.LogInformation("Removed MCP server {ServerId} via UI", SelectedServer.Id);
            SelectedServer = null;
            ResultMessage = "Server removed.";
            ShowResult = true;
            await LoadAsync(token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove MCP server");
            ResultMessage = $"Error removing server: {ex.Message}";
            ShowResult = true;
        }
        finally
        {
            SetBusyState(false);
        }
    }








    private void ResetAddForm()
    {
        NewServerDisplayName = string.Empty;
        NewServerCommandOrEndpoint = string.Empty;
        NewServerArgumentsText = string.Empty;
        NewServerTransportType = McpServerTransportType.Stdio;
        NewServerWorkingDirectory = string.Empty;
        NewServerEnvironmentText = string.Empty;

        foreach (AgentAssignmentRow row in NewServerAgentAssignments)
        {
            row.IsAssigned = false;
        }
    }








    private void SetBusyState(bool isBusy)
    {
        if (_dispatcher.CheckAccess())
        {
            IsBusy = isBusy;
            return;
        }

        _dispatcher.Invoke(() => IsBusy = isBusy);
    }








    [RelayCommand]
    private void ShowAddServer()
    {
        IsAddingServer = true;
        ShowResult = false;
    }








    [RelayCommand(CanExecute = nameof(CanStartServer))]
    private async Task StartServerAsync(CancellationToken token)
    {
        if (SelectedServer is null)
        {
            return;
        }

        SetBusyState(true);
        ShowResult = false;

        try
        {
            await _registry.StartAsync(SelectedServer.Id, token);
            _logger.LogInformation("Started MCP server {ServerId} via UI", SelectedServer.Id);
            ResultMessage = $"Server '{SelectedServer.DisplayName}' started.";
            ShowResult = true;
            await LoadAsync(token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start MCP server");
            ResultMessage = $"Error starting server: {ex.Message}";
            ShowResult = true;
            await LoadAsync(token).ConfigureAwait(false);
        }
        finally
        {
            SetBusyState(false);
        }
    }








    [RelayCommand(CanExecute = nameof(CanStopServer))]
    private async Task StopServerAsync(CancellationToken token)
    {
        if (SelectedServer is null)
        {
            return;
        }

        SetBusyState(true);
        ShowResult = false;

        try
        {
            await _registry.StopAsync(SelectedServer.Id, token).ConfigureAwait(false);
            _logger.LogInformation("Stopped MCP server {ServerId} via UI", SelectedServer.Id);
            ResultMessage = $"Server '{SelectedServer.DisplayName}' stopped.";
            ShowResult = true;
            await LoadAsync(token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop MCP server");
            ResultMessage = $"Error stopping server: {ex.Message}";
            ShowResult = true;
            await LoadAsync(token).ConfigureAwait(false);
        }
        finally
        {
            SetBusyState(false);
        }
    }








    [RelayCommand(CanExecute = nameof(CanRemoveServer))]
    private async Task UpdateAssignmentsAsync(CancellationToken token)
    {
        if (SelectedServer is null)
        {
            return;
        }

        SetBusyState(true);
        ShowResult = false;

        try
        {
            List<string> assigned = SelectedServerAgentAssignments.Where(r => r.IsAssigned).Select(r => r.AgentName).ToList();

            await _registry.UpdateAssignmentsAsync(SelectedServer.Id, assigned, token).ConfigureAwait(false);
            _logger.LogInformation("Updated agent assignments for MCP server {ServerId} via UI", SelectedServer.Id);
            ResultMessage = "Agent assignments updated.";
            ShowResult = true;
            await LoadAsync(token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update agent assignments for MCP server {ServerId}", SelectedServer.Id);
            ResultMessage = $"Error updating assignments: {ex.Message}";
            ShowResult = true;
        }
        finally
        {
            SetBusyState(false);
        }
    }
}
