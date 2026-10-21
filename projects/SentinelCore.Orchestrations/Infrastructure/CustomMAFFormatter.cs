// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         CustomMAFFormatter.cs
// Author: Kyle L. Crowder
// Build Num:  100416



using System.Diagnostics;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using JsonException = System.Text.Json.JsonException;
using JsonSerializer = System.Text.Json.JsonSerializer;




namespace SentinelCore.Orchestrations.Infrastructure;





/// <summary>
/// Custom formatter for MAF (Microsoft Agent Framework) log entries.
/// Formats AI-related log entries with special handling for chat responses, messages, and metadata.
/// </summary>
public sealed class CustomMAFFormatter
{
    /// <summary>
    /// Formats a log entry for AI-related categories with special handling.
    /// </summary>
    /// <typeparam name="TState">The type of the state object.</typeparam>
    /// <param name="logLevel">The log level.</param>
    /// <param name="s"></param>
    /// <param name="eventId">The event ID.</param>
    /// <param name="state">The state object.</param>
    /// <param name="exception">The exception, if any.</param>
    /// <param name="formatter">The formatter function.</param>
    /// <param name="category">The log category.</param>
    /// <returns>A formatted string representation of the log entry.</returns>
    public string Format<TState>(LogLevel logLevel, string s, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var logLevelStr = ShortenLevel(logLevel);
        var category = s;
        var eventIdValue = eventId.Id;
        string message = "";

        var sb = new StringBuilder();

        // Handle AI-related categories with special formatting
        if (category.Contains("AI", StringComparison.OrdinalIgnoreCase))
        {


            var text = formatter.Invoke(state, exception);

            text = text.Replace("\\r\\n", Environment.NewLine);
            text = text.Replace("\\n", Environment.NewLine);
            text = text.Replace("\\r", Environment.NewLine);
            sb.AppendLine(text);


        }
        else
        {
            //print normal message handling for non-AI categories
            message = formatter?.Invoke(state, exception) ?? "";

        }




        if (!string.IsNullOrEmpty(message))
        {
            sb.AppendLine($"[{logLevelStr}] {category} [{eventIdValue}]: {message}");
        }

        if (exception != null)
        {
            sb.AppendLine(exception.ToString());
        }

        return sb.ToString();
    }








    /// <summary>
    /// Attempts to extract category name from state object.
    /// </summary>
    private static string GetCategoryName<TState>(TState state)
    {
        // Try to get category from state if it's a KeyValuePair list
        if (state is IReadOnlyList<KeyValuePair<string, object>> kvps)
        {
            foreach (var kvp in kvps)
            {
                if (kvp.Key.Equals("Category", StringComparison.OrdinalIgnoreCase) || kvp.Key.Equals("LoggerName", StringComparison.OrdinalIgnoreCase))
                {
                    return kvp.Value?.ToString() ?? "Unknown";
                }
            }
        }

        return "Unknown";
    }








    /// <summary>
    /// Attempts to detect a JSON string and return a pretty‑printed version.
    /// If the input is not valid JSON, the original string is returned unchanged.
    /// This protects the logger from throwing when a message payload contains
    /// raw JSON that is not already formatted.
    /// </summary>
    private static string TryPrettyIfJson(string maybeJson)
    {
        if (string.IsNullOrWhiteSpace(maybeJson))
            return maybeJson;

        string trimmed = maybeJson.Trim();
        // Quick heuristic: JSON objects start with '{' and arrays with '['.
        if ((trimmed.StartsWith("{") && trimmed.EndsWith("}")) || (trimmed.StartsWith("[") && trimmed.EndsWith("]")))
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(trimmed);
                return JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (JsonException)
            {
                // Not valid JSON – fall back to original.
            }
        }

        return maybeJson;
    }








    private string ShortenLevel(LogLevel level)
    {
        return level switch
        {
            LogLevel.Information => "INFO",
            LogLevel.Critical => "CRIT",
            LogLevel.Debug => "DBUG",
            LogLevel.Error => "EROR",
            LogLevel.Trace => "TRCE",
            LogLevel.Warning => "WARN",
            LogLevel.None => "NONE",
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, null)
        };
    }






    /// <summary>
    /// Cleans and formats chat response payload for readable output.
    /// </summary>
    private string CleanResponsePayload(string payload)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(Environment.NewLine);
        sb.AppendLine("==================== Starting of ChatResponse ============================");
        JsonElement messages;
        try
        {
            using JsonDocument doc = JsonDocument.Parse(payload!);

            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                messages = doc.RootElement.Clone();
            }
            else if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("messages", out JsonElement msgElement))
            {
                messages = msgElement.Clone();
            }
            else
            {
                return sb.AppendLine("Payload is not in a recognized ChatResponse format.").ToString();
            }

            if (messages.ValueKind != JsonValueKind.Array)
            {
                return sb.AppendLine("Messages element is not an array.").ToString();
            }
        }
        catch (JsonException)
        {
            Debug.WriteLine($"Payload is not valid JSON. payload {payload}");
            return sb.AppendLine("Payload is not valid JSON.").ToString();
        }

        foreach (JsonElement message in messages.EnumerateArray())
        {
            if (message.ValueKind == JsonValueKind.Object && message.TryGetProperty("contents", out JsonElement contents) && contents.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement content in contents.EnumerateArray())
                {
                    if (content.ValueKind == JsonValueKind.Object)
                    {
                        string? jtext = content.TryGetProperty("text", out JsonElement textElement) ? textElement.GetString() : null;
                        if (!string.IsNullOrEmpty(jtext))
                        {
                            sb.AppendLine(jtext?.ToString() ?? "Error: Processing");
                        }
                    }
                }
            }
        }

        sb.AppendLine();
        sb.AppendLine("==================== Ending of ChatResponse ============================");
        return sb.ToString();
    }
}
