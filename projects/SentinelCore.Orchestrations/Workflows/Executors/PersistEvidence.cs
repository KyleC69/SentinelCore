// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         PersistEvidence.cs
// Author: Kyle L. Crowder
// Build Num:  091418

using SentinelCore.Contracts.Abstractions;

namespace SentinelCore.Orchestrations.Workflows.Executors;

// TODO: Implement saving the findings to database — stub for now
/// <summary>
///     Executor that persists evidence to the database.
///     Stub — not yet implemented.
/// </summary>
[YieldsOutput(typeof(string))]
public sealed partial class PersistEvidence : Executor
{
    private readonly ISystemReporter _reporter;

    /// <summary>
    ///     Gets the human-readable name of this executor, used in log messages and diagnostics.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="PersistEvidence" /> class.
    /// </summary>
    /// <param name="reporter">The system reporter for logging.</param>
    public PersistEvidence(ISystemReporter reporter) : base("PersistEvidence")
    {
        _reporter = reporter ?? throw new ArgumentNullException(nameof(reporter));
        Name = Id;
    }

    /// <summary>
    ///     Handles evidence persistence.
    /// </summary>
    /// <param name="message">The input message to process.</param>
    /// <param name="context">The workflow context for shared-state updates and output yielding.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The input message, passed through unchanged (stub).</returns>
    [MessageHandler]
    public ValueTask<string> HandleStringAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        // TODO: Implement evidence persistence logic
        _reporter.ReportInfo($"[{Name}] PersistEvidence stub — passing through.");
        return ValueTask.FromResult(message ?? string.Empty);
    }
}