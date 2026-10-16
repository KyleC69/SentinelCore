// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         JsonLoggerProvider.cs
// Author: Kyle L. Crowder
// Build Num:  100416



using System.Net;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;




namespace SentinelCore.Orchestrations.Infrastructure;





public sealed class JsonLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    private readonly JsonLoggerOptions _options;
    private IExternalScopeProvider? _scopeProvider;








    public JsonLoggerProvider(JsonLoggerOptions options = null!)
    {
        _options = options ?? new JsonLoggerOptions();
    }








    public ILogger CreateLogger(string categoryName)
    {
        return new JsonLogger(categoryName, _options, () => _scopeProvider ?? new LoggerExternalScopeProvider());
    }








    public void Dispose()
    {
        // If you add file/stream resources, dispose them here.
    }








    public void SetScopeProvider(IExternalScopeProvider scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }








    private sealed class JsonLogger : ILogger
    {
        private readonly string _category;
        private readonly JsonLoggerOptions _options;
        private readonly Func<IExternalScopeProvider> _scopeProviderAccessor;








        public JsonLogger(string category, JsonLoggerOptions options, Func<IExternalScopeProvider> scopeProviderAccessor)
        {
            _category = category;
            _options = options;
            _scopeProviderAccessor = scopeProviderAccessor;
        }








        IDisposable ILogger.BeginScope<TState>(TState state)
        {
            return _scopeProviderAccessor()?.Push(state) ?? NullScope.Instance;
        }








        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= _options.MinimumLevel;
        }








        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            IExternalScopeProvider? scopeProvider = _scopeProviderAccessor();

            List<object> scopes = new();
            scopeProvider?.ForEachScope((scope, list) => { list.Add(scope!); }, scopes);

            string level = ConvertLevel(logLevel);
            string renderedMessage = formatter(state, exception!);

            string normalized = WebUtility.HtmlDecode(renderedMessage);

            // Debugger.Break();






            if (_category.Contains("AI", StringComparison.OrdinalIgnoreCase))
            {

                if (state is IReadOnlyList<KeyValuePair<string, object>> kvps)
                {
                    foreach (var kvp in kvps)
                    {
                        if (kvp.Key.Contains("ChatResponse",StringComparison.OrdinalIgnoreCase))
                        {


                            var chatResponseJson = kvp.Value as string;
                            var prettyJson = CleanResponsePayload(chatResponseJson!);
                            Write(prettyJson);
                        }
                        else if (kvp.Key.Contains("Messages",StringComparison.OrdinalIgnoreCase))
                        {
                            var chatResponseJson = kvp.Value as string;
                            var prettyJson = CleanResponsePayload(chatResponseJson!);
                            Write(prettyJson);
                        }
                        else if(kvp.Key.Contains("ChatOptions",StringComparison.OrdinalIgnoreCase) || kvp.Key.Contains("ChatClientMetadata",StringComparison.OrdinalIgnoreCase))
                        {
                            Write($"{kvp.Value.ToString()!}{Environment.NewLine}");
                        }
                        else
                        {
                            Write($"Unhandled Key is {kvp.Key}{Environment.NewLine}");
                        }

                    }
                }
            }

            else if (!_category.Contains("AI", StringComparison.OrdinalIgnoreCase))
            {
                var normal = $"{level} : {DateTime.Now} : {_category} : [{eventId.Id}] : {renderedMessage}{Environment.NewLine}";
                Write(normal);
            }

            LogEnvelope envelope = new()
            {
                Timestamp = DateTimeOffset.Now,
                Level = level,
                Category = _category,
                EventId = eventId.Id,
                Message = formatter(state, exception!),
                Exception = exception?.ToString(),
                Scopes = scopes
            };


            string json = JsonSerializer.Serialize(envelope, new JsonSerializerOptions { WriteIndented = _options.Indented, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });

            //   Write(json);
        }




        private string ConvertLevel(LogLevel level)
        {
            return level switch
            {
                LogLevel.Trace => "TRC ",
                LogLevel.Debug => "DBUG",
                LogLevel.Information => "INFO",
                LogLevel.Warning => "WARN",
                LogLevel.Error => "ERR ",
                LogLevel.Critical => "CRIT",
                LogLevel.None => "NONE",
                _ => "UNKN"
            };
        }








        private string CleanResponsePayload(string payload)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("==================== Starting of ChatResponse ============================");
            using JsonDocument doc = JsonDocument.Parse(payload!);

            JsonElement messages;
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                messages = doc.RootElement;
            }
            else if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("messages", out JsonElement msgElement))
            {
                messages = msgElement;
            }
            else
            {
                return sb.AppendLine("Payload is not in a recognized ChatResponse format.").ToString();
            }

            if (messages.ValueKind != JsonValueKind.Array)
            {
                return sb.AppendLine("Messages element is not an array.").ToString();
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
                                sb.AppendLine(jtext);
                            }
                        }
                    }
                }
            }

            sb.AppendLine();
            sb.AppendLine("==================== Ending of ChatResponse ============================");
            return sb.ToString();
        }



        private void Write(string json)
        {
            if (_options.Output == JsonLoggerOutput.Console)
            {
                //      var obj = JObject.Parse(json);
                //     var text = obj["text"]?.Value<string>();

                lock (Console.Out)
                {


                    Console.Write(json); // Validate JSON



                }


            }
            else if (_options.Output == JsonLoggerOutput.File && !string.IsNullOrEmpty(_options.FilePath))
            {
                lock (_options.FileLock)
                {
                    string path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    Directory.CreateDirectory(path);
                    string filePath = Path.Combine(path, $"SentinelCore_{DateTime.Now:yyyyMMdd}.log");
                    // Ensure directory exists

                    File.AppendAllText(filePath, json + Environment.NewLine, Encoding.UTF8);
                }
            }
            else if (_options.Output == JsonLoggerOutput.Stream && _options.Stream != null)
            {
                lock (_options.Stream)
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(json + Environment.NewLine);
                    _options.Stream.Write(bytes, 0, bytes.Length);
                    _options.Stream.Flush();
                }
            }
        }
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
        if ((trimmed.StartsWith("{") && trimmed.EndsWith("}")) ||
            (trimmed.StartsWith("[") && trimmed.EndsWith("]")))
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(trimmed);
                return JsonSerializer.Serialize(
                        doc.RootElement,
                        new JsonSerializerOptions { WriteIndented = true });
            }
            catch (JsonException)
            {
                // Not valid JSON – fall back to original.
            }
        }
        return maybeJson;
    }

    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();








        public void Dispose()
        {
        }
    }





    private sealed class LogEnvelope
    {
        public string Category { get; set; } = string.Empty;
        public int EventId { get; set; }
        public string? Exception { get; set; }
        public string Level { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public IReadOnlyList<object> Scopes { get; set; } = Array.Empty<object>();
        public object State { get; set; } = string.Empty;
        public DateTimeOffset Timestamp { get; set; }
    }
}





public sealed class JsonLoggerOptions
{
    internal object FileLock { get; } = new();

    // For file output
    public string? FilePath { get; set; }
    public bool Indented { get; set; } = true;
    public LogLevel MinimumLevel { get; set; } = LogLevel.Trace;

    public JsonLoggerOutput Output { get; set; } = JsonLoggerOutput.Console;

    // For custom stream output
    public Stream? Stream { get; set; }
}





public enum JsonLoggerOutput
{
    Console, File, Stream
}
