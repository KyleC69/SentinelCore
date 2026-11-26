// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         ChatModeRouterExecutor.cs
// Author: Kyle L. Crowder
// Build Num:  112518



using SentinelCore.CaseFlowEngine.Infrastructure;
using SentinelCore.Contracts.Abstractions;
using SentinelCore.Contracts.Contracts;
using SentinelCore.Orchestrations.Models;




namespace SentinelCore.Orchestrations.Workflows.Executors;





/// <summary>
///     Deterministic conditional router for The Core graph. It reads the chat mode stamped on the incoming
///     <see cref="ChatMessage" /> by the host, normalizes it to the canonical <see cref="ChatModeNames" /> spelling,
///     publishes it to shared state, and forwards the message unchanged. The workflow's outgoing conditional edges
///     (built from <see cref="RoutesTo" />) then direct the message to the direct-answer branch or the investigation
///     branch.
/// </summary>
/// <remarks>
///     Fail-closed: a message with a missing or unrecognised chat mode yields a user-visible notice and returns
///     <see langword="null" />, which matches no outgoing edge — the message dead-ends rather than being guessed at.
///     No model is contacted by this executor; routing is fully deterministic so a model can never redirect a
///     non-investigation message into case creation (or silently swallow an investigation request).
/// </remarks>
[YieldsOutput(typeof(ChatMessage))] // ← MANDATORY: declare output type for compile-time validation
public sealed partial class ChatModeRouterExecutor : Executor
{
    private readonly ISystemReporter _reporter;








    /// <summary>
    ///     Initializes a new instance of the <see cref="ChatModeRouterExecutor" /> class.
    /// </summary>
    /// <param name="reporter">The system reporter for logging and event publishing.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="reporter" /> is <see langword="null" />.</exception>
    public ChatModeRouterExecutor(ISystemReporter reporter) : base("ChatModeRouterExecutor")
    {
        _reporter = reporter ?? throw new ArgumentNullException(nameof(reporter));
        Name = Id;
    }








    /// <summary>
    ///     Gets the human-readable name of this executor, used in log messages and diagnostics.
    /// </summary>
    public string Name { get; init; }








    /// <summary>
    ///     Main executor entry point called by the MAF dispatcher. Normalizes the message's chat mode, publishes it
    ///     to shared state, and forwards the message so the conditional edges can route it.
    /// </summary>
    /// <param name="message">The input message carrying the host-stamped chat mode.</param>
    /// <param name="context">The workflow context for shared-state updates and output yielding.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The forwarded message, or <see langword="null" /> when no route could be selected.</returns>
    [MessageHandler]
    public async ValueTask<ChatMessage?> HandleChatMessageAsync(ChatMessage? message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        // --- Status: Executor start ---
        _reporter.ReportInfo($"[{Name}] Starting execution. Input type: {nameof(ChatMessage)}");

        // --- Null validation ---
        if (message is null)
        {
            _reporter.ReportError($"[{Name}] Input message was null; no route selected.");
            await context.YieldOutputAsync(new ChatMessage(ChatRole.Assistant, $"[{Name}] Input message was null; no route selected."), cancellationToken).ConfigureAwait(false);
            return null;
        }

        try
        {
            if (message.GetChatType() != ChatType.Chat.ToString())
            {
                _reporter.ReportWarning($"[{Name}] Message carries no recognised chat type. Supported chat types [Chat/Investigation]. Routing rejected.");
                await context.YieldOutputAsync(new ChatMessage(ChatRole.Assistant, "⚠️ The message does not declare a recognised chat type, so no processing route was selected."), cancellationToken).ConfigureAwait(false);
                return null;
            }



            _reporter.ReportInfo($"[{Name}] Selected route for chat type '{ChatType.Chat}'.");

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

            await context.YieldOutputAsync(new ChatMessage(ChatRole.Assistant, $"⚠️ An internal error occurred in {Name}: {ex.Message}"), cancellationToken).ConfigureAwait(false);

            _reporter.ReportInfo($"[{Name}] No route selected due to error.");
            return null;
        }
    }








    /// <summary>
    ///     Creates a predicate function to evaluate whether the output of the executor corresponds to the specified chat mode.
    /// </summary>
    /// <param name="expectedChatType">
    ///     The chat mode to match against, normalized to the canonical spelling (e.g.,
    /// </param>
    /// <returns>
    ///     A predicate function that returns <see langword="true" /> if the executor's output is a <see cref="ChatMessage" />
    ///     with a chat mode that normalizes to <paramref name="expectedChatType" />. Returns <see langword="false" />
    ///     otherwise.
    /// </returns>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="expectedChatType" /> is <see langword="null" />, empty, or contains only whitespace.
    /// </exception>
    /// <remarks>
    ///     The returned predicate ensures deterministic routing by strictly matching the normalized chat mode.
    ///     This guarantees that only messages with the expected chat mode proceed along the corresponding workflow branch.
    /// </remarks>
    public static Func<object?, bool> RoutesTo(string expectedChatType)
    {
        Throw.IfNullOrWhitespace(expectedChatType);

        return output => output is ChatMessage message && message.GetChatType() is string chatType && string.Equals(chatType, expectedChatType, StringComparison.OrdinalIgnoreCase);
    }
}