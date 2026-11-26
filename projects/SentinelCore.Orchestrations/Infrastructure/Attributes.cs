// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         Attributes.cs
// Author: Kyle L. Crowder
// Build Num:  112518



namespace SentinelCore;





/// <summary>
///     Attribute indicates member must not exist in production
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Property)]
public sealed class TemporaryAttribute : Attribute
{

    public TemporaryAttribute(string reason)
    {
        Reason = reason;
    }








    public string Reason { get; }
}