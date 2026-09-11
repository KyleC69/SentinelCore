// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         LoggingChatClient.cs
// Author: Kyle L. Crowder
// Build Num:  091112



using System.Text.Json;

using Microsoft.Extensions.Logging;




namespace SentinelCore.Orchestrations.Agents.Middleware;





/// <summary>
///     A delegating <see cref="IChatClient" /> that logs all requests and responses
///     at trace level for diagnostic purposes.
/// </summary>
public sealed class LoggingChatClient : DelegatingChatClient
{
    private readonly ILogger _logger;








    /// <summary>
    ///     Initializes a new instance of the <see cref="LoggingChatClient" /> class.
    /// </summary>
    /// <param name="inner">The inner chat client to delegate to.</param>
    /// <param name="logger">The logger for trace output.</param>
    public LoggingChatClient(IChatClient inner, ILogger logger) : base(inner)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }








    /// <summary>
    ///     Gets or sets the JSON serializer options for formatting log output.
    /// </summary>
    public JsonSerializerOptions? JsonSerializerOptions { get; set; }








    /// <inheritdoc />
    public override async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        // Cache enumeration to avoid multiple enumeration
        List<ChatMessage> messageList = messages.ToList();

        // Log the request
        LogMessages(messageList, "Agent Request");

        ChatResponse response = await base.GetResponseAsync(messageList, options, cancellationToken).ConfigureAwait(false);

        // Log the response
        LogResponse(response);

        return response;
    }








    /// <inheritdoc />
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Cache enumeration to avoid multiple enumeration
        List<ChatMessage> messageList = messages.ToList();

        // Log the request
        LogMessages(messageList, "Agent Streaming Request");

        await foreach (ChatResponseUpdate update in base.GetStreamingResponseAsync(messageList, options, cancellationToken).ConfigureAwait(false))
        {
            // Log the update
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace("Streaming update received");
            }

            yield return update;
        }
    }








    private void LogMessages(List<ChatMessage> messages, string label)
    {
        if (!_logger.IsEnabled(LogLevel.Trace))
        {
            return;
        }

        try
        {
            string json;
            if (JsonSerializerOptions != null)
            {
                json = JsonSerializer.Serialize(messages, JsonSerializerOptions);
            }
            else
            {
                json = JsonSerializer.Serialize(messages, new JsonSerializerOptions { WriteIndented = true });
            }

            _logger.LogTrace("{Label}:\n{Json}", label, json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to serialize messages for logging");
        }
    }








    private void LogResponse(ChatResponse response)
    {
        if (!_logger.IsEnabled(LogLevel.Trace))
        {
            return;
        }

        try
        {
            string json;
            if (JsonSerializerOptions != null)
            {
                json = JsonSerializer.Serialize(response, JsonSerializerOptions);
            }
            else
            {
                json = JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
            }

            _logger.LogTrace("Agent Response:\n{Json}", json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to serialize response for logging");
        }
    }
}