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
/// <summary>
///     Instrumented harness orchestration that runs a classifier agent followed by a Magentic investigation team so
///     agent and workflow behavior can be exercised outside of a production orchestration.
/// </summary>
/// <remarks>
///     The harness owns all output. Streaming updates, Magentic plan and progress events, tool traffic, and failures are
///     written to the console; the final conversation transcript and report are written to the Debug window. Nothing is
///     returned to the caller.
/// </remarks>
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
    private Workflow _magSubworkflow;
    private AIAgent _magWorkflowAgent;
    private AIAgent _managerAgent;
    private AIAgent _researcherAgent;
    private ExecutorBinding _validatorBinding;
    private AIAgent _evidenceCollectorAgent2;

    /// <summary>Serializes lazy agent initialization so concurrent executions cannot create duplicate agents.</summary>
    private readonly SemaphoreSlim _initializationGate = new(1, 1);

    /// <summary>Upper bound for creating every agent the workflow needs.</summary>
    private static readonly TimeSpan AgentInitializationTimeout = TimeSpan.FromMinutes(5);

    /// <summary>Upper bound for the classifier agent call that produces the investigation objective.</summary>
    private static readonly TimeSpan ClassificationTimeout = TimeSpan.FromMinutes(10);

    /// <summary>Upper bound for the whole streaming run so a stalled provider or executor cannot block termination.</summary>
    private static readonly TimeSpan ExecutionTimeout = TimeSpan.FromMinutes(45);

    /// <summary>Upper bound for starting the run and scheduling the turn token.</summary>
    private static readonly TimeSpan RunStartupTimeout = TimeSpan.FromMinutes(2);

    /// <summary>Upper bound for disposing the run once streaming has ended.</summary>
    private static readonly TimeSpan RunShutdownTimeout = TimeSpan.FromSeconds(30);

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








    /// <summary>
    ///     Initializes a new instance of the <see cref="CustomGroupWorkflow" /> class.
    /// </summary>
    /// <param name="systemReporter">Reports system level events and failures to the host.</param>
    /// <param name="serviceProvider">Resolves the non-agent executors created by <see cref="ExecutorFactory" />.</param>
    /// <param name="agentFactory">Creates every agent that participates in the workflow.</param>
    /// <param name="logger">Creates the loggers used by the workflow base class.</param>
    /// <exception cref="ArgumentNullException">Thrown when any argument is <see langword="null" />.</exception>
    public CustomGroupWorkflow(ISystemReporter systemReporter, IServiceProvider serviceProvider, ISentinelAgentFactory agentFactory, ILoggerFactory logger) : base(systemReporter, logger)
    {
        Throw.IfNull(systemReporter);
        Throw.IfNull(serviceProvider);
        Throw.IfNull(agentFactory);
        Throw.IfNull(logger);

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
    ///     Always <c>null</c>. The harness consumes the workflow event stream itself and reports no event or result data
    ///     to the caller.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="inputMessage" /> is <see langword="null" />.</exception>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="token" /> is cancelled by the caller.</exception>
    [Experimental("MAAI001")]
    public async Task<IAsyncEnumerable<WorkflowEvent>?> ExecuteStreamingAsync(ChatMessage inputMessage, CancellationToken token)
    {
        Throw.IfNull(inputMessage);

        using CancellationTokenSource executionCts = CancellationTokenSource.CreateLinkedTokenSource(token);
        executionCts.CancelAfter(ExecutionTimeout);

        CancellationToken executionToken = executionCts.Token;

        try
        {
            await EnsureAgentInitializedAsync(executionToken).ConfigureAwait(false);

            AIAgent classifierAgent = _classifierAgent ?? throw new InvalidOperationException($"{nameof(CustomGroupWorkflow)} requires an initialized classifier agent before execution.");
            Workflow magSubworkflow = _magSubworkflow ?? throw new InvalidOperationException($"{nameof(CustomGroupWorkflow)} requires an initialized magnetic group sub-workflow before execution.");

            //======================= Signal Classification =========================

            AgentRunOptions options = new() { AllowBackgroundResponses = null, AdditionalProperties = null, ResponseFormat = ChatResponseFormat.ForJsonSchema(typeof(InvestigationObjective)) };
            AgentSession session = await classifierAgent.CreateSessionAsync(executionToken).AsTask().WaitAsync(ClassificationTimeout, executionToken).ConfigureAwait(false);
            AgentResponse<InvestigationObjective> response = await classifierAgent.RunAsync<InvestigationObjective>(inputMessage, session, null, options).WaitAsync(ClassificationTimeout, executionToken).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(response.Text))
            {
                ReportExecutionFailure("Signal classification produced an empty investigation objective; the magnetic group workflow was not started.");
                return null;
            }


                    //================================== Magnetic Group workflow  =========================

                    StreamingRun run = await InProcessExecution.RunStreamingAsync(magSubworkflow, input: response.Text, cancellationToken: executionToken).AsTask().WaitAsync(RunStartupTimeout, executionToken).ConfigureAwait(false);

                    try
                    {
                        bool turnAccepted = await run.TrySendMessageAsync(new TurnToken(emitEvents: true)).AsTask().WaitAsync(RunStartupTimeout, executionToken).ConfigureAwait(false);
                        if (!turnAccepted)
                        {
                            ReportExecutionFailure("The magnetic group workflow rejected the turn token, so no work was scheduled.");
                            return null;
                        }

                        string? lastResponseId = null;
                        WorkflowOutputEvent? finalOutput = null;
                        StringBuilder streamedAnswer = new();

                        await foreach (WorkflowEvent workflowEvent in run.WatchStreamAsync(blockOnPendingRequest: false, executionToken).ConfigureAwait(false))
                            //  ProcessResults(run,token);

                            switch (workflowEvent)
                            {
                                case AgentResponseUpdateEvent updateEvent:
                                    WriteStreamingUpdate(updateEvent, ref lastResponseId, streamedAnswer);
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

                                case WorkflowOutputEvent outputEvent:
                                    finalOutput = outputEvent;
                                    break;

                                case WorkflowErrorEvent workflowError:
                                    ReportExecutionFailure("The workflow reported an error.", workflowError.Exception);
                                    break;

                                case ExecutorFailedEvent executorFailed:
                                    ReportExecutionFailure($"Executor '{executorFailed.ExecutorId}' failed.", executorFailed.Data);
                                    break;
                            }

                        List<ChatMessage>? transcript = null;
                        string? reportText = null;

                        if (finalOutput is not null)
                        {
                            CaptureFinalOutput(finalOutput, ref transcript, ref reportText);
                        }

                        if (transcript is not { Count: > 0 } && string.IsNullOrWhiteSpace(reportText) && streamedAnswer.Length > 0)
                        {
                            reportText = streamedAnswer.ToString();
                        }

                        WriteFinalTranscriptToDebugWindow(transcript, reportText);
                    }
                    finally
                    {
                        await DisposeRunAsync(run).ConfigureAwait(false);
                    }
                }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                // Caller initiated cancellation is not a harness failure, so it propagates instead of being reported.
                throw;
            }
            catch (OperationCanceledException ex)
            {
                ReportExecutionFailure($"The workflow did not finish within the {ExecutionTimeout.TotalMinutes:0} minute execution budget and was cancelled.", ex);
            }
            catch (TimeoutException ex)
            {
                ReportExecutionFailure("A workflow step did not complete within its allotted time.", ex);
            }
            catch (Exception ex)
            {
                ReportExecutionFailure("The workflow execution failed.", ex);
            }

            //Leave this method returning null this test harness handles the streaming output directly. The return value is not used in this context.
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
                .AddEdge(_classifierAgent, _magWorkflowAgent) // Mag group executes and hands to final reporter
                                                              //   .AddEdge(_magSubworkflow, _finalReportWriterAgent) // Consolodates and creates final report and sends to evaluation executor
                .WithName("InvestigationWorkflow") //Final report is sent to the manager for review and approval
                .WithDescription("Coordinates the investigation workflow by connecting the classifier, investigation group, and evaluation executor.")
                .WithOpenTelemetry(activitySource: SentinelCoreDiagnostics.SharedActivitySource)
                .Build();

        return classflow;


    }








    /// <summary>
    ///     Lazily initializes every agent the workflow needs on first use, avoiding the sync-over-async deadlocks that
    ///     occur when building agents in the constructor.
    /// </summary>
    /// <remarks>
    ///     Initialization is serialized by <see cref="_initializationGate" /> so concurrent executions cannot create
    ///     duplicate agents, and the whole creation sequence is bounded by <see cref="AgentInitializationTimeout" /> so an
    ///     unresponsive provider or tool server cannot stall execution. Failures are reported and rethrown so the caller
    ///     can surface a meaningful error instead of failing later with a <see cref="NullReferenceException" />.
    /// </remarks>
    /// <param name="token">A <see cref="CancellationToken" /> to observe while initializing.</param>
    /// <returns>A task that represents the asynchronous initialization operation.</returns>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="token" /> is cancelled.</exception>
    [Experimental("MAAI001")]
    private async Task EnsureAgentInitializedAsync(CancellationToken token)
    {
        if (_agentInitialized)
        {
            return;
        }

        await _initializationGate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            if (_agentInitialized)
            {
                return;
            }

            await CreateAgentsAsync(token).WaitAsync(AgentInitializationTimeout, token).ConfigureAwait(false);

            _agentInitialized = true;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // Caller cancellation is not an initialization failure, so it propagates without being reported.
            throw;
        }
        catch (Exception e)
        {
            ReportExecutionFailure("Failed to initialize the custom group workflow agents.", e);
            throw;
        }
        finally
        {
            _initializationGate.Release();
        }
    }

    /// <summary>
    ///     Creates every agent that participates in the workflow and builds the magnetic group sub-workflow.
    /// </summary>
    /// <param name="token">A <see cref="CancellationToken" /> observed by the agent creation calls that support cancellation.</param>
    /// <returns>A task that represents the asynchronous creation operation.</returns>
    [Experimental("MAAI001")]
    private async Task CreateAgentsAsync(CancellationToken token)
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
            _magSubworkflow = BuildSubWorkflow();

            //         AIAgent classifyingAIAgent = await _agentFactory.CreateAgentAsync(AgentPreset.ClassifierAgent, AgentInstructionConstants.SENTINELCORE_OBJECTIVE_GEN, ChatResponseFormat.Json, token);

            // Classifier agent creates a structured objective with as much as can be inferred from the initial input signal. The preset has domain basics here we supply with task specific instructions.
            // We will be reusing agent, can we?
            _classifierAgent = await _agentFactory.CreateAgentAsync(AgentPreset.ClassifierAgent, AgentInstructionConstants.SENTINELCORE_OBJECTIVE_GEN, token).ConfigureAwait(false);

            //  -----------------── Construct NON-Agent executors ────────────────────────────────────────────────────
            //



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








    /// <summary>
    ///     Writes the contents of an agent message (text, tool traffic, usage, and non-text content) to the console.
    /// </summary>
    /// <param name="agent">The name of the agent that produced the contents.</param>
    /// <param name="contents">The contents to write, if any.</param>
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
                            Console.WriteLine($"[{agent}]");
                            Console.WriteLine(text.Text);
                            Console.WriteLine("");
                        }

                        break;
                    }

                case FunctionCallContent functionCall:
                    {
                        Console.WriteLine($"[{agent}] [tool-call] " + $"{functionCall.Name}({functionCall.Arguments})");

                        Console.WriteLine("");
                        break;
                    }

                case FunctionResultContent functionResult:
                    {
                        Console.WriteLine($"[{agent}] [tool-result] " + $"{functionResult.CallId} -> {functionResult.Result}");

                        Console.WriteLine("");
                        break;
                    }

                case UsageContent usage:
                    {
                        Console.WriteLine($"[{agent}] [usage] " + $"Input={usage.Details?.InputTokenCount ?? 0}, " + $"Output={usage.Details?.OutputTokenCount ?? 0}");

                        break;
                    }

                case DataContent data:
                    {
                        Console.WriteLine($"[{agent}] [data] {data.MediaType}");

                        break;
                    }

                case UriContent uri:
                    {
                        Console.WriteLine($"[{agent}] [uri] {uri.Uri}");

                        break;
                    }

                default:
                    {
                        Console.WriteLine($"[{agent}] [content:{content.GetType().Name}]");

                        Console.WriteLine(content.ToString());

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
            Console.WriteLine($"Failed to log workflow event to file: {ex.Message}");
        }
    }








    /// <summary>
    ///     Writes a visual separator to the console between logical sections of harness output.
    /// </summary>
    private static void PrintLogSpace()
    {
        Console.WriteLine(Environment.NewLine);
        Console.WriteLine(new string('-', 60));
    }







    /// <summary>
    /// To process non streaming run events
    /// </summary>
    /// <param name="run"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    private async Task ProcessResults(Run run, CancellationToken token)
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



        Console.WriteLine("");
        Console.WriteLine("============================================================");
        Console.WriteLine("WORKFLOW TRACE");
        Console.WriteLine("============================================================");
        Console.WriteLine("");

        foreach (WorkflowEvent evt in run.OutgoingEvents)
        {
            await LogWorkflowEventToFileAsync(evt, token);

            if (ShowWorkflowEvents)
            {
                Console.WriteLine($"[EVENT] {evt.GetType().Name} -> {evt.Data?.GetType().FullName ?? "<null>"}");
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
                                Console.WriteLine($"[{speaker}]");
                                Console.WriteLine(text);
                                Console.WriteLine("");
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
                            Console.WriteLine($"[{speaker}]");
                            Console.WriteLine(text);
                            Console.WriteLine("");
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
                            Console.WriteLine("");
                            Console.WriteLine("============================================================");
                            Console.WriteLine($"FINAL RESPONSE : {agent}");
                            Console.WriteLine("============================================================");
                            Console.WriteLine(text);
                            Console.WriteLine("");
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
                            Console.WriteLine($"[UNHANDLED] {evt.Data?.GetType().FullName ?? "<null>"}");
                        }

                        break;
                    }
            }
        }

        if (responseBuffers.Count > 0)
        {
            Console.WriteLine("");
            Console.WriteLine("============================================================");
            Console.WriteLine("REHYDRATED AGENT RESPONSES");
            Console.WriteLine("============================================================");
            Console.WriteLine("");

            foreach ((string Agent, StringBuilder Text) response in responseBuffers.Values)
            {
                if (response.Text.Length == 0)
                {
                    continue;
                }

                Console.WriteLine($"[{response.Agent}]");
                Console.WriteLine(response.Text.ToString());
                Console.WriteLine("");
            }
        }

        Console.WriteLine("");
        Console.WriteLine("============================================================");
        Console.WriteLine("END OF RUN");
        Console.WriteLine("============================================================");
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
            Console.WriteLine($"Failed to save lines to file {filePath}: {e.Message}");
            throw;
        }
    }








    //Temporary logging
    /// <summary>
    ///     Writes a Magentic orchestration message (initial plan, replan, or progress ledger) to the console.
    /// </summary>
    /// <param name="title">The Magentic message category.</param>
    /// <param name="content">The message content, if any.</param>
    private static void WriteMagenticMessage(string title, string? content)
    {
        Console.WriteLine();
        Console.WriteLine($"[Magentic {title}]");
        Console.WriteLine(content);
    }








    /// <summary>
    ///     Writes a streamed agent update to the console and accumulates its text.
    /// </summary>
    /// <remarks>
    ///     The accumulated text is the fallback for runs that never emit a final output event, so the Debug window still
    ///     receives the last agent answer instead of nothing.
    /// </remarks>
    /// <param name="updateEvent">The streamed update event.</param>
    /// <param name="lastResponseId">The response identifier of the previously written update.</param>
    /// <param name="streamedAnswer">Accumulates the text of the response currently being streamed.</param>
    private static void WriteStreamingUpdate(AgentResponseUpdateEvent updateEvent, ref string? lastResponseId, StringBuilder streamedAnswer)
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
            streamedAnswer.Clear();
        }

        if (!string.IsNullOrEmpty(updateEvent.Update.Text))
        {
            streamedAnswer.Append(updateEvent.Update.Text);
            Console.Write(updateEvent.Update.Text);
        }
    }

    /// <summary>
    ///     Extracts the conversation transcript and report text from a workflow output event.
    /// </summary>
    /// <remarks>
    ///     The Magentic workflow can surface its final output as a message list, a single message, an
    ///     <see cref="AgentResponse" />, or plain text depending on the configured output executor, so every documented
    ///     shape is accepted instead of silently dropping the transcript.
    /// </remarks>
    /// <param name="outputEvent">The output event emitted by the workflow.</param>
    /// <param name="transcript">Receives the conversation transcript when the output carries one.</param>
    /// <param name="reportText">Receives the report text when the output carries one.</param>
    private static void CaptureFinalOutput(WorkflowOutputEvent outputEvent, ref List<ChatMessage>? transcript, ref string? reportText)
    {
        switch (outputEvent.Data)
        {
            case List<ChatMessage> messages:
                transcript = messages;
                break;

            case IReadOnlyList<ChatMessage> readOnlyMessages:
                transcript = [.. readOnlyMessages];
                break;

            case ChatMessage message:
                transcript = [message];
                break;

            case AgentResponse agentResponse:
                transcript = agentResponse.Messages?.ToList();
                reportText = agentResponse.Text;
                break;

            case string text:
                reportText = text;
                break;
        }
    }

    /// <summary>
    ///     Writes the final conversation transcript and report to the Debug window.
    /// </summary>
    /// <remarks>
    ///     This is the only harness output that belongs in the Debug window; every other output is console output.
    ///     <see cref="Trace" /> is used instead of <see cref="Debug" /> because <see cref="Debug" /> calls are compiled out of
    ///     Release builds, which silently suppressed the transcript, and it is flushed immediately so the transcript is
    ///     visible before the harness process exits.
    /// </remarks>
    /// <param name="transcript">The final conversation transcript, if the workflow produced one.</param>
    /// <param name="reportText">The final report text, if the workflow produced one.</param>
    private static void WriteFinalTranscriptToDebugWindow(IReadOnlyList<ChatMessage>? transcript, string? reportText)
    {
        StringBuilder output = new();
        output.AppendLine();
        output.AppendLine(new string('=', 80));
        output.AppendLine("Final Conversation Transcript:");
        output.AppendLine(new string('=', 80));
        output.AppendLine();

        if (transcript is { Count: > 0 })
        {
            foreach (ChatMessage message in transcript)
            {
                output.AppendLine($"{message.AuthorName ?? message.Role.ToString()}: {message.Text}");
                output.AppendLine();
            }
        }
        else
        {
            output.AppendLine("(the workflow produced no conversation transcript)");
            output.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(reportText))
        {
            output.AppendLine(new string('=', 80));
            output.AppendLine("Final Report:");
            output.AppendLine(new string('=', 80));
            output.AppendLine();
            output.AppendLine(reportText);
            output.AppendLine();
        }

        Trace.WriteLine(output.ToString());
        Trace.Flush();
    }

    /// <summary>
    ///     Reports an execution failure to the console and to the system reporter without terminating the harness.
    /// </summary>
    /// <param name="message">A description of the failure.</param>
    /// <param name="exception">The exception that caused the failure, if any.</param>
    private void ReportExecutionFailure(string message, Exception? exception = null)
    {
        try
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(exception is null ? message : $"{message}{Environment.NewLine}{exception}");
        }
        finally
        {
            Console.ResetColor();
        }

        _reporter.ReportError(message, exception);
    }

    /// <summary>
    ///     Disposes a streaming run without letting a stuck run prevent method termination.
    /// </summary>
    /// <param name="run">The run to dispose.</param>
    /// <returns>A task that represents the asynchronous dispose operation.</returns>
    private async Task DisposeRunAsync(StreamingRun run)
    {
        try
        {
            await run.DisposeAsync().AsTask().WaitAsync(RunShutdownTimeout).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            ReportExecutionFailure($"The workflow run did not dispose within {RunShutdownTimeout.TotalSeconds:0} seconds, so the harness stopped waiting for it.");
        }
        catch (Exception ex)
        {
            ReportExecutionFailure("The workflow run failed while being disposed.", ex);
        }
    }
}