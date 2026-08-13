// Solution: SentinelCore
// Project:   SentinelCore.Contracts
// File:         SystemReporter.cs
// Author: Kyle L. Crowder
// Build Num:  081312



using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.Logging;

using SentinelCore.Events;




namespace SentinelCore.Abstractions;





/// <summary>
///     Default implementation of <see cref="ISystemReporter" />.
///     Class is used to aggregate output from SentinelCore and distribute it to listeners, in what ever form the
///     implementer
///     has configured, Event system, ILoggers, file etc.
/// </summary>
public sealed class SystemReporter : ISystemReporter
{
    private readonly ILogger<SystemReporter> _logger;
    private readonly ISentinelCoreEvents _publisher;








    /// <summary>
    ///     Initializes a new instance of the <see cref="SystemReporter" /> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="publisher">The SentinelCore event hub.</param>
    public SystemReporter([NotNull] ILogger<SystemReporter> logger, [NotNull] ISentinelCoreEvents publisher)
    {
        _logger = logger ?? Throw.IfNull(logger);
        _publisher = publisher ?? Throw.IfNull(publisher);

    }








    /// <summary>
    /// </summary>
    /// <param name="v"></param>
    public void DebugMsg(string v)
    {
        _logger.LogDebug(v);


    }








    /// <summary>
    ///     Reports an error to the logging pipeline and the host UI event stream.
    /// </summary>
    /// <param name="ex">The exception that occurred.</param>
    /// <param name="message">An optional descriptive message.</param>
    public void ReportError(Exception? ex, string? message = null)
    {
        // Guard against a null exception; log a generic message if ex is null.
        if (ex is null)
        {
            _logger.LogError(message ?? "An error occurred.");
            _publisher.RaiseError(message ?? "An error occurred.", new Exception(message ?? "An error occurred."));
        }
        else
        {
            _logger.LogError(ex, "{Message}", message ?? ex.Message);
            _publisher.RaiseError(message ?? ex.Message, ex);
        }
    }








    /// <summary>
    ///     Reports an informational message to the logging pipeline and the host UI event stream.
    /// </summary>
    /// <param name="message">The informational message.</param>
    public void ReportInfo(string message)
    {
        _logger.LogInformation(message);
        _publisher.RaiseSentinelOutputEvent(new SentinelOutputEventArgs("System", message, ActivityType.System));
    }








    /// <summary>
    ///     Reports a warning to the logging pipeline and the host UI event stream.
    /// </summary>
    /// <param name="message">The warning message.</param>
    /// <param name="ex">An optional exception associated with the warning.</param>
    public void ReportWarning(string message, Exception? ex = null)
    {
        _logger.LogWarning(ex, "{Message}", message);
        _publisher.RaiseSentinelOutputEvent(new SentinelOutputEventArgs("System", message, ActivityType.System));
    }
}