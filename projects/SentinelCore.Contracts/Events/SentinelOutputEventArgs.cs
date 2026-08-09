// Solution: SentinelCore
// Project:   SentinelCore.Contracts
// File:         SentinelOutputEventArgs.cs
// Author: Kyle L. Crowder
// Build Num:  080801



namespace SentinelCore.Events;





/// <summary>
///     Unified output event payload for all normal agent/tool/workflow activity
///     flowing from the SentinelCore library to the host UI.
///     Uses a single event with an <see cref="ActivityType" /> discriminator
///     to tell the UI which category the payload belongs to.
/// </summary>
/// <param name="AgentName">The name of the agent or component that produced the output.</param>
/// <param name="Message">A human-readable description of the output event.</param>
/// <param name="ActivityType">The category of activity being reported.</param>
public sealed record SentinelOutputEventArgs(string AgentName, string Message, ActivityType ActivityType);