// Solution: SentinelCore
// Project:   SentinelCore.CaseFlowEngine
// File:         CaseFlowException.cs
// Author: Kyle L. Crowder
// Build Num:  110813



namespace SentinelCore.CaseFlowEngine.CaseFlow;





/// <summary>
///     Base exception for all errors occurring within the Case Flow Engine.
/// </summary>
public class CaseFlowException : Exception
{
    public CaseFlowException()
    {
    }








    public CaseFlowException(string message) : base(message)
    {
    }








    public CaseFlowException(string message, Exception innerException) : base(message, innerException)
    {
    }
}