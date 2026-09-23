// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         SafetyImmediateBlockException.cs
// Author: Kyle L. Crowder
// Build Num:  092308



namespace SentinelCore.Orchestrations.Exceptions;





public class SafetyImmediateBlockException : Exception
{
    public SafetyImmediateBlockException(string message) : base(message)
    {
    }
}