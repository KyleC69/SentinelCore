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
    private static readonly object Lock = new();

    private static readonly string LogFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SentinelCore", "SentinelCore.log");


    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;


    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;








    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;
        string message = formatter(state, exception);
        WriteLog(logLevel, message);
    }







    // Method formats a json string to be more human-readable with indentation and line breaks.
    //Helper made for formatting json data properties that are attached to parent objects
    private string PrettyJson(string json)
    {
        // If the input is not a JSON string, just return it as-is.
        try
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(
                doc.RootElement,
                new JsonSerializerOptions { WriteIndented = true });
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
            Directory.CreateDirectory(Path.GetDirectoryName(LogFilePath)!);
            using StreamWriter streamWriter = new(LogFilePath, true);
            streamWriter.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{logLevel}] {message}");
        }
    }
}





internal sealed class FileLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new FileLoggerServiceStore();








    public void Dispose()
    {
        // No resources to dispose in the current implementation.
        // This method is provided for future extensibility.
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