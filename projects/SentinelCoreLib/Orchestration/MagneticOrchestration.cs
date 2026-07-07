// Solution: SentinelCoreLib
// Project:   SentinelCoreLib
// File:         MagneticOrchestration.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Specialized.Magentic;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

using SentinelCoreLib.Agents.Domain;
using SentinelCoreLib.Agents.Manager;
using SentinelCoreLib.Application;
using SentinelCoreLib.Application.Abstractions;
using SentinelCoreLib.Contracts;

using System.Text;
using System.Text.Json;




namespace SentinelCoreLib.Orchestration;





/// <summary>
///     Represents a magnetic orchestrated workflow that is handed a list of investigation tasks.
///     The workflow manager monitors streaming execution and logs communication between the
///     manager and the domain agents created for each task.
/// </summary>
public sealed class MagneticOrchestration
{
    private readonly DomainAgentFactory _domainAgentFactory;
    private readonly ILogger<MagneticOrchestration> _logger;
    private readonly ManagerAgentFactory _managerAgentFactory;

    private static readonly JsonSerializerOptions s_jsonOptions = new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };








    /// <summary>
    ///     Initializes a new instance of the <see cref="MagneticOrchestration" /> class.
    /// </summary>
    public MagneticOrchestration(ManagerAgentFactory managerAgentFactory, DomainAgentFactory domainAgentFactory, ILoggerFactory loggerFactory)
    {
        _managerAgentFactory = managerAgentFactory ?? throw new ArgumentNullException(nameof(managerAgentFactory));
        _domainAgentFactory = domainAgentFactory ?? throw new ArgumentNullException(nameof(domainAgentFactory));
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _logger = loggerFactory.CreateLogger<MagneticOrchestration>();
    }








    /// <summary>
    ///     Builds the execution prompt from the task list and optional case identifier.
    /// </summary>
    private static string BuildExecutionPrompt(List<InvestigationPlanStep> tasks, string? caseId)
    {
        StringBuilder builder = new();
        builder.AppendLine("Execute this SentinelCore investigation task list.");

        if (!string.IsNullOrWhiteSpace(caseId))
        {
            builder.Append("CaseId: ").AppendLine(caseId);
        }

        builder.AppendLine("Add the results to the results property of the InvestigationPlan object.");
        builder.AppendLine("Return the InvestigationPlan with the task results added to it.");
        builder.AppendLine("Tasks:");
        builder.AppendLine(JsonSerializer.Serialize(tasks, s_jsonOptions));

        return builder.ToString();
    }








    /// <summary>
    ///     Creates one domain agent per task and assigns tools for the task's target domain.
    /// </summary>
    private List<AIAgent> CreateParticipants(IReadOnlyList<InvestigationPlanStep> tasks)
    {
        List<AIAgent> participants = [];
        HashSet<string> participantNames = new(StringComparer.OrdinalIgnoreCase);

        foreach (InvestigationPlanStep step in tasks)
        {
            string domain = step.Surface ?? string.Empty;
            if (string.IsNullOrWhiteSpace(domain))
            {
                _logger.LogWarning("Skipping task with missing TargetDomain.");
                throw new SentinelCoreModelException("The Core failed to enter required information for the task. Missing TargetDomain. File a report in the application repository");
                continue;
            }

            IList<AITool>? domainTools = ToolRegistry.GetToolByDomain(domain);
            if (domainTools is null || domainTools.Count == 0)
            {
                _logger.LogWarning("No tools found for domain '{Domain}'. Skipping agent creation.", domain);
                throw new SentinelCoreModelException($"The Core failed to enter required information for the task. No tools found for domain '{domain}'. File a report in the application repository");
                // continue;
            }

            string agentName = $"{domain}_agent";
            if (!participantNames.Add(agentName))
            {
                _logger.LogDebug("Domain agent '{AgentName}' already created; reusing existing participant.", agentName);
                // continue;
            }

            AIAgent agent = _domainAgentFactory.CreateAgent(agentName, ToolRegistry.GetToolByDomain(domain)?.ToArray() ?? Array.Empty<AITool>(), $"Executes tasks for the {domain} domain using only the provided tools.");

            _logger.LogInformation("Created domain agent '{AgentName}' for domain '{Domain}' with {ToolCount} tool(s).", agentName, domain, domainTools.Count);

            participants.Add(agent);
        }

        return participants.Count == 0 ? throw new InvalidOperationException("No domain agents could be created for the supplied tasks.") : participants;

    }








    /// <summary>
    ///     Deserializes the manager's final text into an <see cref="InvestigationPlan" /> result.
    /// </summary>
    private static InvestigationPlan CreateResult(string responseText, string? caseId)
    {
        if (!string.IsNullOrWhiteSpace(responseText))
        {
            try
            {
                InvestigationPlan? result = JsonSerializer.Deserialize<InvestigationPlan>(responseText, s_jsonOptions);
                if (result is not null)
                {
                    if (string.IsNullOrWhiteSpace(result.CaseId))
                    {
                        result.CaseId = caseId ?? string.Empty;
                    }

                    return result;
                }
            }
            catch (JsonException)
            {
                // Fall back to a textual result when the manager returns a non-JSON summary.
            }
        }

        return new() { CaseId = caseId ?? string.Empty };
    }








    /// <summary>
    ///     Executes the supplied investigation tasks as a monitored magnetic workflow.
    ///     A domain agent is created for each task and equipped with tools from the registry
    ///     via <see cref="IToolRegistry.GetToolByDomain" />. The manager coordinates the agents
    ///     using streaming execution, and all manager/agent communication is logged.
    /// </summary>
    /// <param name="tasks">The investigation tasks to execute.</param>
    /// <param name="caseId">Optional case identifier for correlation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The synthesized investigation plan result.</returns>
    public async Task<InvestigationPlan> ExecuteTasksAsync(List<InvestigationPlanStep> tasks, string caseId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tasks);
        ArgumentNullException.ThrowIfNull(caseId);

        _logger.LogInformation("Starting magnetic workflow for case {CaseId} with {TaskCount} task(s).", caseId, tasks);

        AIAgent manager = _managerAgentFactory.Create();
        List<AIAgent> participants = CreateParticipants(tasks);

        Workflow workflow = new MagenticWorkflowBuilder(manager).AddParticipants(participants).WithName("SentinelCore Monitored Magnetic Workflow").WithDescription("Coordinates domain agents created per task and logs manager/agent communication via streaming execution.").RequirePlanSignoff(false).WithMaxRounds(2).WithMaxStalls().WithMaxResets(2).Build();

        string prompt = BuildExecutionPrompt(tasks, caseId);
        string managerSystemPrompt = """
                                     **You are the Manager.
                                     You do not create tasks.
                                     You do not modify tasks.
                                     You do not invent tasks.
                                     You do not plan.
                                     You do not guess.
                                     You do not generate reminders.
                                     You do not summarize.
                                     You do not interpret OS commands.
                                     You do not execute anything.

                                     Your ONLY job is:
                                     Receive the pre‑built plan (list of EvidenceOperations).
                                     Select the next uncompleted operation.
                                     Send that operation to the correct agent.
                                     Wait for the agent’s result.
                                     Mark the operation complete or failed.
                                     Move to the next operation.
                                     You must NEVER alter the operation’s instruction text.
                                     You must NEVER generate new instructions.
                                     You must NEVER add commentary.
                                     You must NEVER produce reminders or helpful suggestions.
                                     You are a deterministic dispatcher, not a planner.
                                     Output ONLY the next operation to the correct agent.**
                                     """;

        List<ChatMessage> messages =
        [
                new(ChatRole.System, managerSystemPrompt),
                new(ChatRole.User, prompt)
        ];

        await using StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, messages, cancellationToken: cancellationToken);

        await run.TrySendMessageAsync(new TurnToken(true));

        string? lastResponseId = null;
        WorkflowOutputEvent? finalOutput = null;

        await foreach (WorkflowEvent workflowEvent in run.WatchStreamAsync(cancellationToken))
            switch (workflowEvent)
            {
                case AgentResponseUpdateEvent updateEvent:
                    string responseId = updateEvent.Update.ResponseId ?? updateEvent.Update.MessageId ?? updateEvent.ExecutorId;

                    if (!string.Equals(responseId, lastResponseId, StringComparison.Ordinal))
                    {
                        _logger.LogInformation("Agent communication - {ExecutorId} responding (case {CaseId}):", updateEvent.ExecutorId, caseId ?? "(unknown)");
                        lastResponseId = responseId;
                    }

                    _logger.LogInformation("{UpdateText}", updateEvent.Update.Text);
                    break;
                /*
                                case MagenticPlanCreatedEvent planCreated:
                                    _logger.LogInformation(
                                        "[Magentic Initial Plan] (case {CaseId}):\n{PlanText}",
                                        caseId ?? "(unknown)",
                                        planCreated.FullTaskLedger.Text);
                                    break;

                                case MagenticReplannedEvent replanned:
                                    _logger.LogInformation(
                                        "[Magentic Replanned] (case {CaseId}):\n{PlanText}",
                                        caseId ?? "(unknown)",
                                        replanned.FullTaskLedger.Text);
                                    break;
                */
                case MagenticProgressLedgerUpdatedEvent progressUpdated:
                    MagenticProgressLedger ledger = progressUpdated.ProgressLedger;
                    _logger.LogInformation("[Magentic Progress Ledger] (case {CaseId}) satisfied={IsRequestSatisfied}, " + "inLoop={IsInLoop}, progressing={IsProgressBeingMade}, " + "nextSpeaker={NextSpeaker}, instruction={InstructionOrQuestion}", caseId ?? "(unknown)", ledger.IsRequestSatisfied, ledger.IsInLoop, ledger.IsProgressBeingMade, ledger.NextSpeaker, ledger.InstructionOrQuestion);
                    break;

                case WorkflowOutputEvent outputEvent when outputEvent.Is<List<ChatMessage>>():
                    finalOutput = outputEvent;
                    break;

                case WorkflowErrorEvent workflowError:
                    _logger.LogError(workflowError.Exception, "Workflow error occurred for case {CaseId}.", caseId ?? "(unknown)");
                    break;

                case ExecutorFailedEvent executorFailed:
                    _logger.LogError("Executor '{ExecutorId}' failed for case {CaseId}: {Error}", executorFailed.ExecutorId, caseId ?? "(unknown)", executorFailed.Data?.ToString() ?? "unknown error");
                    break;
            }

        if (finalOutput is null)
        {
            throw new InvalidOperationException($"Workflow completed without producing a final output for case {caseId ?? "(unknown)"}.");
        }

        List<ChatMessage>? resultMessages = finalOutput.Data as List<ChatMessage>;
        string responseText = ExtractText(resultMessages);

        return CreateResult(responseText, caseId);
    }








    /// <summary>
    ///     Extracts text content from the final workflow output messages.
    /// </summary>
    private static string ExtractText(List<ChatMessage>? messages)
    {
        if (messages is null || messages.Count == 0)
        {
            return string.Empty;
        }

        IEnumerable<string> text = messages.SelectMany(message => message.Contents.OfType<TextContent>().Select(content => content.Text)).Where(segment => !string.IsNullOrWhiteSpace(segment));

        return string.Join(Environment.NewLine, text);
    }
}