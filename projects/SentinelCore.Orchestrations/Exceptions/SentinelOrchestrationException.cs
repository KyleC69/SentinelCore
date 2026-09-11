// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         SentinelOrchestrationException.cs
// Author: Kyle L. Crowder
// Build Num:  091112



namespace SentinelCore.Orchestrations.Exceptions;





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