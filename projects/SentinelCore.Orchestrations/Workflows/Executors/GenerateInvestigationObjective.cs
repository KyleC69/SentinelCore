// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         GenerateInvestigationObjective.cs
// Author: Kyle L. Crowder
// Build Num:  112101



using SentinelCore.Contracts.Abstractions;
using SentinelCore.Contracts.CaseFlow;
using SentinelCore.Orchestrations.Models;




namespace SentinelCore.Orchestrations.Workflows.Executors;





/// <summary>
///     Represents an executor responsible for orchestrating classification tasks within a workflow.
/// </summary>
/// <remarks>
///     This class integrates with AI agents and system reporting to process classification-related operations.
///     It is capable of sending messages of type <see cref="ChatMessage" /> and yielding outputs of types
///     <see cref="NextStep" /> and <see cref="ChatMessage" />.
/// </remarks>
[SendsMessage(typeof(ChatMessage))]
[YieldsOutput(typeof(NextStep))]
[YieldsOutput(typeof(ChatMessage))]
public sealed partial class ClassifierAgentExec : Executor
{
    private readonly AIAgent _agent;

    private readonly ISystemReporter _reporter;








    /// <summary>
    ///     Initializes a new instance of the <see cref="ClassifierAgentExec" /> class.
    /// </summary>
    /// <param name="agent">The classifier agent.</param>
    /// <param name="reporter">The system reporter for logging.</param>
    public ClassifierAgentExec(AIAgent agent, ISystemReporter reporter) : base("GenerateInvestigationObjective")
    {
        _agent = agent ?? throw new ArgumentNullException(nameof(agent));
        _reporter = reporter ?? throw new ArgumentNullException(nameof(reporter));
    }








    public string Name { get; } = "GenerateInvestigationObjective";








    [MessageHandler]
    public async ValueTask<ChatMessage> HandleChatMessageAsync(ChatMessage? message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        // --- Status: Executor start ---
        _reporter.ReportInfo($"[{Name}] Starting execution. Input type: {typeof(ChatMessage).Name}");

        // --- Null validation ---
        if (message is null)
        {
            string msg = $"[{Name}] Input message was null. Returning fallback {nameof(NextStep)}.";
            _reporter.ReportError(msg);
            await context.YieldOutputAsync(msg, cancellationToken).ConfigureAwait(false);
            message?.AddAssistantMessage(msg);

            return message;
        }

        try
        {
            // --- Status: Begin processing ---
            _reporter.ReportInfo($"[{Name}]Executor processing message...");
            _reporter.ReportDebug(message.Text);
            //TODO: transient error recovery


            ChatMessage result = await ProcessMessageAsync(message, context, cancellationToken).ConfigureAwait(false);

            //Send the prompt to the next step in the flow.
            //   await context.SendMessageAsync(message!, cancellationToken);

            // --- Status: Final output ---
            _reporter.ReportInfo($"[{Name}] Completed successfully. Output type: {nameof(NextStep)}.. Value of {message}");

            //Yield results to execution runner
            //  await context.YieldOutputAsync(messages, cancellationToken);

            // docs state duplication of return results is an anti-pattern however if return is not included a compiler error appears
            // should language exception be made for handlers that return value??
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
            // Classification failed. Route to human-operator escalation — a system
            // error requiring human intervention. Never fall back to RedAlert:
            // a transient model failure is not a critical security event.
            _reporter.ReportError($"[{Name}] Exception: {ex.Message}", ex);

            string messa = $"⚠️ Signal classification failed: {ex.Message} The signal has been routed for human review.";
            _reporter.ReportInfo(messa);

            await context.YieldOutputAsync(messa, cancellationToken).ConfigureAwait(false);



        }

        return message;
    }








    /// <summary>
    ///     classifier processing logic: invokes the classifier agent and attaches the original prompt.
    /// </summary>
    private async ValueTask<ChatMessage> ProcessMessageAsync(ChatMessage messages, IWorkflowContext context, CancellationToken cancellationToken)
    {
        _reporter.ReportInfo($"[{Name}] Starting classifier handler");


        //    ChatMessage? prompt = await context.ReadStateAsync<ChatMessage>(WorkFlowStateKeys.PROMPT, "SharedState").ConfigureAwait(false);

        try
        {

            AgentResponse<InvestigationObjective> response = await _agent.RunAsync<InvestigationObjective>(messages, cancellationToken: cancellationToken).ConfigureAwait(false);




            _reporter.ReportInfo($"[{Name}] ...returning to classifier handler");

            return new(ChatRole.User, response.Text);
        }
        catch (OperationCanceledException e)
        {
            //TODO: verify bubble up of cancellation to proper layer
            _reporter.ReportWarning("The agent operation was cancelled", e);
            throw;
        }
        catch (Exception ex)
        {
            _reporter.ReportError($"[{Name}] Exception: {ex.Message}", ex);
            throw;
        }
    }
}