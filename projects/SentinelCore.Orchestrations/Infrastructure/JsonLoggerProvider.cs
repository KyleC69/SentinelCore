// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         JsonLoggerProvider.cs
// Author: Kyle L. Crowder
// Build Num:  100416



using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

using JsonException = System.Text.Json.JsonException;
using JsonSerializer = System.Text.Json.JsonSerializer;




namespace SentinelCore.Orchestrations.Infrastructure;





public sealed class JsonLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    private readonly JsonLoggerOptions _options;
    private IExternalScopeProvider? _scopeProvider;
    private readonly CustomMAFFormatter _formatter;








    public JsonLoggerProvider(JsonLoggerOptions options = null!, CustomMAFFormatter formatter = null!)
    {
        _options = options ?? new JsonLoggerOptions();
        _formatter = formatter ?? new CustomMAFFormatter();

    }








    public ILogger CreateLogger(string categoryName)
    {
        return new JsonLogger(categoryName, _options, () => _scopeProvider ?? new LoggerExternalScopeProvider(), _formatter);
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
        private readonly CustomMAFFormatter _formatter;








        public JsonLogger(string category, JsonLoggerOptions options, Func<IExternalScopeProvider> scopeProviderAccessor, CustomMAFFormatter formatter)
        {
            _category = category;
            _options = options;
            _scopeProviderAccessor = scopeProviderAccessor;
            _formatter = formatter;
        }








        IDisposable ILogger.BeginScope<TState>(TState state)
        {
            return _scopeProviderAccessor()?.Push(state) ?? NullScope.Instance;
        }








        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= _options.MinimumLevel;
        }








        /// <summary>Writes a log entry.</summary>
        /// <param name="logLevel">Entry will be written on this level.</param>
        /// <param name="eventId">Id of the event.</param>
        /// <param name="state">The entry to be written. Can be also an object.</param>
        /// <param name="exception">The exception related to this entry.</param>
        /// <param name="formatter">Function to create a <see cref="T:System.String" /> message of the <paramref name="state" /> and <paramref name="exception" />.</param>
        /// <typeparam name="TState">The type of the object to be written.</typeparam>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            IExternalScopeProvider? scopeProvider = _scopeProviderAccessor();

            List<object> scopes = new();
            scopeProvider?.ForEachScope((scope, list) => { list.Add(scope!); }, scopes);

            // Use CustomMAFFormatter for special AI formatting
            string formattedMessage = _formatter.Format(logLevel, _category, eventId, state, exception, formatter);

            LogEnvelope envelope = new()
            {
                Timestamp = DateTimeOffset.Now,
                Level = logLevel.ToString(),
                Category = _category,
                EventId = eventId.Id,
                Message = formattedMessage,
                Exception = exception?.ToString(),
                Scopes = scopes
            };


            //  string json = JsonConvert.SerializeObject(envelope, new JsonSerializerSettings { Formatting = _options.Indented ? Formatting.Indented : Formatting.None });

            Write(formattedMessage);
        }








        private void Write(string json)
        {
            if (_options.Output == JsonLoggerOutput.Console)
            {
                lock (Console.Out)
                {
                    Console.Write(json);
                }
            }
            else if (_options.Output == JsonLoggerOutput.File && !string.IsNullOrEmpty(_options.FilePath))
            {
                lock (_options.FileLock)
                {
                    string path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    Directory.CreateDirectory(path);
                    string filePath = Path.Combine(path, $"SentinelCore_{DateTime.Now:yyyyMMdd}.log");
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





public sealed class JsonLoggerOptions : ConsoleFormatterOptions
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
