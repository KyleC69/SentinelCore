// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         JsonLoggerProvider.cs
// Author: Kyle L. Crowder
// Build Num:  100908



using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

using Microsoft.Extensions.Logging;




namespace SentinelCore.Orchestrations.Infrastructure;





public sealed class JsonLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    private readonly JsonLoggerOptions _options;
    private IExternalScopeProvider? _scopeProvider;








    public JsonLoggerProvider(JsonLoggerOptions options = null!)
    {
        _options = options ?? new JsonLoggerOptions();
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






        // Method formats a json string to be more human-readable with indentation and line breaks.
        //Helper made for formatting json data properties that are attached to parent objects
        private string PrettyJson(string json)
        {
            // Directly attempt to parse the JSON envelope. If parsing fails (e.g., due to stray control chars),
            // fall back to the original string to avoid corrupting already‑valid JSON.
            try
            {
                using JsonDocument doc = JsonDocument.Parse(json);
                return JsonSerializer.Serialize(
                    doc.RootElement,
                    new JsonSerializerOptions { WriteIndented = true });
            }
            catch (JsonException)
            {
                return json;
            }
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

            // Safely format the message – if it contains a JSON string we pretty‑print it,
            // otherwise we keep the original text.
            string rawMessage = formatter(state, exception!);
            //  string formattedMessage = TryPrettyIfJson(rawMessage);

            LogEnvelope envelope = new()
            {
                Timestamp = DateTimeOffset.Now,
                Level = logLevel.ToString(),
                Category = _category,
                State = state,
                EventId = eventId.Id,
                Message = rawMessage,
                Exception = exception?.ToString(),
                Scopes = scopes
            };

            string json = JsonSerializer.Serialize(envelope, new JsonSerializerOptions { WriteIndented = _options.Indented, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });

            Write(json);
        }








        private void Write(string json)
        {

            if (_options.Output == JsonLoggerOutput.Console)
            {
                lock (Console.Out)
                {
                    Console.WriteLine(json);
                }
            }
            else if (_options.Output == JsonLoggerOutput.File)
            {
                // Determine the target file path. If a custom FilePath is supplied, use it; otherwise fall back to the default location.
                var logFilePath = !string.IsNullOrWhiteSpace(_options.FilePath)
                    ? _options.FilePath
                    : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SentinelCore", $"SentinelCore_{DateTime.Now:yyyyMMdd}.log");

                lock (_options.FileLock)
                {
                    // Ensure the directory for the log file exists.
                    var directory = Path.GetDirectoryName(logFilePath);
                    if (!string.IsNullOrEmpty(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    // Append the formatted JSON entry.
                    File.AppendAllText(logFilePath, json + Environment.NewLine, Encoding.UTF8);
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

    public JsonLoggerOutput Output { get; set; } = JsonLoggerOutput.File;

    // For custom stream output
    public Stream? Stream { get; set; }
}




public enum JsonLoggerOutput
{
    Console, File, Stream
}