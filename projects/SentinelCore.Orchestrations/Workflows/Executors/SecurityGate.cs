// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         TheCoreSwitchExec.cs
// Author: Kyle L. Crowder
// Build Num:  112518



using SentinelCore.Contracts.Abstractions;
using SentinelCore.Orchestrations.Models;
using SentinelCore.Security;
using SentinelCore.Security.Quarantine;
using SentinelCore.Security.Sources;




namespace SentinelCore.Orchestrations.Workflows.Executors;





/// <summary>
///     Model-boundary provenance re-check for The Core graph. Runs after the safety, pattern, and whitelist filters
///     and immediately before routing, re-verifying message integrity (PL-10) without consuming the nonce — the entry
///     gate already consumed it. Validated messages are forwarded unchanged; rejected messages are quarantined
///     unchanged, a rejection notice is yielded to the UI, and <see langword="null" /> is returned so no outgoing
///     conditional edge matches and the message dead-ends. Fail-closed by design.
/// </summary>
/// <remarks>
///     Routing is not performed here: <see cref="ChatTypeRouterExecutor" /> owns the deterministic chat-type decision
///     so this executor remains a single-responsibility security check with no model contact.
/// </remarks>
[YieldsOutput(typeof(ChatMessage))] // ← MANDATORY: declare output type for compile-time validation
public sealed partial class SecurityGate : Executor
{
    private readonly ISentinelProvenanceService _provenanceService;
    private readonly IQuarantineStore _quarantineStore;
    private readonly ISystemReporter _reporter;








    /// <summary>
    ///     Initializes a new instance of the <see cref="TheCoreSwitchExec" /> class.
    /// </summary>
    /// <param name="reporter">The system reporter for logging and event publishing.</param>
    /// <param name="provenanceService">The provenance service for model-boundary integrity re-validation.</param>
    /// <param name="quarantineStore">The store receiving messages that fail re-validation, unchanged.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is <see langword="null" />.</exception>
    public SecurityGate(ISystemReporter reporter, ISentinelProvenanceService provenanceService, IQuarantineStore quarantineStore) : base("SecurityGate", ExecutorOutputPolicy.CreateExplicitYieldsOnly())
    {
        _reporter = reporter ?? throw new ArgumentNullException(nameof(reporter));
        _provenanceService = provenanceService ?? throw new ArgumentNullException(nameof(provenanceService));
        _quarantineStore = quarantineStore ?? throw new ArgumentNullException(nameof(quarantineStore));
        Name = Id;
    }








    /// <summary>
    ///     Gets the human-readable name of this executor, used in log messages and diagnostics.
    /// </summary>
    public string Name { get; init; }








    /// <summary>
    ///     Handles incoming chat messages by re-validating their provenance and forwarding them if valid.
    /// </summary>
    /// <param name="message">The chat message to process. Can be <see langword="null" />.</param>
    /// <param name="context">The workflow context used for yielding outputs.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    ///     The validated <see cref="ChatMessage" />, or <see langword="null" /> if the message was rejected or an error
    ///     occurred.
    /// </returns>
    /// <remarks>
    ///     This method ensures integrity by re-checking the provenance of the message before forwarding it.
    ///     If the message is invalid or an exception occurs, appropriate error handling is performed,
    ///     including reporting the issue and yielding an error message to the workflow context.
    /// </remarks>
    [MessageHandler]
    public async ValueTask<ChatMessage?> HandleChatMessageAsync(ChatMessage? message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        // --- Status: Executor start ---
        _reporter.ReportInfo($"[{Name}] Starting execution. Input type: {nameof(ChatMessage)}");

        // --- Null validation ---
        if (message is null)
        {
            _reporter.ReportError($"[{Name}] Input message was null.");
            await context.YieldOutputAsync(new ChatMessage(ChatRole.Assistant, $"[{Name}] Input message was null.").WithMessageSource(SentinelSources.Internal), cancellationToken).ConfigureAwait(false);
            return null;
        }

        try
        {
            // --- Defense in depth: integrity re-check before routing and any model contact.
            // The entry gate already consumed the nonce, so calling Validate here would reject every
            // message as a replay — only the non-consuming integrity check runs. ---
            if (!ValidateIntegrity(message))
            {
                await context.YieldOutputAsync(new ChatMessage(ChatRole.Assistant, "⚠️ Message rejected: provenance validation failed. The message has been quarantined.").WithMessageSource(SentinelSources.Internal), cancellationToken).ConfigureAwait(false);
                return null;
            }

            _reporter.ReportInfo($"[{Name}] Integrity re-check passed. Forwarding message to the chat-type router.");

            return message;
        }
        catch (OperationCanceledException)
        {
            // Cooperative cancellation must propagate — never swallow it.
            _reporter.ReportInfo($"[{Name}] Execution canceled.");
            throw;
        }
        catch (Exception ex)
        {
            // --- Robust error handling: report, surface to the user, and dead-end ---
            _reporter.ReportError($"[{Name}] Exception: {ex.Message}", ex);

            await context.YieldOutputAsync(new ChatMessage(ChatRole.Assistant, $"⚠️ An internal error occurred in {Name}: {ex.Message}").WithMessageSource(SentinelSources.Internal), cancellationToken).ConfigureAwait(false);

            _reporter.ReportInfo($"[{Name}] Message not forwarded due to error.");
            return null;
        }
    }








    /// <summary>
    ///     Defense-in-depth integrity re-check performed immediately before the routing decision
    ///     (and therefore before any model contact). Quarantines the message unchanged when
    ///     validation does not pass; validation throwing is treated as a failure.
    /// </summary>
    /// <param name="message">The message to re-validate.</param>
    /// <returns><c>true</c> when the message may proceed to the model; <c>false</c> when it was quarantined.</returns>
    private bool ValidateIntegrity(ChatMessage message)
    {
        ProvenanceValidationResult integrity;

        try
        {
            integrity = _provenanceService.VerifyIntegrity(message);
        }
        catch (Exception ex)
        {
            _quarantineStore.Quarantine(message, ProvenanceFailure.InternalError, $"Integrity validation threw: {ex.Message}");
            _reporter.ReportError($"[{Name}] Integrity validation threw; message quarantined. {ex.Message}", ex);
            return false;
        }

        if (integrity.IsValid)
        {
            return true;
        }

        _quarantineStore.Quarantine(message, integrity.Failure, integrity.Detail);
        _reporter.ReportError($"[{Name}] Message quarantined at model boundary. Reason: {integrity.Failure}. Detail: {integrity.Detail}");
        return false;
    }
}
