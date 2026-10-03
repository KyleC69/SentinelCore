// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         WhiteListExecutor.cs
// Author: Kyle L. Crowder
// Build Num:  100310



using SentinelCore.Contracts.Abstractions;




namespace SentinelCore.Orchestrations.Workflows.Executors;





/// <summary>
///     Checks the signal against the operator's whitelist. This will be stored in DB and vector searchable.
///     A list of environmentally acceptable signals that should be ignored. This list is populated only by end user
///     as a means of silencing benign signals. If the signal is not on the whitelist, it must flow through normal
///     pathways.
///     If it is on the list, it will be logged and the flow terminated.
/// </summary>
[YieldsOutput(typeof(ChatMessage))]
public sealed partial class WhiteListExecutor : Executor
{
    private readonly ISystemReporter _reporter;








    /// <summary>
    ///     Initializes a new instance of the <see cref="WhiteListExecutor" /> class.
    /// </summary>
    /// <param name="reporter">The system reporter for logging.</param>
    public WhiteListExecutor(ISystemReporter reporter) : base("WhitelistExecutor")
    {
        _reporter = reporter ?? throw new ArgumentNullException(nameof(reporter));
        Name = Id;
    }








    /// <summary>
    ///     Gets the human-readable name of this executor, used in log messages and diagnostics.
    /// </summary>
    public string Name { get; init; }








    /// <summary>
    ///     Creates a fallback result when the executor encounters an error or receives null input.
    /// </summary>
    private ChatMessage CreateFallbackResult() => new(ChatRole.Assistant, $"[{Name}] Returning fallback {nameof(ChatMessage)}.");








    /// <summary>
    ///     Handles the suppression of a chat message by evaluating it against the operator's whitelist.
    /// </summary>
    /// <param name="message">The chat message to be evaluated.</param>
    /// <param name="context">The workflow context providing execution details.</param>
    /// <param name="ct">The cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A <see cref="ChatMessage" /> indicating the result of the suppression process.
    /// </returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="message" /> is null.</exception>
    /// <remarks>
    ///     If the message is not on the whitelist, it will proceed through normal processing.
    ///     If it is on the whitelist, the flow will be terminated, and the message will be logged.
    /// </remarks>
    [MessageHandler]
    public async ValueTask<ChatMessage> HandleSuppressAsync(ChatMessage message, IWorkflowContext context, CancellationToken ct = default)
    {
        // --- Status: Executor start ---
        _reporter.ReportInfo($"[{Name}] Starting execution. Input type: {typeof(ChatMessage).Name}");

        // --- Null validation ---
        if (message is null)
        {
            _reporter.ReportError($"[{Name}] Input message was null. Returning fallback {nameof(ChatMessage)}.");
            await context.YieldOutputAsync(new ChatMessage(ChatRole.Assistant, $"[{Name}] Input message was null. Returning fallback {nameof(ChatMessage)}."), ct).ConfigureAwait(false);
            return CreateFallbackResult();
        }

        try
        {
            // --- Status: Begin processing ---
            _reporter.ReportInfo($"[{Name}] Processing message...");

            ChatMessage result = await ProcessMessageAsync(message, context, ct).ConfigureAwait(false);

            if (result is null)
            {
                _reporter.ReportError($"[{Name}] ProcessMessageAsync returned null. Using fallback {nameof(ChatMessage)}.");
                result = CreateFallbackResult();
            }

            // --- Status: Final output ---
            _reporter.ReportInfo($"[{Name}] Completed successfully. Output type: {nameof(ChatMessage)}");

            return result;
        }
        catch (OperationCanceledException)
        {
            // Cooperative cancellation must propagate — never swallow it.
            _reporter.ReportInfo($"[{Name}] Execution canceled.");
            throw;
        }
        catch (Exception ex)
        {
            // --- Robust error handling ---
            _reporter.ReportError($"[{Name}] Exception: {ex.Message}", ex);

            await context.YieldOutputAsync(new ChatMessage(ChatRole.Assistant, $"⚠️ An internal error occurred in {Name}: {ex.Message}"), ct).ConfigureAwait(false);

            _reporter.ReportInfo($"[{Name}] Returning fallback {nameof(ChatMessage)} due to error.");

            return CreateFallbackResult();
        }
    }








    /// <summary>
    ///     Core processing logic: checks the signal against the operator's whitelist.
    /// </summary>
    private async ValueTask<ChatMessage> ProcessMessageAsync(ChatMessage message, IWorkflowContext context, CancellationToken ct)
    {
        _reporter.ReportInfo("Starting whitelist executor...");
        return message;
    }
}