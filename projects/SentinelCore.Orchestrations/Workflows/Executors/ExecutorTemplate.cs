using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using SentinelCore.Contracts.Abstractions;
using SentinelCore.Orchestrations.agents;




namespace SentinelCore.Orchestrations.Workflows.Executors
{


    /// <summary>
    /// A reusable, production-grade MAF executor template.
    ///
    /// Key features:
    /// - Strong typing: validates input type at runtime to prevent silent routing failures.
    /// - Robust error handling: catches and logs all exceptions, including cooperative cancellation.
    /// - Deterministic logging: emits start, progress, and completion markers for workflow tracing.
    /// - Final response pattern: guarantees a well-formed TOut even under failure conditions.
    /// - Context-safe: uses IWorkflowContext for shared-state updates and output yielding.
    ///
    /// Replace TIn and TOut with your actual message types.
    /// </summary>
    public sealed class TemplateExecutor<TIn, TOut> : Executor<TIn, TOut> where TIn : class where TOut : class, new()
    {
        private readonly ISystemReporter _reporter;








        public TemplateExecutor(ISystemReporter reporter) : base($"TemplateExecutor<{typeof(TIn).Name},{typeof(TOut).Name}>")
        {
            _reporter = reporter ?? throw new ArgumentNullException(nameof(reporter));
            Name = Id;
        }








        public string Name { get; init; }





        /// <summary>
        /// Main executor entry point.
        /// This method is called by the MAF dispatcher when routing reaches this executor.
        /// </summary>
        public override async ValueTask<TOut> HandleAsync(TIn message, IWorkflowContext context, CancellationToken cancellationToken = default)
        {
            // --- Status: Executor start ---
            _reporter.ReportInfo($"[{Name}] Starting execution. Input type: {typeof(TIn).Name}");
// Send a user-visible message back to the UI
            await context.YieldOutputAsync(new ChatMessage(ChatRole.Assistant, $"Processing your request in {Name}..."), cancellationToken).ConfigureAwait(false);





            // --- Type validation ---
            if (message is null)
            {
                _reporter.ReportError($"[{Name}] Input message was null. Returning fallback {typeof(TOut).Name}.");
                await context.YieldOutputAsync(new ChatMessage().AddAssistantMessage($"[{Name}] Input message was null. returning fallback {typeof(TOut).Name}."));
                return new TOut();
            }

            if (message is not TIn)
            {
                // This should never happen, but if it does, routing is corrupted.
               var err= $"[{Name}] Input type mismatch. Expected {typeof(TIn).Name}, got {message.GetType().Name}. " + $"Returning fallback {typeof(TOut).Name}.";
               _reporter.ReportError(err);
               await context.YieldOutputAsync(new ChatMessage().AddAssistantMessage(err));

                return new TOut();
            }

            try
            {
                // --- Status: Begin processing ---
                _reporter.ReportInfo($"[{Name}] Processing message...");

                // Example: update shared state (optional)
                await context.QueueStateUpdateAsync(key: "LastExecutor", value: Name, cancellationToken).ConfigureAwait(false);

                // --- Core logic placeholder ---
                // Replace this block with your actual executor logic.
                // This is where you transform TIn → TOut deterministically.
                TOut result = await ProcessMessageAsync(message, context, cancellationToken).ConfigureAwait(false);

                if (result is null)
                {
                    _reporter.ReportError($"[{Name}] ProcessAsync returned null. Using fallback {typeof(TOut).Name}.");
                    result = new TOut();
                }

                // --- Status: Final output ---
                _reporter.ReportInfo($"[{Name}] Completed successfully. Output type: {typeof(TOut).Name}");

                return result;
            }
            catch (OperationCanceledException)
            {
                // Cooperative cancellation must propagate.
                _reporter.ReportInfo($"[{Name}] Execution canceled.");
                //bubble up cancellation
                throw;
            }
            catch (Exception ex)
            {
                // --- Robust error handling ---
                _reporter.ReportError($"[{Name}] Exception: {ex.Message}", ex);

                // Optional: yield a user-visible message
                await context.YieldOutputAsync(new ChatMessage(ChatRole.Assistant, $"⚠️ An internal error occurred in {Name}: {ex.Message}"), cancellationToken).ConfigureAwait(false);

                // --- Status: fallback output ---
                _reporter.ReportInfo($"[{Name}] Returning fallback {typeof(TOut).Name} due to error.");

                return new TOut();
            }
        }








        /// <summary>
        /// Core processing logic for the executor.
        /// Override or replace this method when using the template.
        /// </summary>
        private async ValueTask<TOut> ProcessMessageAsync(TIn message, IWorkflowContext context, CancellationToken cancellationToken)
        {
            // Example placeholder logic:
            await Task.Delay(10, cancellationToken).ConfigureAwait(false);

            // Always return a valid TOut instance.
            return new TOut();
        }
    }


}
