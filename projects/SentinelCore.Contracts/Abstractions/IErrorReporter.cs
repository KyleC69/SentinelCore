// Solution: SentinelCore
// Project:   SentinelCore.Contracts
// File:         IErrorReporter.cs
// Author: Kyle L. Crowder
// Build Num:  080801



namespace SentinelCore.Abstractions;





/// <summary>
///     Reports errors through logging and the SentinelCore event hub.
/// </summary>
public interface ISystemReporter
{
    void DebugMsg(string v);








    /// <summary>
    ///     Reports an error to the logging pipeline and the host UI event stream.
    /// </summary>
    /// <param name="ex">The exception that occurred.</param>
    /// <param name="message">An optional descriptive message.</param>
    void ReportError(Exception? ex, string? message = null);








    /// <summary>
    ///     Reports an informational message to the logging pipeline and the host UI event stream.
    /// </summary>
    /// <param name="message">The informational message.</param>
    void ReportInfo(string message);








    /// <summary>
    ///     Reports a warning to the logging pipeline and the host UI event stream.
    /// </summary>
    /// <param name="message">The warning message.</param>
    /// <param name="ex">An optional exception associated with the warning.</param>
    void ReportWarning(string message, Exception? ex = null);
}