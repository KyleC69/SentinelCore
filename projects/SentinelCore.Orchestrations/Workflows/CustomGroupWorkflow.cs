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

        Workflow workflow = BuildWorkflow();

        string sessionid = Guid.NewGuid().ToString();
        string? lastResponseId = null;
        WorkflowOutputEvent? finalOutput = null;



        await using (StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, inputMessage, sessionid, token))
        {
            await run.TrySendMessageAsync(new TurnToken(true));
            await foreach (WorkflowEvent workflowEvent in run.WatchStreamAsync(token))
                switch (workflowEvent)
                {
                    case AgentResponseUpdateEvent updateEvent:


                        // Log Tool content if present. This is useful for debugging tool calls and results.
                        if (updateEvent.Update.Contents.Count > 0)
                        {
                            LogToolContent(updateEvent.ExecutorId, updateEvent.Update.Contents);

                        }

                        // Stream per-participant deltas. Group by ResponseId / MessageId / ExecutorId so
                        // each new contiguous response prints its executor header once.
                        string responseId = updateEvent.Update.ResponseId ?? updateEvent.Update.MessageId ?? updateEvent.ExecutorId;
                        if (!string.Equals(responseId, lastResponseId, StringComparison.Ordinal))
                        {
                            if (lastResponseId is not null)
                            {
                                Debug.WriteLine(Environment.NewLine);
                            }

                            Debug.Write($"- {updateEvent.ExecutorId}: ");
                            lastResponseId = responseId;
                        }

                        Debug.Write(updateEvent.Update.Text);
                        // LogToolContent(updateEvent.ExecutorId, updateEvent.Update.Contents);
                        break;
                    case ExecutorCompletedEvent executorCompletedEvent:
                        //    Debug.WriteLine($"Executor '{executorCompletedEvent.ExecutorId}' completed.");
                        //  PrintLogSpace();
                        break;

                    case MagenticPlanCreatedEvent planCreated:
                        Debug.WriteLine($"\n[Magentic Initial Plan]\n{planCreated.FullTaskLedger.Text}");
                        PrintLogSpace();
                        break;

                    case MagenticReplannedEvent replanned:
                        Debug.WriteLine($"\n[Magentic Replanned]\n{replanned.FullTaskLedger.Text}");
                        PrintLogSpace();
                        break;

                    case MagenticProgressLedgerUpdatedEvent progressUpdated:
                        MagenticProgressLedger ledger = progressUpdated.ProgressLedger;
                        Debug.WriteLine($"\n[Magentic Progress Ledger] satisfied={ledger.IsRequestSatisfied}, " + $"inLoop={ledger.IsInLoop}, progressing={ledger.IsProgressBeingMade}, " + $"nextSpeaker={ledger.NextSpeaker}, instruction={ledger.InstructionOrQuestion}");
                        PrintLogSpace();
                        break;

                    case WorkflowOutputEvent outputEvent when outputEvent.Is<List<ChatMessage>>():
                        finalOutput = outputEvent;
                        PrintLogSpace();
                        break;

                    case WorkflowErrorEvent workflowError:
                        Debug.WriteLine(workflowError.Exception?.ToString() ?? "Unknown workflow error.");
                        break;

                    case ExecutorFailedEvent executorFailed:
                        Debug.WriteLine($"Executor '{executorFailed.ExecutorId}' failed: " + (executorFailed.Data?.ToString() ?? "unknown error"));
                        break;
                    case ExecutorInvokedEvent executorInvokedEvent:
                        // Debug.WriteLine($"Executor '{executorInvokedEvent.ExecutorId}' invoked.");
                        // PrintLogSpace();
                        break;
                }

            if (finalOutput?.As<List<ChatMessage>>() is { } transcript)
            {
                Debug.WriteLine("\n\n=== Final Conversation Transcript ===\n");
                foreach (ChatMessage message in transcript) Debug.WriteLine($"{message.AuthorName ?? message.Role.ToString()}: {message.Text}");
            }


            return null;




        }

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
    private ExecutorBinding BuildSubWorkflow()
    {
        MagenticPromptOverrides over = new() { FinalAnswerPrompt = Orchestrations.MagenticPromptOverrides.FinalAnswerPrompt, TaskLedgerPlanPrompt = Orchestrations.MagenticPromptOverrides.TaskLedgerPlanPrompt };
        //-------------------  Magentic Group workflow  -------------------
        Workflow workflow = new MagenticWorkflowBuilder(_managerAgent).AddParticipants(_evidenceCollectorAgent, _adversarialAnalystAgent, _researcherAgent, _finalReportWriterAgent) //Test run with adversarial agent to challenge assumptions and keep other honest.
                .WithName("ForensicsInvestigationTeamWorkflow")
                .WithDescription("A powerful manager coordinates the investigation team and monitors the activities and progress of the evidence collector, adversarial analyst, and researcher.")
                .RequirePlanSignoff(false)
                .WithPromptOverrides(over)
                .WithOutputFrom(_finalReportWriterAgent)
                .WithMaxRounds(10)
                .WithMaxResets(2)
                .Build();



        return workflow.BindAsExecutor("ForensicsInvestigationTeamWorkflow");
    }








    [Experimental("MAAI001")]
    private Workflow BuildWorkflow()
    {
        ExecutorCollection executors = _executorFactory.CreateAllExecutors();
        ExecutorBinding magSubworkflow = BuildSubWorkflow();
        ExecutorBinding finalReporterExec = _finalReportWriterAgent.BindAsExecutor();
        ClassifierAgentExec classifierExec = new(_classifierAgent, _reporter);



        //------------------- PARENT workflow  -------------------a
        Workflow classflow = new WorkflowBuilder(executors.NewCaseExecutor) //Create New case
                .AddEdge(executors.NewCaseExecutor, executors.SignalClassifierExecutor) // Build objective and send to Mag group
                .AddEdge(executors.SignalClassifierExecutor, magSubworkflow) // Mag group executes and hands to final reporter
                .AddEdge(magSubworkflow, finalReporterExec) // Consolodates and creates final report and sends to evaluation executor
                .AddEdge(finalReporterExec, _validatorBinding) // All evidence is validate and evaluated and a final report is generated
                .AddEdge(_validatorBinding, new TerminateWorkflow(_reporter)) // .AddEdge(_evalExec, executors.TerminateWorkflow) // All data is persisted for audit trail status is set!
                .WithName("InvestigationWorkflow") //Final report is sent to the manager for review and approval
                .WithDescription("Coordinates the investigation workflow by connecting the classifier, investigation group, and evaluation executor.")
                .Build();

        return classflow;
    }








    /// <summary>
    ///     Lazily initializes the agent on first use, avoiding sync-over-async
    ///     deadlocks that occur when building the agent in the constructor.
    /// </summary>
    private async Task EnsureAgentInitializedAsync(CancellationToken token)
    {
        if (_agentInitialized)
        {
            return;
        }

        try
        {
            _managerAgent = await _agentFactory.CreateAgentAsync(AgentPreset.ManagerAgent, "ManagerCoordinator", "Coordinates the investigation team and monitors progress", AgentInstructionConstants.MAG_MANAGER_INSTRUCTIONS);
            _evidenceCollectorAgent = await _agentFactory.CreateAgentAsync(AgentPreset.WorkerAgent, "EvidenceCollector", "Collects evidence for the investigation", "You are a forensic grade digital evidence collector, you collect and document each piece of evidence meticulously.");
            _adversarialAnalystAgent = await _agentFactory.CreateAgentAsync(AgentPreset.WorkerAgent, "AdversarialAnalyst", "The challenger of the collected evidence", "You are a forensic grade digital evidence debunker, it is your job to challenge the findings of your teammates and validate each piece of evidence. if you can't validate it, it is not definitive.");
            _researcherAgent = await _agentFactory.CreateAgentAsync(AgentPreset.WorkerAgent, "Researcher", "Conducts research related to the investigation and fact checker.", "You are a research assistant, you conduct thorough research to support the investigation.");


            _finalReportWriterAgent = await _agentFactory.CreateAgentAsync(AgentPreset.AggregatorAgent, "CaseReportAuthor", "Aggregates the results from all workers and generates the final case report.", "You are a case report author, you aggregate the results from all workers and generate the final case report.");

            // evidence validator is a skeptical adversarial analyst. Magentic groups may or may not select this type of worker, this will have to be tested in the future. For now it's a separate step/executor
            _evidenceValidationAgent = await _agentFactory.CreateAgentAsync(AgentPreset.WorkerAgent, "EvidenceValidator", "The formal Investigation Auditor will challenge the collected evidence", "You are a forensic grade digital evidence validator, you challenge the findings and validate each piece of evidence. if you can't validate it, it is not definitive.");
            _validatorBinding = _evidenceValidationAgent.BindAsExecutor();


            // Classifier agent creates a structured objective with as much as can be inferred from the initial input signal. The preset has domain basics here we supply with task specific instructions. 
            // We will be reusing agent, can we?
            _classifierAgent = await _agentFactory.CreateAgentAsync(AgentPreset.UtilityAgent, AgentInstructionConstants.SENTINELCORE_OBJECTIVE_GEN, token).ConfigureAwait(false);

            //  -----------------── Construct NON-Agent executors ────────────────────────────────────────────────────
            //
            EvaluationExec evalExec = new("EvaluationExec");

            ExecutorBinding finalWriter = _finalReportWriterAgent.BindAsExecutor();


            Throw.IfNull(_managerAgent);
            Throw.IfNull(_evidenceCollectorAgent);
            Throw.IfNull(_adversarialAnalystAgent);
            Throw.IfNull(_researcherAgent);
            Throw.IfNull(_evidenceValidationAgent);
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
        Debug.WriteLine(Environment.NewLine);
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




}