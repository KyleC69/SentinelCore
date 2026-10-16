using System;

namespace SentinelCore.CaseFlowEngine.CaseFlow;

/// <summary>
///     Base exception for all errors occurring within the Case Flow Engine.
/// </summary>
public class CaseFlowException : Exception
{
    public CaseFlowException() : base() { }
    public CaseFlowException(string message) : base(message) { }
    public CaseFlowException(string message, Exception innerException) : base(message, innerException) { }
}
