// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         SentinelCorePlatformException.cs
// Author: Kyle L. Crowder
// Build Num:  081312



namespace SentinelCore.Exceptions;





/// <summary>
///     Represents a fatal error that occurs from operations from a supporting component of the SentinelCore platform.
///     This exception is intended to be used for critical failures that prevent the platform from functioning correctly
///     and may require immediate attention or intervention.
/// </summary>
public class SentinelCorePlatformException : Exception
{
    public SentinelCorePlatformException()
    {
    }








    public SentinelCorePlatformException(string fatalPlatformErrorMessage) : base(fatalPlatformErrorMessage)
    {
    }








    public SentinelCorePlatformException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}