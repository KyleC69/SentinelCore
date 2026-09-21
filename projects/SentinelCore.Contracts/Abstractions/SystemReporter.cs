// Solution: SentinelCore
// Project:   SentinelCore.Contracts
// File:         SystemReporter.cs
// Author: Kyle L. Crowder
// Build Num:  091418



using Microsoft.Extensions.Logging;

using SentinelCore.Contracts.Events;




namespace SentinelCore.Contracts.Abstractions;





/// <summary>
///     Default implementation of <see cref="ISystemReporter" />.
///     Class is used to aggregate output from SentinelCore and distribute it to listeners, in what ever form the
///     implementer
///     has configured, Event system, ILoggers, file etc.
/// </summary>
public sealed class SystemReporter : ISystemReporter
{
    private ILogger _logger;
    private ISentinelCoreEvents _publisher;








    /// <summary>
    ///     Initializes a new instance of the <see cref="SystemReporter" /> class.
    /// </summary>
    /// <param name="factory">The logger factory.</param>
    /// <param name="publisher">The SentinelCore event hub.</param>
    public SystemReporter(ILoggerFactory factory, ISentinelCoreEvents publisher)
    {
        ArgumentNullException.ThrowIfNull(factory);

        _logger = factory.CreateLogger("SystemReporter");
        _publisher = publisher ?? throw new ArgumentException(nameof(publisher));
    }








    /// <summary>
    ///     Logs a debug-level message through the logging pipeline.
    /// </summary>
    /// <param name="message">The debug message to log.</param>
    public void DebugMsg(string message)
    {
        _logger.LogDebug("[DEBUG] {Message}", message);
        _publisher.RaiseSentinelOutputEvent(new SentinelOutputEventArgs("System", message, ActivityType.System));
    }








    /// <summary>
    ///     Reports an error to the logging pipeline and the host UI event stream.
    /// </summary>
    /// <param name="message">A descriptive message.</param>
    /// <param name="ex">The exception that occurred.</param>
    public void ReportError(string? message, Exception? ex = null)
    {
        string finalMessage = message ?? ex?.Message ?? "An unspecified error occurred.";
        _logger.LogError(ex, "[ERROR] {Message}", finalMessage);
        _publisher.RaiseError(finalMessage, ex ?? new Exception(finalMessage));
    }








    /// <summary>
    ///     Reports an informational message to the logging pipeline and the host UI event stream.
    /// </summary>
    /// <param name="message">The informational message.</param>
    public void ReportInfo(string message)
    {
        _logger.LogInformation("[INFO] " + message);
        _publisher.RaiseSentinelOutputEvent(new SentinelOutputEventArgs("System", message, ActivityType.System));
    }








    /// <summary>
    ///     Reports a warning to the logging pipeline and the host UI event stream.
    /// </summary>
    /// <param name="message">The warning message.</param>
    /// <param name="ex">An optional exception associated with the warning.</param>
    public void ReportWarning(string message, Exception? ex = null)
    {
        _logger.LogWarning(ex, "[WARNING] {Message}", message);
        _publisher.RaiseSentinelOutputEvent(new SentinelOutputEventArgs("System", message, ActivityType.System));
    }
}
