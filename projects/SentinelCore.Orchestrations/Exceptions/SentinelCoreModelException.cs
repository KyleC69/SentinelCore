// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         SentinelCoreModelException.cs
// Author: Kyle L. Crowder
// Build Num:  080801



namespace SentinelCore.Exceptions;





/// <summary>
///     Represents an exception that can be directly attributed to the model itself.
/// </summary>
public class SentinelCoreModelException : Exception
{
    public SentinelCoreModelException()
    {
    }








    /// <summary>
    ///     Initializes a new instance of the <see cref="SentinelCoreModelException" /> class.
    /// </summary>
    /// <param name="modelErrorMessage">The message that describes the error related to the model.</param>
    public SentinelCoreModelException(string modelErrorMessage) : base(modelErrorMessage)
    {
    }








    /// <summary>
    ///     Initializes a new instance of the <see cref="SentinelCoreModelException" /> class.
    /// </summary>
    /// <param name="modelErrorMessage">The message that describes the error related to the model.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public SentinelCoreModelException(string modelErrorMessage, Exception innerException) : base(modelErrorMessage, innerException)
    {
    }
}