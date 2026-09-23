// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         SafetyEngineInternalException.cs
// Author: Kyle L. Crowder
// Build Num:  092308



namespace SentinelCore.Orchestrations.Exceptions;





public class SafetyEngineInternalException : Exception
{



    public SafetyEngineInternalException(string message, Exception innerException) : base(message, innerException)
    {
    }








    public Dictionary<string, object> ContextData { get; } = new Dictionary<string, object>();








    /// <summary>
    ///     Populates the exception with detailed tracing information for monitoring.
    /// </summary>
    public void AddContext(string key, object value)
    {
        ContextData[key] = value;
    }
}