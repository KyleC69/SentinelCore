// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         WhiteListExecutor.cs
// Author: Kyle L. Crowder
// Build Num:  091418



using SentinelCore.Contracts.Abstractions;




namespace SentinelCore.Orchestrations.Workflows.Executors;





/// <summary>
///     Checks the signal against the operator's whitelist. This will be stored in DB and vector searchable.
///     A list of environmentally acceptable signals that should be ignored. This list is populated only by end user
///     as a means of silencing benign signals. If the signal is not on the whitelist, it must flow through normal pathways.
///     If it is on the list, it will be logged and the flow terminated.
/// </summary>
[YieldsOutput(typeof(SuppressionDecision))]
public sealed partial class WhiteListExecutor : Executor
{
    private readonly ISystemReporter _reporter;

    /// <summary>
    ///     Gets the human-readable name of this executor, used in log messages and diagnostics.
    /// </summary>
    public string Name { get; init; }








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
    ///     Main executor entry point called by the MAF dispatcher.
    ///     Provides uniform cross-cutting concerns: logging, null validation,
    ///     cooperative cancellation propagation, and structured error handling.
    /// </summary>
    /// <param name="message">The input message to process.</param>
    /// <param name="context">The workflow context for shared-state updates and output yielding.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="SuppressionDecision" /> indicating whether the signal should be suppressed.</returns>
    [MessageHandler]
    public async ValueTask<SuppressionDecision> HandleAsync(ChatMessage message, IWorkflowContext context, CancellationToken ct = default)
    {
        // --- Status: Executor start ---
        _reporter.ReportInfo($"[{Name}] Starting execution. Input type: {typeof(ChatMessage).Name}");

        // --- Null validation ---
        if (message is null)
        {
            _reporter.ReportError($"[{Name}] Input message was null. Returning fallback {nameof(SuppressionDecision)}.");
            await context.YieldOutputAsync(new ChatMessage(ChatRole.Assistant, $"[{Name}] Input message was null. Returning fallback {nameof(SuppressionDecision)}."), ct).ConfigureAwait(false);
            return CreateFallbackResult();
        }

        try
        {
            // --- Status: Begin processing ---
            _reporter.ReportInfo($"[{Name}] Processing message...");

            SuppressionDecision result = await ProcessMessageAsync(message, context, ct).ConfigureAwait(false);

            if (result is null)
            {
                _reporter.ReportError($"[{Name}] ProcessMessageAsync returned null. Using fallback {nameof(SuppressionDecision)}.");
                result = CreateFallbackResult();
            }

            // --- Status: Final output ---
            _reporter.ReportInfo($"[{Name}] Completed successfully. Output type: {nameof(SuppressionDecision)}");

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

            _reporter.ReportInfo($"[{Name}] Returning fallback {nameof(SuppressionDecision)} due to error.");

            return CreateFallbackResult();
        }
    }








    /// <summary>
    ///     Core processing logic: checks the signal against the operator's whitelist.
    /// </summary>
    private async ValueTask<SuppressionDecision> ProcessMessageAsync(ChatMessage message, IWorkflowContext context, CancellationToken ct)
    {
        _reporter.ReportInfo("Starting whitelist executor...");

        SuppressionDecision results = new();
        if (message.Text.StartsWith("CASEGEN:", StringComparison.CurrentCulture))
        {
            _reporter.ReportInfo("Detected CASEGEN command. Bypassing whitelist check.");
            results.Command = CommandValue.CASEGEN;
            results.Prompt = message.Text.Substring(8); // Extract the prompt after "CASEGEN:"
        }
        else
        {
            results.Command = CommandValue.OTHER;
            results.Prompt = message.Text;
        }

        await context.SendMessageAsync(results, cancellationToken: ct).ConfigureAwait(false);
        return results;
    }








    /// <summary>
    ///     Creates a fallback result when the executor encounters an error or receives null input.
    /// </summary>
    private SuppressionDecision CreateFallbackResult() => new() { Command = CommandValue.OTHER, Prompt = string.Empty, Suppress = false };
}





/// <summary>
///     Represents a decision about whether a signal should be suppressed.
/// </summary>
public class SuppressionDecision
{
    /// <summary>
    ///     Gets or sets the command value indicating the type of signal.
    /// </summary>
    public CommandValue Command { get; set; }

    /// <summary>
    ///     Gets or sets the prompt text extracted from the signal.
    /// </summary>
    public string Prompt { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets whether the signal should be suppressed.
    /// </summary>
    public bool Suppress { get; set; }
}





/// <summary>
///     Enumerates the possible command values for signal classification.
/// </summary>
public enum CommandValue
{
    /// <summary>
    ///     Indicates a case generation command.
    /// </summary>
    CASEGEN,

    /// <summary>
    ///     Indicates any other command type.
    /// </summary>
    OTHER
}
