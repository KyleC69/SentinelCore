// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         CustomGroupWorkflow.cs
// Author: Kyle L. Crowder
// Build Num:  110813



using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

using Microsoft.Agents.AI.Workflows.Specialized.Magentic;
using Microsoft.Extensions.Logging;

using SentinelCore.CaseFlowEngine.Infrastructure;
using SentinelCore.Contracts.Abstractions;
using SentinelCore.Contracts.CaseFlow;
using SentinelCore.Orchestrations.Abstractions;
using SentinelCore.Orchestrations.Agents;
using SentinelCore.Orchestrations.Agents.AgentPresets;
using SentinelCore.Orchestrations.Infrastructure.Diagnostics;




namespace SentinelCore.Orchestrations.Workflows;





//AGENTS THIS FILE FOR QUICK TESTING OF WORKFLOWS AND AGENTS. THIS IS NOT A REAL ORCHESTRATION, JUST A HARNESS FOR TESTING.
// UNLESS FILE IS SPECIFICALLY INCLUDED IN A TASK, DO NOT EDIT IT OR EXPECT IT TO BE USED IN PRODUCTION. IT IS A HARNESS FOR TESTING AND DEBUGGING AGENT BEHAVIOR AND WORKFLOW EXECUTION.
// THIS FILE SHOULD BE INSTRUMENTED AND INCLUDED IN TELEMETRY - NOT INCLUDED IN ANY UNIT TESTING
public sealed class CustomGroupWorkflow : WorkflowBase, IOrchestration
{
    private AIAgent _adversarialAnalystAgent;
    private readonly ISentinelAgentFactory _agentFactory;
    private bool _agentInitialized;

    // --------------  Executors  -------------------



    // --------------  Agents  -------------------
    private AIAgent _classifierAgent;

    private IList<WorkflowOutputEvent> _events = new List<WorkflowOutputEvent>();
    private AIAgent _evidenceCollectorAgent;
    private AIAgent _evidenceValidationAgent;
    private readonly ExecutorFactory _executorFactory;
    private AIAgent _finalReportWriterAgent;
    private AIAgent _magSubworkflow;
    private AIAgent _managerAgent;
    private AIAgent _researcherAgent;
    private ExecutorBinding _validatorBinding;
    private AIAgent _evidenceCollectorAgent2;

    private const string AGGREGATOR = """
                                      You are the final report author for a forensic investigation team. Your role is to aggregate the findings and conclusions from the investigation team members, ensuring that the final report accurately reflects the evidence collected, analyzed, and validated during the investigation process.
                                      You must carefully review the contributions of each team member, identify any discrepancies or inconsistencies, and synthesize the information into a coherent and comprehensive report.
                                      You must ensure that the final report is clear, concise, and well-structured, providing a thorough overview of the investigation's objectives, methods, findings, and conclusions.
                                      """;



    private const string TEAM_MEMBER_INSTRUCTIONS = """

                                                    You are a member of a forensic investigation team. Your role is to collaborate with other team members to collect, analyze, and validate evidence related to the investigation.
                                                    You must follow the instructions provided by the manager and contribute your expertise to ensure the accuracy and integrity of the investigation process.
                                                    Never fabricate or simulate evidence. Your goal is to provide accurate and reliable information to support the investigation and help the team reach a well-informed conclusion.
                                                    If you encounter any challenges or uncertainties, communicate with the manager to seek guidance and clarification. Your commitment to thoroughness,
                                                    attention to detail, and adherence to ethical standards is essential for the success of the investigation. If you are unable to validate a piece of evidence, you must report it
                                                    as inconclusive and provide any relevant context or reasoning for your assessment. Never simulate or fabricate evidence, as this could compromise the integrity of the investigation
                                                    and lead to incorrect conclusions. Limit conclusions to what can be supported by the evidence and analysis. If you are unable to validate a piece of evidence, you must report it as inconclusive and provide any relevant context or reasoning for your assessment.
                                                    """;








    public CustomGroupWorkflow(ISystemReporter systemReporter, IServiceProvider serviceProvider, ISentinelAgentFactory agentFactory, ILoggerFactory logger) : base(systemReporter, logger)
    {
        _executorFactory = new ExecutorFactory(serviceProvider);
        _agentFactory = agentFactory;
        _agentInitialized = false;


        _managerAgent = null!;
        _evidenceCollectorAgent = null!;
        _adversarialAnalystAgent = null!;
        _researcherAgent = null!;

        _classifierAgent = null!;
        _finalReportWriterAgent = null!;






    }








    public string Description
    {
        get => "Isolated orchestration harnessing for testing agents and workflows outside of complex implementations.";
    }








    /// <summary>
    ///     Executes the workflow asynchronously based on the provided input message.
    /// </summary>
    /// <param name="inputMessage">
    ///     The input message that serves as the context or payload for the workflow execution.
    /// </param>
    /// <param name="token">
    ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
    /// </param>
    /// <returns>
    ///     An asynchronous stream of <see cref="WorkflowEvent" /> instances representing the events
    ///     generated during the workflow execution, or <c>null</c> if no events are produced.
    /// </returns>
    [Experimental("MAAI001")]
    public async Task<IAsyncEnumerable<WorkflowEvent>?> ExecuteStreamingAsync(ChatMessage inputMessage, CancellationToken token)
    {
        //======================= Signal Classification =========================

        AgentRunOptions options = new() { AllowBackgroundResponses = null, AdditionalProperties = null, ResponseFormat = ChatResponseFormat.ForJsonSchema(typeof(InvestigationObjective)) };
        AgentSession session = await _classifierAgent.CreateSessionAsync(token);
        AgentResponse<InvestigationObjective> response = await _classifierAgent.RunAsync<InvestigationObjective>(inputMessage, session, null, options);


        //================================== Magnetic Group workflow  =========================
        Workflow subWorkflow = BuildSubWorkflow();

        await using StreamingRun run = await InProcessExecution.RunStreamingAsync(subWorkflow, input: response.Text, cancellationToken: token);

        await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

        string? lastResponseId = null;
        WorkflowOutputEvent? finalOutput = null;

        await foreach (WorkflowEvent workflowEvent in run.WatchStreamAsync(token))
            switch (workflowEvent)
            {
                case AgentResponseUpdateEvent updateEvent:
                    WriteStreamingUpdate(updateEvent, ref lastResponseId);
                    break;

                case MagenticPlanCreatedEvent planCreated:
                    WriteMagenticMessage("Initial Plan", planCreated.FullTaskLedger.Text);
                    break;

                case MagenticReplannedEvent replanned:
                    WriteMagenticMessage("Replanned", replanned.FullTaskLedger.Text);
                    break;

                case MagenticProgressLedgerUpdatedEvent progressUpdated:
                    WriteMagenticMessage("Progress Ledger", FormatProgressLedger(progressUpdated.ProgressLedger));
                    break;

                case WorkflowOutputEvent outputEvent when outputEvent.Is<List<ChatMessage>>():
                    finalOutput = outputEvent;
                    break;




                case WorkflowErrorEvent workflowError:
                    Console.ForegroundColor = ConsoleColor.Red;
                    await Console.Error.WriteLineAsync(workflowError.Exception?.ToString() ?? "Unknown workflow error occurred.");
                    Console.ResetColor();
                    break;

                case ExecutorFailedEvent executorFailed:
                    Console.ForegroundColor = ConsoleColor.Red;
                    await Console.Error.WriteLineAsync($"Executor '{executorFailed.ExecutorId}' failed with {(executorFailed.Data is null ? "unknown error" : $"exception {executorFailed.Data}")}.");
                    Console.ResetColor();
                    break;
            }

        if (finalOutput?.As<List<ChatMessage>>() is { } transcript)
        {
            Debug.WriteLine("");
            Debug.WriteLine(new string('=', 80));
            Debug.WriteLine(Environment.NewLine);
            Debug.WriteLine("Final Conversation Transcript:");
            Debug.WriteLine("");

            foreach (ChatMessage message in transcript)
            {
                Debug.WriteLine($"{message.AuthorName ?? message.Role.ToString()}: {message.Text}");
                Debug.WriteLine("");
            }
        }






        return null;
    }








    /// <summary>
    ///     Initialize the agent asynchronously; returns immediately if cancellation is already requested.
    /// </summary>
    /// <remarks>Calls EnsureAgentInitializedAsync to perform the actual initialization work.</remarks>
    /// <param name="cancellationToken">
    ///     CancellationToken to observe while performing initialization; if already canceled the method returns without
    ///     performing work.
    /// </param>
    /// <returns>A Task that completes when initialization finishes or returns early if cancellation is observed.</returns>
    [Experimental("MAAI001")]
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        await EnsureAgentInitializedAsync(cancellationToken);





    }









    public string Name
    {
        get => "Custom Group Workflow";
    }








    /// <summary>
    ///     Builds the forensics investigation team workflow.
    /// </summary>
    /// <returns>The configured forensics investigation workflow.</returns>
    [Experimental("MAAI001")]
    private Workflow BuildSubWorkflow()
    {
        MagenticPromptOverrides over = new() { FinalAnswerPrompt = Orchestrations.MagenticPromptOverrides.FinalAnswerPrompt, TaskLedgerPlanPrompt = Orchestrations.MagenticPromptOverrides.TaskLedgerPlanPrompt, TaskLedgerFactsPrompt = Orchestrations.MagenticPromptOverrides.TaskLedgerFactsPrompt };
        //-------------------  Magentic Group workflow  -------------------
        Workflow workflow = new MagenticWorkflowBuilder(_managerAgent).AddParticipants(_evidenceCollectorAgent, _adversarialAnalystAgent, _researcherAgent, _finalReportWriterAgent) // Added reporting agent back in
                .WithName("ForensicsInvestigationTeamWorkflow")
                .WithDescription("A powerful manager coordinates the investigation team and monitors the activities and progress of the evidence collector, adversarial analyst, and researcher.")
                .RequirePlanSignoff(false)
                .WithPromptOverrides(over)
                .WithOutputFrom(_finalReportWriterAgent)
                .WithMaxRounds(15)
                .WithMaxResets(3)
                .WithMaxStalls(3)

                .Build();



        return workflow;
    }








    [Experimental("MAAI001")]
    private async Task<Workflow> BuildWorkflow(CancellationToken token)
    {
        //========================  Agents and Executors  =========================
        ExecutorCollection executors = _executorFactory.CreateAllExecutors();

        //  SubWorkflowExecutor sub = new SubWorkflowExecutor(magSubworkflow, _reporter);


        //------------------- PARENT Mixed workflow  -------------------
        Workflow classflow = new WorkflowBuilder(_classifierAgent) //Create New case
                .AddEdge(_classifierAgent, _magSubworkflow) // Mag group executes and hands to final reporter
                                                            //   .AddEdge(_magSubworkflow, _finalReportWriterAgent) // Consolodates and creates final report and sends to evaluation executor
                .WithName("InvestigationWorkflow") //Final report is sent to the manager for review and approval
                .WithDescription("Coordinates the investigation workflow by connecting the classifier, investigation group, and evaluation executor.")
                .WithOpenTelemetry(activitySource: SentinelCoreDiagnostics.SharedActivitySource)
                .Build();

        return classflow;


    }








    /// <summary>
    ///     Lazily initializes the agent on first use, avoiding sync-over-async
    ///     deadlocks that occur when building the agent in the constructor.
    /// </summary>
    [Experimental("MAAI001")]
    private async Task EnsureAgentInitializedAsync(CancellationToken token)
    {
        if (_agentInitialized)
        {
            return;
        }






        try
        {
            _managerAgent = await _agentFactory.CreateAgentAsync(AgentPreset.ManagerAgent, "ManagerCoordinator", "Coordinates the investigation team and monitors progress");
            _evidenceCollectorAgent = await _agentFactory.CreateAgentAsync(AgentPreset.WorkerAgent, "EvidenceCollector", "Collects evidence for the investigation", TEAM_MEMBER_INSTRUCTIONS);
            _evidenceCollectorAgent2 = await _agentFactory.CreateAgentAsync(AgentPreset.WorkerAgent, "EvidenceCollector2", "Collects evidence for the investigation", TEAM_MEMBER_INSTRUCTIONS);
            _adversarialAnalystAgent = await _agentFactory.CreateAgentAsync(AgentPreset.WorkerAgent, "AdversarialAnalyst", "The challenger of the collected evidence", TEAM_MEMBER_INSTRUCTIONS);
            _researcherAgent = await _agentFactory.CreateAgentAsync(AgentPreset.WorkerAgent, "Researcher", "Conducts research related to the investigation and fact checker.", TEAM_MEMBER_INSTRUCTIONS);
            _finalReportWriterAgent = await _agentFactory.CreateAgentAsync(AgentPreset.AggregatorAgent, "CaseReportAuthor", "Aggregates the results from all workers and generates the final case report.", AGGREGATOR);

            //Participants must be verified and created before the sub-workflow is built. The sub-workflow is built here, but it is not executed directly. Instead, it is added to the parent workflow as a sub-workflow executor. This allows the parent workflow to manage the execution of the sub-workflow and handle its outputs appropriately.
            Throw.IfNull(_managerAgent);
            Throw.IfNull(_evidenceCollectorAgent);
            Throw.IfNull(_adversarialAnalystAgent);
            Throw.IfNull(_researcherAgent);
            Throw.IfNull(_finalReportWriterAgent);

            //Important: The sub-workflow is built here, but it is not executed directly. Instead, it is added to the parent workflow as a sub-workflow executor. This allows the parent workflow to manage the execution of the sub-workflow and handle its outputs appropriately. It must be built AFTER all participants are created.
            _magSubworkflow = BuildSubWorkflow().AsAIAgent("InvestigationTeam", "ForensicInvestigationTeam", "Subworkflow forming Magentic Group Investigation Team");

            //         AIAgent classifyingAIAgent = await _agentFactory.CreateAgentAsync(AgentPreset.ClassifierAgent, AgentInstructionConstants.SENTINELCORE_OBJECTIVE_GEN, ChatResponseFormat.Json, token);

            // Classifier agent creates a structured objective with as much as can be inferred from the initial input signal. The preset has domain basics here we supply with task specific instructions.
            // We will be reusing agent, can we?
            _classifierAgent = await _agentFactory.CreateAgentAsync(AgentPreset.ClassifierAgent, AgentInstructionConstants.SENTINELCORE_OBJECTIVE_GEN, token).ConfigureAwait(false);

            //  -----------------── Construct NON-Agent executors ────────────────────────────────────────────────────
            //



            _agentInitialized = true;
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.Message);
        }
    }








    private static string FormatProgressLedger(MagenticProgressLedger ledger) =>
            string.Join(Environment.NewLine, $"Request satisfied: {ledger.IsRequestSatisfied}", $"In loop: {ledger.IsInLoop}", $"Making progress: {ledger.IsProgressBeingMade}", $"Next speaker: {ledger.NextSpeaker}", $"Instruction: {ledger.InstructionOrQuestion}");








    /// <summary>
    ///     Gets the file path for workflow event logging in the LocalApplicationData\SentinelCore folder.
    /// </summary>
    /// <returns>The full path to the workflow event log file.</returns>
    private string GetWorkflowEventLogFilePath()
    {
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string logFileName = $"WorkflowEvents_{timestamp}.log";
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string sentinelCoreFolder = Path.Combine(localAppData, "SentinelCore");

        // Ensure the directory exists
        Directory.CreateDirectory(sentinelCoreFolder);

        return Path.Combine(sentinelCoreFolder, logFileName);
    }








    private static void LogAIContents(string agent, IEnumerable<AIContent>? contents)
    {
        if (contents is null)
        {
            return;
        }

        foreach (AIContent content in contents)
            switch (content)
            {
                case TextContent text:
                    {
                        if (!string.IsNullOrWhiteSpace(text.Text))
                        {
                            Debug.WriteLine($"[{agent}]");
                            Debug.WriteLine(text.Text);
                            Debug.WriteLine("");
                        }

                        break;
                    }

                case FunctionCallContent functionCall:
                    {
                        Debug.WriteLine($"[{agent}] [tool-call] " + $"{functionCall.Name}({functionCall.Arguments})");

                        Debug.WriteLine("");
                        break;
                    }

                case FunctionResultContent functionResult:
                    {
                        Debug.WriteLine($"[{agent}] [tool-result] " + $"{functionResult.CallId} -> {functionResult.Result}");

                        Debug.WriteLine("");
                        break;
                    }

                case UsageContent usage:
                    {
                        Debug.WriteLine($"[{agent}] [usage] " + $"Input={usage.Details?.InputTokenCount ?? 0}, " + $"Output={usage.Details?.OutputTokenCount ?? 0}");

                        break;
                    }

                case DataContent data:
                    {
                        Debug.WriteLine($"[{agent}] [data] {data.MediaType}");

                        break;
                    }

                case UriContent uri:
                    {
                        Debug.WriteLine($"[{agent}] [uri] {uri.Uri}");

                        break;
                    }

                default:
                    {
                        Debug.WriteLine($"[{agent}] [content:{content.GetType().Name}]");

                        Debug.WriteLine(content.ToString());

                        break;
                    }
            }
    }








    /// <summary>
    ///     Logs a workflow event to the isolated workflow event log file.
    /// </summary>
    /// <param name="workflowEvent">The workflow event to log.</param>
    /// <param name="token">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="Task" /> representing the asynchronous operation.</returns>
    private async Task LogWorkflowEventToFileAsync(WorkflowEvent workflowEvent, CancellationToken token)
    {
        try
        {
            string logFilePath = GetWorkflowEventLogFilePath();
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string eventType = workflowEvent.GetType().Name;

            string logEntry = $"[{timestamp}] {eventType}";

            // Add event-specific details to the log entry
            switch (workflowEvent)
            {
                case AgentResponseUpdateEvent updateEvent:
                    logEntry += $" - Executor: {updateEvent.ExecutorId}, ResponseId: {updateEvent.Update.ResponseId ?? "null"}, MessageId: {updateEvent.Update.MessageId ?? "null"}";
                    if (updateEvent.Update.Contents.Count > 0)
                    {
                        logEntry += $", Contents Count: {updateEvent.Update.Contents.Count}";
                        logEntry += $", Text: \"{updateEvent.Update.Text}\"";
                    }

                    break;

                case ExecutorCompletedEvent completedEvent:
                    logEntry += $" - Executor: {completedEvent.ExecutorId}";
                    break;

                case MagenticPlanCreatedEvent planCreated:
                    logEntry += $" - Plan Length: {planCreated.FullTaskLedger.Text?.Length ?? 0} chars";
                    break;

                case MagenticReplannedEvent replanned:
                    logEntry += $" - Replan Length: {replanned.FullTaskLedger.Text?.Length ?? 0} chars";
                    break;

                case MagenticProgressLedgerUpdatedEvent progressUpdated:
                    MagenticProgressLedger ledger = progressUpdated.ProgressLedger;
                    logEntry += $" - Satisfied: {ledger.IsRequestSatisfied}, InLoop: {ledger.IsInLoop}, Progressing: {ledger.IsProgressBeingMade}";
                    logEntry += $", NextSpeaker: {ledger.NextSpeaker ?? "null"}";
                    break;

                case WorkflowOutputEvent outputEvent:
                    logEntry += $" - Data Type: {outputEvent.Data?.GetType().FullName ?? "null"}";
                    if (outputEvent.Data != null)
                    {
                        string dataStr = outputEvent.Data.ToString();

                        logEntry += $", Data: {dataStr}";
                    }

                    break;

                case WorkflowErrorEvent workflowError:
                    logEntry += $" - Exception: {workflowError.Exception?.Message ?? "Unknown error"}";
                    if (workflowError.Exception != null)
                    {
                        logEntry += $" - Type: {workflowError.Exception.GetType().FullName}";
                    }

                    break;

                case ExecutorFailedEvent executorFailed:
                    logEntry += $" - Executor: {executorFailed.ExecutorId}";
                    logEntry += $" - Error: {executorFailed.Data?.ToString() ?? "unknown error"}";
                    break;

                case ExecutorInvokedEvent executorInvokedEvent:
                    logEntry += $" - Executor: {executorInvokedEvent.ExecutorId}";
                    break;

                default:
                    logEntry += " - No additional details";
                    break;
            }

            logEntry += Environment.NewLine;

            await SaveLinesToFileAsync(logFilePath, logEntry, token);
        }
        catch (Exception ex)
        {
            // Don't let logging failures break the workflow
            Debug.WriteLine($"Failed to log workflow event to file: {ex.Message}");
        }
    }








    private void PrintLogSpace()
    {
        Debug.WriteLine(Environment.NewLine);
        Debug.WriteLine(new string('-', 60));
    }








    private async Task ProcessResults(CancellationToken token, Run run)
    {
        const bool ShowWorkflowEvents = false;
        const bool ShowToolCalls = true;
        const bool ShowToolResults = true;
        const bool ShowAgentMessages = false;
        const bool ShowUnknownEvents = true;

        Dictionary<string, (string Agent, StringBuilder Text)> responseBuffers = new(StringComparer.Ordinal);

        static string GetSpeaker(ChatMessage message) => message.AuthorName ?? message.Role.Value ?? "Unknown";

        static string GetAgentName(string? agentId) => string.IsNullOrWhiteSpace(agentId) ? "Agent" : agentId;



        static string ExtractTextFromContents(IEnumerable<AIContent>? contents)
        {
            if (contents is null)
            {
                return string.Empty;
            }

            StringBuilder builder = new();

            foreach (AIContent content in contents)
                switch (content)
                {
                    case TextContent textContent:
                        builder.Append(textContent.Text);
                        break;

                }

            return builder.ToString();
        }



        void AppendUpdateText(string responseId, string agent, string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            if (!responseBuffers.TryGetValue(responseId, out (string Agent, StringBuilder Text) entry))
            {
                entry = (agent, new StringBuilder());
                responseBuffers[responseId] = entry;
            }

            entry.Text.Append(text);
        }



        Debug.WriteLine("");
        Debug.WriteLine("============================================================");
        Debug.WriteLine("WORKFLOW TRACE");
        Debug.WriteLine("============================================================");
        Debug.WriteLine("");

        foreach (WorkflowEvent evt in run.OutgoingEvents)
        {
            await LogWorkflowEventToFileAsync(evt, token);

            if (ShowWorkflowEvents)
            {
                Debug.WriteLine($"[EVENT] {evt.GetType().Name} -> {evt.Data?.GetType().FullName ?? "<null>"}");
            }

            switch (evt.Data)
            {
                //------------------------------------------------------------------
                // COLLECTION OF CHAT MESSAGES
                //------------------------------------------------------------------
                case ICollection<ChatMessage> messages:
                    {
                        foreach (ChatMessage message in messages)
                        {
                            string speaker = GetSpeaker(message);

                            string text = !string.IsNullOrWhiteSpace(message.Text) ? message.Text : ExtractTextFromContents(message.Contents);

                            if (ShowAgentMessages && !string.IsNullOrWhiteSpace(text))
                            {
                                Debug.WriteLine($"[{speaker}]");
                                Debug.WriteLine(text);
                                Debug.WriteLine("");
                            }

                            if (ShowToolCalls || ShowToolResults)
                            {
                                LogAIContents(speaker, message.Contents);
                            }
                        }

                        break;
                    }

                //------------------------------------------------------------------
                // SINGLE CHAT MESSAGE
                //------------------------------------------------------------------
                case ChatMessage message:
                    {
                        string speaker = GetSpeaker(message);

                        string text = !string.IsNullOrWhiteSpace(message.Text) ? message.Text : ExtractTextFromContents(message.Contents);

                        if (ShowAgentMessages && !string.IsNullOrWhiteSpace(text))
                        {
                            Debug.WriteLine($"[{speaker}]");
                            Debug.WriteLine(text);
                            Debug.WriteLine("");
                        }

                        if (ShowToolCalls || ShowToolResults)
                        {
                            LogAIContents(speaker, message.Contents);
                        }

                        break;
                    }

                //------------------------------------------------------------------
                // RESPONSE UPDATES
                //------------------------------------------------------------------
                case AgentResponseUpdate update:
                    {
                        string responseId = update.ResponseId ?? update.MessageId ?? Guid.NewGuid().ToString();

                        string agent = GetAgentName(update.AgentId);

                        string updateText = !string.IsNullOrWhiteSpace(update.Text) ? update.Text : ExtractTextFromContents(update.Contents);

                        AppendUpdateText(responseId, agent, updateText);

                        if (ShowToolCalls || ShowToolResults)
                        {
                            LogAIContents(agent, update.Contents);
                        }

                        break;
                    }

                //------------------------------------------------------------------
                // FINAL AGENT RESPONSE
                //------------------------------------------------------------------
                case AgentResponse response:
                    {
                        string agent = GetAgentName(response.AgentId);

                        string text = !string.IsNullOrWhiteSpace(response.Text) ? response.Text : string.Join(Environment.NewLine, response.Messages?.SelectMany(m => m.Contents).OfType<TextContent>().Select(t => t.Text) ?? Enumerable.Empty<string>());

                        if (ShowAgentMessages && !string.IsNullOrWhiteSpace(text))
                        {
                            Debug.WriteLine("");
                            Debug.WriteLine("============================================================");
                            Debug.WriteLine($"FINAL RESPONSE : {agent}");
                            Debug.WriteLine("============================================================");
                            Debug.WriteLine(text);
                            Debug.WriteLine("");
                        }

                        if (response.Messages is not null)
                        {
                            foreach (ChatMessage msg in response.Messages)
                                LogAIContents(GetSpeaker(msg), msg.Contents);
                        }

                        break;
                    }

                //------------------------------------------------------------------
                // UNKNOWN
                //------------------------------------------------------------------
                default:
                    {
                        if (ShowUnknownEvents)
                        {
                            Debug.WriteLine($"[UNHANDLED] {evt.Data?.GetType().FullName ?? "<null>"}");
                        }

                        break;
                    }
            }
        }

        if (responseBuffers.Count > 0)
        {
            Debug.WriteLine("");
            Debug.WriteLine("============================================================");
            Debug.WriteLine("REHYDRATED AGENT RESPONSES");
            Debug.WriteLine("============================================================");
            Debug.WriteLine("");

            foreach ((string Agent, StringBuilder Text) response in responseBuffers.Values)
            {
                if (response.Text.Length == 0)
                {
                    continue;
                }

                Debug.WriteLine($"[{response.Agent}]");
                Debug.WriteLine(response.Text.ToString());
                Debug.WriteLine("");
            }
        }

        Debug.WriteLine("");
        Debug.WriteLine("============================================================");
        Debug.WriteLine("END OF RUN");
        Debug.WriteLine("============================================================");
    }








    /// <summary>
    ///     Appends the specified lines to a file asynchronously.
    /// </summary>
    /// <param name="filePath">The path to the file where lines should be saved.</param>
    /// <param name="lines">The text to append to the file.</param>
    /// <param name="token">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="Task" /> representing the asynchronous operation.</returns>
    public async Task SaveLinesToFileAsync(string filePath, string lines, CancellationToken token)
    {
        try
        {
            await File.AppendAllTextAsync(filePath, lines, token).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            Debug.WriteLine($"Failed to save lines to file {filePath}: {e.Message}");
            throw;
        }
    }








    //Temporary logging
    private static void WriteMagenticMessage(string title, string? content)
    {
        Debug.WriteLine("");
        Debug.WriteLine($"[Magentic {title}]");
        Debug.WriteLine(content);
    }








    private static void WriteStreamingUpdate(AgentResponseUpdateEvent updateEvent, ref string? lastResponseId)
    {
        string responseId = updateEvent.Update.ResponseId ?? updateEvent.Update.MessageId ?? updateEvent.ExecutorId;
        if (!string.Equals(responseId, lastResponseId, StringComparison.Ordinal))
        {
            if (lastResponseId is not null)
            {
                Console.WriteLine();
                Console.WriteLine();
            }

            Console.Write($"- Stream Update {updateEvent.ExecutorId}: ");
            lastResponseId = responseId;
        }

        if (!string.IsNullOrEmpty(updateEvent.Update.Text))
        {

            Console.Write(updateEvent.Update.Text);
        }
    }
}