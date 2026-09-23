// Solution: SentinelCore
// Project:   SentinelCore.UI
// File:         FileLoggerServiceStore.cs
// Author: Kyle L. Crowder
// Build Num:  092308



using System.IO;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;




namespace SentinelCore.UI.Services;





internal class FileLoggerServiceStore : ILogger
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





internal class FileLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new FileLoggerServiceStore();








    public void Dispose()
    {
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