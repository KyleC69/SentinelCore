// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         SentinelCoreExecutionException.cs
// Author: Kyle L. Crowder
// Build Num:  091418

namespace SentinelCore.Orchestrations.Exceptions;





/// <summary>
///     Exception thrown when a Sentinel core execution error occurs.
/// </summary>
/// <remarks>
///     Contains an error message and an inner exception to preserve the original failure details for
///     diagnostics and logging.
/// </remarks>
public sealed class SentinelCoreExecutionException : Exception
{
    public SentinelCoreExecutionException(string message, Exception innerException) : base(message, innerException)
    {
    }
}