// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         SentinelOrchestrationException.cs
// Author: Kyle L. Crowder
// Build Num:  080801



namespace SentinelCore.Exceptions;





/// <summary>
///     Represents errors that occur during the execution of the Sentinel Orchestration.
/// </summary>
public class SentinelOrchestrationException : Exception
{
    public SentinelOrchestrationException()
    {
    }








    public SentinelOrchestrationException(string orchestrationErrorMessage) : base(orchestrationErrorMessage)
    {
    }








    public SentinelOrchestrationException(string orchestrationErrorMessage, Exception innerException) : base(orchestrationErrorMessage, innerException)
    {
    }
}