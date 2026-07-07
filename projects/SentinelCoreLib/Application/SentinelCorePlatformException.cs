// Solution: SentinelCoreLib
// Project:   SentinelCoreLib
// File:         SentinelCorePlatformException.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



namespace SentinelCoreLib.Application;





/// <summary>
///     Represents an exception that occurs within the Sentinel Core Platform.
/// </summary>
/// <remarks>
///     This exception is typically thrown to indicate a fatal error during the initialization process
///     or other critical operations within the Sentinel Core Platform.
/// </remarks>
public class SentinelCorePlatformException : Exception
{
    /// <inheritdoc />
    public SentinelCorePlatformException(string fatalErrorDuringInitialization) : base(fatalErrorDuringInitialization)
    {
    }
}