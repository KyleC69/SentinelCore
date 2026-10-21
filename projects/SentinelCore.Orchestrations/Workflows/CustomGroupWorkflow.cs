// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         CustomGroupWorkflow.cs
// Author: Kyle L. Crowder
// Build Num:  101601



using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using Microsoft.Agents.AI.Workflows.Specialized.Magentic;
using Microsoft.Extensions.Logging;

using SentinelCore.CaseFlowEngine.Infrastructure;
using SentinelCore.Contracts.Abstractions;
using SentinelCore.Orchestrations.Abstractions;
using SentinelCore.Orchestrations.Agents;
using SentinelCore.Orchestrations.Agents.AgentPresets;
using SentinelCore.Orchestrations.Workflows.Executors;




namespace SentinelCore.Orchestrations.Workflows;





//AGENTS IGNORE THIS FILE FOR QUICK TESTING OF WORKFLOWS AND AGENTS. THIS IS NOT A REAL ORCHESTRATION, JUST A HARNESS FOR TESTING.
public sealed class CustomGroupWorkflow : WorkflowBase, IOrchestration
{
    private AIAgent _adversarialAnalystAgent;
    private readonly ISentinelAgentFactory _agentFactory;
    private bool _agentInitialized;

    // --------------  Executors  -------------------



    // --------------  Agents  -------------------
    private AIAgent _classifierAgent;
    private AIAgent _evidenceCollectorAgent;
    private AIAgent _evidenceValidationAgent;
    private readonly ExecutorFactory _executorFactory;
    private AIAgent _finalReportWriterAgent;
    private AIAgent _managerAgent;
    private AIAgent _researcherAgent;
    private ExecutorBinding _validatorBinding;
    private AIAgent _magSubworkflow;








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

        AgentSession session = await _classifierAgent.CreateSessionAsync(token);

        AgentRunOptions options = new() { ResponseFormat = ChatResponseFormat.Json };
        //  AgentResponse<InvestigationObjective> response = await _classifier.RunAsync<InvestigationObjective>(inputMessage, session, null, options);


        //================================== Magnetic Group workflow  =========================

        Workflow workflow = await BuildWorkflow(token);

        string sessionid = Guid.NewGuid().ToString();
        string? lastResponseId = null;
        WorkflowOutputEvent? finalOutput = null;



        await using (StreamingRun run = await InProcessExecution.Lockstep.RunStreamingAsync(workflow, inputMessage, sessionid, token))
        {
            await run.TrySendMessageAsync(new TurnToken(true));
            await foreach (WorkflowEvent workflowEvent in run.WatchStreamAsync(token))
            {
                await LogWorkflowEventToFileAsync(workflowEvent, token);

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
    ///     Magentic subworkflow investigation team
    /// </summary>
    /// <returns></returns>
    [Experimental("MAAI001")]
    private Workflow BuildSubWorkflow()
    {
        MagenticPromptOverrides over = new()
        {
            FinalAnswerPrompt = Orchestrations.MagenticPromptOverrides.FinalAnswerPrompt,
            TaskLedgerPlanPrompt = Orchestrations.MagenticPromptOverrides.TaskLedgerPlanPrompt,
            TaskLedgerFactsPrompt = Orchestrations.MagenticPromptOverrides.TaskLedgerFactsPrompt,
        };
        //-------------------  Magentic Group workflow  -------------------
        Workflow workflow = new MagenticWorkflowBuilder(_managerAgent).AddParticipants(_evidenceCollectorAgent, _adversarialAnalystAgent, _researcherAgent) //Test run with adversarial agent to challenge assumptions and keep other honest.
                .WithName("ForensicsInvestigationTeamWorkflow")
                .WithDescription("A powerful manager coordinates the investigation team and monitors the activities and progress of the evidence collector, adversarial analyst, and researcher.")
                .RequirePlanSignoff(false)
                .WithPromptOverrides(over)
                //    .WithOutputFrom(_finalReportWriterAgent)
                .WithMaxRounds(10)
                .WithMaxResets(2)
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
                .AddEdge(_magSubworkflow, _finalReportWriterAgent) // Consolodates and creates final report and sends to evaluation executor
                .WithName("InvestigationWorkflow") //Final report is sent to the manager for review and approval
                .WithDescription("Coordinates the investigation workflow by connecting the classifier, investigation group, and evaluation executor.")
                .Build();

        return classflow;


    }



    private const string TEAM_MEMBER_INSTRUCTIONS = """
        
                                                    You are a member of a forensic investigation team. Your role is to collaborate with other team members to collect, analyze, and validate evidence related to the investigation. 
                                                    You must follow the instructions provided by the manager and contribute your expertise to ensure the accuracy and integrity of the investigation process.
                                                    Never fabricate or simulate evidence. Your goal is to provide accurate and reliable information to support the investigation and help the team reach a well-informed conclusion.
                                                    If you encounter any challenges or uncertainties, communicate with the manager to seek guidance and clarification. Your commitment to thoroughness, 
                                                    attention to detail, and adherence to ethical standards is essential for the success of the investigation. If you are unable to validate a piece of evidence, you must report it 
                                                    as inconclusive and provide any relevant context or reasoning for your assessment. Never simulate or fabricate evidence, as this could compromise the integrity of the investigation 
                                                    and lead to incorrect conclusions. 
                                                    """;




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
            _evidenceCollectorAgent = await _agentFactory.CreateAgentAsync(AgentPreset.WorkerAgent, "EvidenceCollector", "Collects evidence for the investigation", TEAM_MEMBER_INSTRUCTIONS + " You are a forensic grade digital evidence collector, you collect and document each piece of evidence meticulously.");
            _adversarialAnalystAgent = await _agentFactory.CreateAgentAsync(AgentPreset.WorkerAgent, "AdversarialAnalyst", "The challenger of the collected evidence", TEAM_MEMBER_INSTRUCTIONS + "  You are a forensic grade digital evidence debunker, it is your job to challenge the findings of your teammates and validate each piece of evidence. if you can't validate it, it is not definitive.");
            _researcherAgent = await _agentFactory.CreateAgentAsync(AgentPreset.WorkerAgent, "Researcher", "Conducts research related to the investigation and fact checker.", TEAM_MEMBER_INSTRUCTIONS + "  You are a research assistant, you conduct thorough research to support the investigation.");

            _magSubworkflow = BuildSubWorkflow().AsAIAgent("InvestigationTeam", "ForensicInvestigationTeam", "Subworkflow forming Magentic Group Investigation Team");


            AIAgent classifyingAIAgent = await _agentFactory.CreateAgentAsync(AgentPreset.ClassifierAgent, AgentInstructionConstants.SENTINELCORE_OBJECTIVE_GEN, ChatResponseFormat.Json, token);

            _finalReportWriterAgent = await _agentFactory.CreateAgentAsync(AgentPreset.AggregatorAgent, "CaseReportAuthor", "Aggregates the results from all workers and generates the final case report.", TEAM_MEMBER_INSTRUCTIONS + "  You are a case report author, you aggregate the results from all workers and generate the final case report.");

            // evidence validator is a skeptical adversarial analyst. Magentic groups may or may not select this type of worker, this will have to be tested in the future. For now it's a separate step/executor
            //   _evidenceValidationAgent = await _agentFactory.CreateAgentAsync(AgentPreset.WorkerAgent, "EvidenceValidator", "The formal Investigation Auditor will challenge the collected evidence", "You are a forensic grade digital evidence validator, you challenge the findings and validate each piece of evidence. if you can't validate it, it is not definitive.");
            //   _validatorBinding = _evidenceValidationAgent.BindAsExecutor();


            // Classifier agent creates a structured objective with as much as can be inferred from the initial input signal. The preset has domain basics here we supply with task specific instructions. 
            // We will be reusing agent, can we?
            _classifierAgent = await _agentFactory.CreateAgentAsync(AgentPreset.UtilityAgent, AgentInstructionConstants.SENTINELCORE_OBJECTIVE_GEN, token).ConfigureAwait(false);

            //  -----------------── Construct NON-Agent executors ────────────────────────────────────────────────────
            //
            EvaluationExec evalExec = new("EvaluationExec");




            Throw.IfNull(_managerAgent);
            Throw.IfNull(_evidenceCollectorAgent);
            Throw.IfNull(_adversarialAnalystAgent);
            Throw.IfNull(_researcherAgent);
            //        Throw.IfNull(_evidenceValidationAgent);
            Throw.IfNull(_finalReportWriterAgent);

            _agentInitialized = true;
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.Message);
        }
    }








    private static void LogToolContent(string? executorId, IEnumerable<AIContent> contents)
    {
        foreach (AIContent content in contents)
            switch (content)
            {
                case FunctionCallContent call:
                    Debug.WriteLine($"[tool-call]   {executorId}.{call.Name}({(call.Arguments is { } args ? string.Join(", ", args.Select(kv => $"{kv.Key}={kv.Value}")) : string.Empty)})");
                    break;
                case FunctionResultContent result:
                    Debug.WriteLine($"[tool-result] {result.CallId} -> {result.Result}");
                    break;
            }
    }








    private void PrintLogSpace()
    {
        Debug.WriteLine(Environment.NewLine);
        Debug.WriteLine(new string('-', 60));
    }








    [Experimental("MAAI001")]
    public async Task RunWorkflow()
    {









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
                    var ledger = progressUpdated.ProgressLedger;
                    logEntry += $" - Satisfied: {ledger.IsRequestSatisfied}, InLoop: {ledger.IsInLoop}, Progressing: {ledger.IsProgressBeingMade}";
                    logEntry += $", NextSpeaker: {ledger.NextSpeaker ?? "null"}";
                    break;

                case WorkflowOutputEvent outputEvent:
                    logEntry += $" - Data Type: {outputEvent.Data?.GetType().FullName ?? "null"}";
                    if (outputEvent.Data != null)
                    {
                        string dataStr = outputEvent.Data.ToString();
                        // Truncate very long data to prevent excessively large log entries
                        if (dataStr.Length > 1000)
                        {
                            dataStr = dataStr.Substring(0, 1000) + "... [truncated]";
                        }
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




}