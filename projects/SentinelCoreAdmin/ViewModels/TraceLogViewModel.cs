// Solution: SentinelCore
// Project:   SentinelCoreAdmin
// File:         TraceLogViewModel.cs
// Author: Kyle L. Crowder
// Build Num:  082808



using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using JetBrains.Annotations;

using Microsoft.Extensions.Logging;

using SentinelCoreAdmin.Contracts.Services;
using SentinelCoreAdmin.Contracts.ViewModels;




namespace SentinelCoreAdmin.ViewModels;





/// <summary>
///     View-model for the TraceLog page.
///     Loads and displays the SentinelCore JSON trace log file written
///     by <c>JsonLoggerProvider</c> to the user profile directory.
/// </summary>
public partial class TraceLogViewModel : ObservableObject, INavigationAware
{

    [ObservableProperty] private Visibility _failedMessageVisibility = Visibility.Collapsed;

    [ObservableProperty] private bool _isLoaded;

    [ObservableProperty] private bool _isLoading;

    [ObservableProperty] private string _logContent = string.Empty;

    [ObservableProperty] private ObservableCollection<LogEntry> _logEntries = new();

    [ObservableProperty] private string _logFilePath = string.Empty;
    private readonly ILogger<TraceLogViewModel> _logger;

    [ObservableProperty] private string _searchText = string.Empty;

    [ObservableProperty] private LogEntry? _selectedLogEntry;
    private readonly ISystemService _systemService;








    public TraceLogViewModel([CanBeNull] ISystemService systemService, [CanBeNull] ILogger<TraceLogViewModel> logger)
    {
        _systemService = systemService;
        _logger = logger;
    }








    public void OnNavigatedFrom()
    {
    }








    public async void OnNavigatedTo([CanBeNull] object parameter)
    {
        await RefreshLogAsync();
    }








    [RelayCommand]
    private void ClearSearch()
    {
        SearchText = string.Empty;
    }








    [RelayCommand]
    private void OpenLogFolder()
    {
        string? directory = Path.GetDirectoryName(LogFilePath);
        string path = directory ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        _systemService.OpenInWebBrowser(path);
    }








    [RelayCommand]
    private async Task RefreshLogAsync()
    {
        IsLoading = true;
        FailedMessageVisibility = Visibility.Collapsed;

        try
        {
            string userProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string todayFileName = $"SentinelCore_{DateTime.Now:yyyyMMdd}.log";
            string fullPath = Path.Combine(userProfilePath, todayFileName);

            LogFilePath = fullPath;

            if (File.Exists(fullPath))
            {
                string content = await File.ReadAllTextAsync(fullPath);

                LogContent = content;
                LogEntries.Clear();

                // Parse each line as a JSON log entry
                foreach (string line in content.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    LogEntry? entry = LogEntry.Parse(line);
                    if (entry is not null)
                    {
                        LogEntries.Add(entry);
                    }
                }

                IsLoaded = true;
                _logger.LogTrace("TraceLog loaded {EntryCount} entries from {Path}", LogEntries.Count, fullPath);
            }
            else
            {
                LogContent = $"No trace log file found at: {fullPath}";
                LogEntries.Clear();
                IsLoaded = false;
                _logger.LogWarning("TraceLog file not found: {Path}", fullPath);
            }
        }
        catch (Exception ex)
        {
            FailedMessageVisibility = Visibility.Visible;
            _logger.LogError(ex, "Failed to load trace log");
        }
        finally
        {
            IsLoading = false;
        }
    }
}





/// <summary>
///     Represents a single parsed JSON log entry from the SentinelCore trace log.
/// </summary>
public sealed class LogEntry
{
    public string Category { get; init; } = string.Empty;
    public int EventId { get; init; }
    public string? Exception { get; init; }
    public string Level { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public DateTimeOffset Timestamp { get; init; }


    public override string ToString() => $"[{Timestamp:HH:mm:ss.fff}] [{Level}] {Category}: {Message}";








    public static LogEntry? Parse(string jsonLine)
    {
        try
        {
            using System.Text.Json.JsonDocument doc = System.Text.Json.JsonDocument.Parse(jsonLine);
            System.Text.Json.JsonElement root = doc.RootElement;

            return new LogEntry
            {
                    Timestamp = root.TryGetProperty("Timestamp", out System.Text.Json.JsonElement ts) ? ts.GetDateTimeOffset() : DateTimeOffset.Now,
                    Level = root.TryGetProperty("Level", out System.Text.Json.JsonElement lvl) ? lvl.GetString() ?? "Unknown" : "Unknown",
                    Category = root.TryGetProperty("Category", out System.Text.Json.JsonElement cat) ? cat.GetString() ?? "" : "",
                    Message = root.TryGetProperty("Message", out System.Text.Json.JsonElement msg) ? msg.GetString() ?? "" : "",
                    EventId = root.TryGetProperty("EventId", out System.Text.Json.JsonElement eid) ? eid.GetInt32() : 0,
                    Exception = root.TryGetProperty("Exception", out System.Text.Json.JsonElement ex) && ex.ValueKind != System.Text.Json.JsonValueKind.Null ? ex.GetString() : null
            };
        }
        catch
        {
            return null;
        }
    }
}