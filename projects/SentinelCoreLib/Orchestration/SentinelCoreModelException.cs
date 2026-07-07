// Solution: SentinelCoreLib
// Project:   SentinelCoreLib
// File:         SentinelCoreModelException.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



namespace SentinelCoreLib.Orchestration;





internal class SentinelCoreModelException : Exception
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SentinelCoreModelException" /> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public SentinelCoreModelException(string message) : base(message)
    {
    }








    /// <summary>
    ///     Initializes a new instance of the <see cref="SentinelCoreModelException" /> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public SentinelCoreModelException(string message, Exception innerException) : base(message, innerException)
    {
    }
}