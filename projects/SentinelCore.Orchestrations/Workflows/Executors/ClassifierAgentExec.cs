// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         ClassifierAgentExec.cs
// Author: Kyle L. Crowder
// Build Num:  100908



using SentinelCore.Contracts.Abstractions;
using SentinelCore.Contracts.CaseFlow;
using SentinelCore.Orchestrations.Agents;
using SentinelCore.Orchestrations.Agents.Models;




namespace SentinelCore.Orchestrations.Workflows.Executors;





/// <summary>
///     This <c>Conditional Edge</c> is the first AI interaction and analysis of the signal and acts as a router to direct
///     the workflow in specific directions
///     depending on the input. It tries to determine if the input comes from automation or operator queries. The model
///     also attempts to identify incomplete signals (transport hiccup) and
///     ambiguous operator queries that appear to be valid but a subsystem is not identifiable so <c>More Information</c>
///     is needed so it is sent to the operator for review and intervention. No signal is
///     thrown out or ignored, the exception being the operator created whitelist. TODO: expand and employ absolute catch
///     all of ANY signal (DB based input logger/guarded file log)
///     Pre-filtering should have captured and blocked any noise from automation or intrusion attempts through simple
///     injection.
///     **Important Note** Security module is not integrated yet so signed input is not active. Signed input will require
///     that every piece of input be signed and verified before being
///     sent through the pipeline and is verified at key points in pipeline to prevent context contamination and catch
///     intrusion attempts. This is in no way intended as a catch-all or hack-proofing
///     security measure. Any integration must employ their own security and is responsible for such. This module is
///     intended as a provenance tracking/auditing system, this platform can be sent "signals" from any
///     form of external sources such as Sentinel Core anomaly detectors, specialized AI bots with specific error catching
///     capabilities. This capability opens up many doors for exploit attempts and the security module
///     ensures that only properly formatted and signed, messages from approved sources make it through. Integration point
///     is extension of ChatMessage type.
///     Classifies an incoming signal and routes it by returning a <see cref="NextStep" />.
///     On classification failure the handler yields a user-visible <see cref="ChatMessage" />
///     notice before returning a fallback routed to human review.
/// </summary>
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
    public ClassifierAgentExec(AIAgent agent, ISystemReporter reporter) : base("ClassifierExec")
    {
        _agent = agent ?? throw new ArgumentNullException(nameof(agent));
        _reporter = reporter ?? throw new ArgumentNullException(nameof(reporter));
        Name = Id;
    }








    /// <summary>
    ///     Gets the human-readable name of this executor, used in log messages and diagnostics.
    /// </summary>
    public string Name { get; init; }

    private ChatMessages _chatMessages = new();





    [MessageHandler]
    public async ValueTask<ChatMessages> HandleChatMessageAsync(ChatMessages? message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        // --- Status: Executor start ---
        _reporter.ReportInfo($"[{Name}] Starting execution. Input type: {typeof(ChatMessage).Name}");
        _reporter.ReportDebug($"ChatMessages object passed in with {message.Count} messages.");

        // --- Null validation ---
        if (message is null)
        {
            var msg = $"[{Name}] Input message was null. Returning fallback {nameof(NextStep)}.";
            _reporter.ReportError(msg);
            await context.YieldOutputAsync(msg, cancellationToken).ConfigureAwait(false);
            message?.AddAssistantMessage(msg);

            return message;
        }

        try
        {
            // --- Status: Begin processing ---
            _reporter.ReportInfo($"[{Name}] Processing message...");
            //TODO: transient error recovery


            var resultt = await ProcessMessageAsync(message, context, cancellationToken).ConfigureAwait(false);

            //Send the prompt to the next step in the flow.
            //   await context.SendMessageAsync(message!, cancellationToken);

            // --- Status: Final output ---
            _reporter.ReportInfo($"[{Name}] Completed successfully. Output type: {nameof(NextStep)}.. Value of {message.ToString()}");

            //Yield results to execution runner
            //  await context.YieldOutputAsync(messages, cancellationToken);

            // docs state duplication of return results is an anti-pattern however if return is not included a compiler error appears
            // should language exception be made for handlers that return value??
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
            // --- Robust error handling ---
            // Classification failed. Route to human-operator escalation — a system
            // error requiring human intervention. Never fall back to RedAlert:
            // a transient model failure is not a critical security event.
            _reporter.ReportError($"[{Name}] Exception: {ex.Message}", ex);

            var messa = $"⚠️ Signal classification failed: {ex.Message} The signal has been routed for human review.";
            _reporter.ReportInfo(messa);

            await context.YieldOutputAsync(messa, cancellationToken).ConfigureAwait(false);



        }

        return message;
    }








    /// <summary>
    ///     classifier processing logic: invokes the classifier agent and attaches the original prompt.
    /// </summary>
    private async ValueTask<ChatMessages> ProcessMessageAsync(ChatMessages messages, IWorkflowContext context, CancellationToken cancellationToken)
    {
        _reporter.ReportInfo($"[{Name}] Starting classifier handler");


        var prompt = await context.ReadStateAsync<ChatMessage>(WorkFlowStateKeys.PROMPT, "SharedState").ConfigureAwait(false);

        try
        {

            ChatMessages msg = new();

            // Add system and user messages to the message stack
            // The system message provides context and instructions for the AI agent.
            msg.AddSystemMessage(AgentInstructionConstants.CURRENT_PLATFORM_DOMAIN_S);
            // Add classifier specific instructions to the message stack
            msg.AddSystemMessage(AgentInstructionConstants.CLASSIFIER_INSTRUCTIONS);
            //Add signal to message stack
            if (!ReferenceEquals(prompt, null))
            {
                msg.AddUserMessage(prompt.Text);
            }

            AgentResponse<InvestigationObjective> response = await _agent.RunAsync<InvestigationObjective>(msg, cancellationToken: cancellationToken).ConfigureAwait(false);



            var objective = messages.Where(e => e.Contents is InvestigationObjective);

            _reporter.ReportInfo($"[{Name}] ...returning to classifier handler");

            return messages;
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