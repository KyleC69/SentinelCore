// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         JsonLoggerProvider.cs
// Author: Kyle L. Crowder
// Build Num:  081312



using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Logging;




namespace SentinelCore.Infrastructure;





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

            LogEnvelope envelope = new()
            {
                    Timestamp = DateTimeOffset.Now,
                    Level = logLevel.ToString(),
                    Category = _category,
                    EventId = eventId.Id,
                    Message = formatter(state, exception!),
                    Exception = exception?.ToString(),
                    Scopes = scopes
            };

            string json = JsonSerializer.Serialize(envelope, new JsonSerializerOptions { WriteIndented = _options.Indented, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });

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