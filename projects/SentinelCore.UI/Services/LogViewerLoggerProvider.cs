// Solution: SentinelCore
// Project:   SentinelCore.UI
// File:         LogViewerLoggerProvider.cs
// Author: Kyle L. Crowder
// Build Num:  110813



using System.Collections.Concurrent;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;




namespace SentinelCore.UI.Services;





/// <summary>
///     A logger provider that forwards raw markdown log output to the <see cref="Views.LogViewerWindow" />.
///     Passes log data unmutated from the console logger to the markdown viewer.
/// </summary>
public sealed class LogViewerLoggerProvider : ILoggerProvider
{

    private readonly ConcurrentDictionary<string, LogViewerLogger> _loggers = new();








    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, name => new LogViewerLogger(name));
    }








    /// <inheritdoc />
    public void Dispose()
    {
        _loggers.Clear();
    }








    /// <summary>
    ///     Event raised when a raw markdown log entry is received.
    /// </summary>
    public static event Action<string>? LogEntryReceived;





    /// <summary>
    ///     Logger implementation that forwards raw markdown to the LogViewerWindow.
    /// </summary>
    private sealed class LogViewerLogger : ILogger
    {
        private readonly string _categoryName;








        public LogViewerLogger(string categoryName)
        {
            _categoryName = categoryName;
        }








        /// <inheritdoc />
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;








        /// <inheritdoc />
        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;








        /// <inheritdoc />
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            string message = formatter(state, exception);
            string exceptionText = exception?.ToString() ?? string.Empty;

            // Build raw markdown exactly as it comes from the console logger - no mutation
            string markdown = $"`{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}` **[{_categoryName}]** `{logLevel}` {message}";

            if (!string.IsNullOrEmpty(exceptionText))
            {
                markdown += $"\n\n```text\n{exceptionText}\n```";
            }

            // Raise event with raw markdown
            LogEntryReceived?.Invoke(markdown);
        }
    }
}





/// <summary>
///     Extension methods for adding the LogViewerLoggerProvider to the logging builder.
/// </summary>
public static class LogViewerLoggerExtensions
{
    /// <summary>
    ///     Adds the LogViewerLoggerProvider to the logging builder.
    /// </summary>
    public static ILoggingBuilder AddLogViewer(this ILoggingBuilder builder)
    {
        builder.Services.AddSingleton<ILoggerProvider, LogViewerLoggerProvider>();
        return builder;
    }
}