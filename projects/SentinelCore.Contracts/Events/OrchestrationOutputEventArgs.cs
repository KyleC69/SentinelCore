// Solution: SentinelCore
// Project:   SentinelCore.Contracts
// File:         OrchestrationOutputEventArgs.cs
// Author: Kyle L. Crowder
// Build Num:  080801



namespace SentinelCore.Events;





/// <summary>
///     Event arguments for orchestration lifecycle events.
/// </summary>
/// <param name="Message">A human-readable description of the orchestration event.</param>
/// <param name="Source">The name of the orchestration type that raised the event.</param>
public sealed record OrchestrationActivityArgs(string Message, string Source);