// Solution: SentinelCore
// Project:   SentinelCore.UI
// File:         FileLoggerServiceStore.cs
// Author: Kyle L. Crowder
// Build Num:  100908



using System.IO;
using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;




namespace SentinelCore.UI.Services;





internal sealed class FileLoggerServiceStore : ILogger
{
    private readonly string _category;








    public FileLoggerServiceStore(string categoryName)
    {
        _category = categoryName;
    }








    private static readonly object Lock = new();

    private static readonly string LogFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SentinelCore", $"SentinelCore_{DateTime.Now:yyyyMMdd_HHmmss}.log");

    private static readonly Lazy<StreamWriter> _logWriter = new Lazy<StreamWriter>(InitializeLogWriter);








    private static StreamWriter InitializeLogWriter()
    {

        string directory = Path.GetDirectoryName(LogFilePath)!;
        Directory.CreateDirectory(directory);
        StreamWriter writer = new StreamWriter(LogFilePath, append: true);
        writer.AutoFlush = true;
        return writer;
    }








    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;


    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;








    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        bool isAI = _category.Contains("AI", StringComparison.OrdinalIgnoreCase);
        string text;

        if (isAI && state is System.Collections.Generic.IDictionary<string, object> dict)
        {
            // Check for GetStreamingResponseAsync update using KVPs in the state
            if (dict.TryGetValue("Method", out object methodObj) && methodObj is string method && method.Equals("GetStreamingResponseAsync", StringComparison.OrdinalIgnoreCase) && dict.TryGetValue("IsUpdate", out object isUpdateObj) && isUpdateObj is bool isUpdate && isUpdate)
            {
                return; // Skip logging this update
            }

            // Serialize the dictionary to JSON
            try
            {
                string json = JsonSerializer.Serialize(dict);
                try
                {
                    // Try to parse as JSON and re-serialize with indentation
                    using JsonDocument doc = JsonDocument.Parse(json);
                    text = JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true });
                }
                catch (JsonException)
                {
                    // If not valid JSON, use the serialized string
                    text = json;
                }
            }
            catch (Exception)
            {
                // Fall back to formatter if serialization fails
                text = formatter(state, exception);
            }
        }
        else
        {
            // For non-AI logs or when state is not a dictionary, use the formatter
            text = formatter(state, exception);
        }

        // Apply common formatting for AI logs (replace escape sequences)
        if (isAI)
        {
            text = text.Replace("\\r\\n", Environment.NewLine);
            text = text.Replace("\\n", Environment.NewLine);
            text = text.Replace("\\r", Environment.NewLine);
        }

        WriteLog(logLevel, text);
    }








    // Method formats a json string to be more human-readable with indentation and line breaks.
    //Helper made for formatting json data properties that are attached to parent objects
    private string PrettyJson(string json)
    {
        // If the input is not a JSON string, just return it as-is.
        try
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (JsonException)
        {
            // Not valid JSON, return the original string.
            return json;
        }
    }








    private void WriteLog(LogLevel logLevel, string message)
    {
        lock (Lock)
        {
            StreamWriter writer = _logWriter.Value;
            writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{logLevel}] {message}");
        }
    }








    public static void Dispose()
    {
        if (_logWriter.IsValueCreated)
        {
            _logWriter.Value.Dispose();
        }
    }
}





internal sealed class FileLoggerProvider : ILoggerProvider
{
    private string _category;








    public ILogger CreateLogger(string categoryName)
    {
        return new FileLoggerServiceStore(categoryName);
    }








    public void Dispose()
    {
        FileLoggerServiceStore.Dispose();
    }
}





public static class FileLoggerExtensions
{
    public static ILoggingBuilder AddFileLogger(this ILoggingBuilder builder)
    {
        builder.Services.AddSingleton<ILoggerProvider, FileLoggerProvider>();
        return builder;
    }
}
