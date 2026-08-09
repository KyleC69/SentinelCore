// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         SafetyEngineOptions.cs
// Author: Kyle L. Crowder
// Build Num:  080801



namespace SentinelCore.SafetyEngine;





/// <summary>
///     Configuration options for the <see cref="SafetyEngineAgent" />.
/// </summary>
public sealed class SafetyEngineOptions
{

    /// <summary>
    ///     Optional custom message to return when a prompt is blocked.
    ///     If <c>null</c>, a default message including the evaluation summary is used.
    /// </summary>
    public string? BlockedResponseMessage { get; init; }

    /// <summary>
    ///     Default options with safe defaults.
    /// </summary>
    public static SafetyEngineOptions Default { get; } = new();

    /// <summary>
    ///     Whether to stop evaluating rules as soon as one returns <see cref="SafetyAction.Block" />.
    ///     Default is <c>true</c>.
    /// </summary>
    public bool StopOnFirstBlock { get; init; } = true;

    /// <summary>
    ///     Whether to treat rule evaluation errors as blocks.
    ///     When <c>true</c>, if a rule throws an exception, the prompt is blocked.
    ///     When <c>false</c>, the exception is logged and a warning is recorded.
    ///     Default is <c>true</c> (fail-safe).
    /// </summary>
    public bool TreatRuleErrorsAsBlocks { get; init; } = true;
}