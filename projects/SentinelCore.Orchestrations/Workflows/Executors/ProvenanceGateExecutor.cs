// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         Workflows\Executors\ProvenanceGateExecutor.cs

using SentinelCore.Contracts.Abstractions;

using SentinelCore.Security;
using SentinelCore.Security.Quarantine;

namespace SentinelCore.Orchestrations.Workflows.Executors;





/// <summary>
///     Outermost workflow gate: validates the provenance of every message entering the
///     The Core workflow before any executor — and therefore any model — sees it.
///     Validated messages pass through unchanged. Rejected messages are quarantined
///     unchanged, a rejection notice is yielded to the UI, and no further executor runs.
///     Fail-closed by design.
/// </summary>
[YieldsOutput(typeof(ChatMessage))] // ← MANDATORY: declare output type for compile-time validation
public sealed partial class ProvenanceGateExecutor : Executor
{
    private readonly ISystemReporter _reporter;
    private readonly ISentinelProvenanceService _provenanceService;
    private readonly IQuarantineStore _quarantineStore;





    /// <summary>
    ///     Initializes a new instance of the <see cref="ProvenanceGateExecutor" /> class.
    /// </summary>
    /// <param name="reporter">The system reporter for logging and event publishing.</param>
    /// <param name="provenanceService">The provenance service performing entry-point validation.</param>
    /// <param name="quarantineStore">The store receiving rejected messages, unchanged.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is <c>null</c>.</exception>
    public ProvenanceGateExecutor(ISystemReporter reporter, ISentinelProvenanceService provenanceService, IQuarantineStore quarantineStore) : base("ProvenanceGateExecutor")
    {
        _reporter = reporter ?? throw new ArgumentNullException(nameof(reporter));
        _provenanceService = provenanceService ?? throw new ArgumentNullException(nameof(provenanceService));
        _quarantineStore = quarantineStore ?? throw new ArgumentNullException(nameof(quarantineStore));
        Name = Id;
    }







    /// <summary>
    ///     Gets the human-readable name of this executor, used in log messages and diagnostics.
    /// </summary>
    public string Name { get; }







    /// <summary>
    ///     Main executor entry point called by the MAF dispatcher. Validates provenance,
    ///     quarantines rejected messages, and yields a user-visible rejection notice.
    /// </summary>
    /// <param name="message">The input message to process.</param>
    /// <param name="context">The workflow context for output yielding.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The validated message, or <c>null</c> when the message was rejected.</returns>
    [MessageHandler]
    public async ValueTask<ChatMessage?> HandleChatMessageAsync(ChatMessage message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        // --- Status: Executor start ---
        _reporter.ReportInfo($"[{Name}] Validating message provenance.");

        // --- Null validation ---
        if (message is null)
        {
            _reporter.ReportError($"[{Name}] Input message was null.");
            await context.YieldOutputAsync(new ChatMessage(ChatRole.Assistant, $"[{Name}] Input message was null."), cancellationToken).ConfigureAwait(false);
            return null;
        }

        try
        {
            ChatMessage? outcome = ProcessMessage(message);

            if (outcome is null)
            {
                await context.YieldOutputAsync(new ChatMessage(ChatRole.Assistant, "⚠️ Message rejected: provenance validation failed. The message has been quarantined."), cancellationToken).ConfigureAwait(false);
            }
            else
            {
                _reporter.ReportInfo($"[{Name}] Provenance validated. Forwarding message to the safety gate.");
            }

            return outcome;
        }
        catch (OperationCanceledException)
        {
            // Cooperative cancellation must propagate — never swallow it.
            _reporter.ReportInfo($"[{Name}] Execution canceled.");
            throw;
        }
        catch (Exception ex)
        {
            _reporter.ReportError($"[{Name}] Exception: {ex.Message}", ex);

            // Yield a user-visible message
            await context.YieldOutputAsync(new ChatMessage(ChatRole.Assistant, $"⚠️ An internal error occurred in {Name}: {ex.Message}"), cancellationToken).ConfigureAwait(false);

            return null;
        }
    }







    /// <summary>
    ///     Domain logic: validates provenance and quarantines rejected messages.
    ///     Validation itself throwing is treated as a failure — the message is quarantined
    ///     and never forwarded (fail-closed).
    /// </summary>
    /// <param name="message">The message to validate.</param>
    /// <returns>The validated message, or <c>null</c> when the message was rejected.</returns>
    private ChatMessage? ProcessMessage(ChatMessage message)
    {
        ProvenanceValidationResult result;

        try
        {
            result = _provenanceService.Validate(message);
        }
        catch (Exception ex)
        {
            _quarantineStore.Quarantine(message, ProvenanceFailure.InternalError, $"Validation threw: {ex.Message}");
            _reporter.ReportError($"[{Name}] Provenance validation threw; message quarantined. {ex.Message}", ex);
            return null;
        }

        if (result.IsValid)
        {
            return message;
        }

        // The quarantine store reports the event (reason + detail) through the system
        // reporter — a second report here would duplicate the record.
        _quarantineStore.Quarantine(message, result.Failure, result.Detail);
        return null;
    }

}
