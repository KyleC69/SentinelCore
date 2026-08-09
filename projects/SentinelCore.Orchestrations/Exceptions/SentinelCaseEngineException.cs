// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         SentinelCaseEngineException.cs
// Author: Kyle L. Crowder
// Build Num:  080801



namespace SentinelCore.Exceptions;





/// <summary>
///     Represents errors that occur during the execution of the Sentinel Case Engine.
/// </summary>
public class SentinelCaseEngineException : Exception
{
    public SentinelCaseEngineException()
    {
    }








    public SentinelCaseEngineException(string caseEngineErrorMessage) : base(caseEngineErrorMessage)
    {
    }








    public SentinelCaseEngineException(string caseEngineErrorMessage, Exception innerException) : base(caseEngineErrorMessage, innerException)
    {
    }
}