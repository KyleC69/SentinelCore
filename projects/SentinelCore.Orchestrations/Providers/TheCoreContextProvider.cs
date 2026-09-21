

// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         TheCoreContextProvider.cs
// Author: Kyle L. Crowder
// Build Num:  092121



using System.Text.Json;

using Microsoft.Extensions.Logging;

using SentinelCore.Abstractions;
using SentinelCore.Contracts.Abstractions;




namespace SentinelCore.Orchestrations.Providers;





/// <summary>
///     Provides persistent, file-backed conversational context for the Core agent using a
///     sliding window bounded by a fixed token budget. Context entries are serialized to JSON
///     and stored in the application's local data folder. When the total estimated token count
///     exceeds the budget, the oldest entries are purged to make room for new ones.
/// </summary>
/// <remarks>
///     <para>
///         This provider participates in the MAF agent invocation lifecycle:
///         <list type="bullet">
///             <item><see cref="ProvideAIContextAsync" /> — loads persisted context entries and injects them as messages so the Core agent can reason over prior conversation turns.</item>
///             <item><see cref="StoreAIContextAsync" /> — persists the current invocation's request and response messages, applying the sliding window eviction policy.</item>
///         </list>
///     </para>
///     <para>
///         The sliding window is defined by <see cref="MaxTokens" />. When the estimated
///         token count of all persisted entries exceeds this budget, the oldest entries
///         (by timestamp) are removed until the window fits within the budget.
///     </para>
///     <para>
///         Token estimation uses a character-based heuristic (≈4 characters per token),
///         which provides a reasonable approximation for English text without requiring
///         a model-specific tokenizer dependency.
///     </para>
///     <para>
///         Storage location: <c>%LOCALAPPDATA%\SentinelCore\context\TheCoreContext.json</c>.
///     </para>
/// </remarks>
public sealed class TheCoreContextProvider : AIContextProvider
{
    private const string AppFolderName = "SentinelCore";
    private const string ContextSubFolder = "context";
    private const string FileName = "TheCoreContext.json";

    /// <summary>
    ///     The default token budget (300,000), chosen to coincide with the model's context window.
    /// </summary>
    public const int DEFAULT_MAX_TOKENS = 300_000;

    /// <summary>
    ///     The heuristic ratio of characters per token used for estimation.
    ///     Approximately 4 characters per token is a well-established approximation for English text.
    /// </summary>
    private const double CharactersPerToken = 4.0;

    private readonly ILogger<TheCoreContextProvider> _logger;
    private readonly ISystemReporter _reporter;
    private readonly string _filePath;
    private readonly int _maxTokens;
    private readonly SemaphoreSlim _fileLock = new(1, 1);








    /// <summary>
    ///     Initializes a new instance of the <see cref="TheCoreContextProvider" /> class.
    /// </summary>
    /// <param name="reporter">
    ///     An instance of <see cref="ISystemReporter" /> used for reporting system-level events or errors.
    /// </param>
    /// <param name="logger">
    ///     The logger for diagnostic output.
    /// </param>
    /// <param name="maxTokens">
    ///     The maximum estimated token budget for the sliding window. When the total
    ///     estimated tokens exceed this budget, the oldest entries are purged.
    ///     Defaults to <see cref="DEFAULT_MAX_TOKENS" /> (300,000).
    /// </param>
    /// <param name="filePath">
    ///     Optional override for the context file path. When not specified, the default
    ///     path is <c>%LOCALAPPDATA%\SentinelCore\context\TheCoreContext.json</c>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="reporter" /> or <paramref name="logger" /> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when <paramref name="maxTokens" /> is less than 1.
    /// </exception>
    public TheCoreContextProvider(ISystemReporter reporter, ILogger<TheCoreContextProvider> logger, int maxTokens = DEFAULT_MAX_TOKENS, string? filePath = null)
    {
        ArgumentNullException.ThrowIfNull(reporter);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxTokens, 1);

        _reporter = reporter;
        _logger = logger;
        _maxTokens = maxTokens;
        _filePath = filePath ?? BuildDefaultFilePath();
    }








    /// <summary>
    ///     Gets the maximum estimated token budget for the sliding window.
    ///     When the total estimated tokens of all persisted entries exceed this budget,
    ///     the oldest entries are purged until the window fits.
    /// </summary>
    public int MaxTokens => _maxTokens;



    /// <summary>
    ///     Gets the file path used for context persistence.
    /// </summary>
    public string FilePath => _filePath;








    /// <summary>
    ///     Loads persisted context entries from disk and returns them as an <see cref="AIContext" />
    ///     containing messages that the Core agent can reason over.
    /// </summary>
    /// <param name="context">
    ///     The invocation context containing the agent, session, and current AI context.
    /// </param>
    /// <param name="cancellationToken">
    ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
    /// </param>
    /// <returns>
    ///     An <see cref="AIContext" /> with messages from the persisted sliding window,
    ///     or an empty context if no entries exist or loading fails.
    /// </returns>
    protected override async ValueTask<AIContext> ProvideAIContextAsync(InvokingContext context, CancellationToken cancellationToken = default)
    {
        Throw.IfNull(context);

        try
        {
            List<ContextEntry> entries = await LoadEntriesAsync(cancellationToken).ConfigureAwait(false);

            if (entries.Count == 0)
            {
                _logger.LogTrace("TheCoreContextProvider: No persisted context entries found.");
                return new AIContext();
            }

            _reporter.ReportInfo($"TheCoreContextProvider: Injecting {entries.Count} persisted context entries.");

            List<ChatMessage> messages = new(entries.Count);

            foreach (ContextEntry entry in entries)
            {
                string role = entry.Role ?? "assistant";
                ChatRole chatRole = new(role);
                ChatMessage msg = new(chatRole, entry.Content ?? string.Empty);
                messages.Add(msg);
            }

            return new AIContext { Messages = messages };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TheCoreContextProvider: Failed to load context entries from {FilePath}", _filePath);
            _reporter.ReportError("TheCoreContextProvider: Failed to load persisted context.", ex);

            // Fail open — return empty context rather than blocking the agent invocation.
            return new AIContext();
        }
    }








    /// <summary>
    ///     Persists the current invocation's request and response messages to disk,
    ///     applying the sliding window eviction policy. When the total entry count
    ///     the oldest entries are purged.
    /// </summary>
    /// <param name="context">
    ///     The invoked context containing request messages, response messages, and any exception.
    /// </param>
    /// <param name="cancellationToken">
    ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
    /// </param>
    protected override async ValueTask StoreAIContextAsync(AIContextProvider.InvokedContext context, CancellationToken cancellationToken = default)
    {
        Throw.IfNull(context);

        try
        {
            List<ContextEntry> entries = await LoadEntriesAsync(cancellationToken).ConfigureAwait(false);

            // Append new entries from the current invocation.
            DateTime now = DateTime.UtcNow;

            if (context.RequestMessages is not null)
            {
                foreach (ChatMessage msg in context.RequestMessages)
                {
                    entries.Add(new ContextEntry { Timestamp = now, Role = msg.Role.Value ?? "user", Content = msg.Text });
                }
            }

            if (context.ResponseMessages is not null)
            {
                foreach (ChatMessage msg in context.ResponseMessages)
                {
                    entries.Add(new ContextEntry { Timestamp = now, Role = msg.Role.Value ?? "assistant", Content = msg.Text });
                }
            }

            // Sliding window eviction: purge oldest entries until the estimated
            // token count fits within the budget.
            int totalEstimatedTokens = EstimateTokenCount(entries);

            if (totalEstimatedTokens > _maxTokens)
            {
                // Sort oldest-first, then remove from the beginning until under budget.
                entries = entries.OrderBy(e => e.Timestamp).ToList();
                int purged = 0;

                while (entries.Count > 0 && EstimateTokenCount(entries) > _maxTokens)
                {
                    entries.RemoveAt(0);
                    purged++;
                }

                _logger.LogTrace("TheCoreContextProvider: Purged {Purged} oldest entries to fit token budget ({EstimatedTokens}/{MaxTokens}).", purged, totalEstimatedTokens, _maxTokens);
            }

            await SaveEntriesAsync(entries, cancellationToken).ConfigureAwait(false);

            int finalTokenCount = EstimateTokenCount(entries);
            _reporter.ReportInfo($"TheCoreContextProvider: Persisted context. Entries: {entries.Count}, Estimated tokens: {finalTokenCount}/{_maxTokens}.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TheCoreContextProvider: Failed to persist context entries to {FilePath}", _filePath);
            _reporter.ReportError("TheCoreContextProvider: Failed to persist context.", ex);
        }
    }








    /// <summary>
    ///     Loads context entries from the persistence file.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of <see cref="ContextEntry" /> instances, or an empty list if the file does not exist or cannot be read.</returns>
    private async Task<List<ContextEntry>> LoadEntriesAsync(CancellationToken cancellationToken)
    {
        await _fileLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (!File.Exists(_filePath))
            {
                return new List<ContextEntry>();
            }

            await using FileStream stream = new(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
            List<ContextEntry>? entries = await JsonSerializer.DeserializeAsync<List<ContextEntry>>(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

            return entries ?? new List<ContextEntry>();
        }
        finally
        {
            _fileLock.Release();
        }
    }








    /// <summary>
    ///     Saves context entries to the persistence file, creating the directory if it does not exist.
    /// </summary>
    /// <param name="entries">The context entries to persist.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    private async Task SaveEntriesAsync(List<ContextEntry> entries, CancellationToken cancellationToken)
    {
        await _fileLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            string? directory = Path.GetDirectoryName(_filePath);

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await using FileStream stream = new(_filePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true);
            await JsonSerializer.SerializeAsync(stream, entries, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _fileLock.Release();
        }
    }








    /// <summary>
    ///     Builds the default file path using <see cref="Environment.SpecialFolder.LocalApplicationData" />.
    /// </summary>
    /// <returns>The fully qualified path to the context persistence file.</returns>
    private static string BuildDefaultFilePath()
    {
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appDataPath, AppFolderName, ContextSubFolder, FileName);
    }








    /// <summary>
    ///     Estimates the total token count for a collection of context entries
    ///     using the character-based heuristic (<see cref="CharactersPerToken" />).
    /// </summary>
    /// <param name="entries">The context entries to estimate tokens for.</param>
    /// <returns>The estimated total token count.</returns>
    private static int EstimateTokenCount(List<ContextEntry> entries)
    {
        int totalChars = 0;

        foreach (ContextEntry entry in entries)
        {
            totalChars += entry.Content?.Length ?? 0;
        }

        return (int)Math.Ceiling(totalChars / CharactersPerToken);
    }








    /// <summary>
    ///     Represents a single context entry persisted to disk.
    /// </summary>
    internal sealed class ContextEntry
    {
        /// <summary>
        ///     The UTC timestamp when this entry was recorded.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        ///     The chat role of the message (e.g., "user", "assistant", "system").
        /// </summary>
        public string? Role { get; set; }

        /// <summary>
        ///     The text content of the message.
        /// </summary>
        public string? Content { get; set; }
    }
}
