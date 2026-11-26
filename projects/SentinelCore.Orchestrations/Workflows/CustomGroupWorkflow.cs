// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         CustomGroupWorkflow.cs
// Author: Kyle L. Crowder
// Build Num:  112518



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
using SentinelCore.Orchestrations.Infrastructure.DependencyInjection;
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
    private readonly ExecutorFactory _executorFactory;
    private AIAgent _finalReportWriterAgent;

    /// <summary>Serializes lazy agent initialization so concurrent executions cannot create duplicate agents.</summary>
    private readonly SemaphoreSlim _initializationGate = new(1, 1);

    private Workflow _magSubworkflow;
    private AIAgent _magWorkflowAgent;
    private AIAgent _managerAgent;
    private AIAgent _researcherAgent;








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

        _executorFactory = new(serviceProvider);
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
                this.ReportExecutionFailure("Signal classification produced an empty investigation objective; the magnetic group workflow was not started.");
                return null;
            }


            //================================== Magnetic Group workflow  =========================

            StreamingRun run = await InProcessExecution.RunStreamingAsync(magSubworkflow, response.Text, cancellationToken: executionToken).AsTask().WaitAsync(RunStartupTimeout, executionToken).ConfigureAwait(false);

            try
            {
                bool turnAccepted = await run.TrySendMessageAsync(new TurnToken(true)).AsTask().WaitAsync(RunStartupTimeout, executionToken).ConfigureAwait(false);
                if (!turnAccepted)
                {
                    this.ReportExecutionFailure("The magnetic group workflow rejected the turn token, so no work was scheduled.");
                    return null;
                }

                string? lastResponseId = null;
                WorkflowOutputEvent? finalOutput = null;
                StringBuilder streamedAnswer = new();

                await foreach (WorkflowEvent workflowEvent in run.WatchStreamAsync(false, executionToken).ConfigureAwait(false))



                {
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
                            this.ReportExecutionFailure("The workflow reported an error.", workflowError.Exception);
                            break;

                        case ExecutorFailedEvent executorFailed:
                            this.ReportExecutionFailure($"Executor '{executorFailed.ExecutorId}' failed.", executorFailed.Data);
                            break;
                    }
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
            this.ReportExecutionFailure($"The workflow did not finish within the {ExecutionTimeout.TotalMinutes:0} minute execution budget and was cancelled.", ex);
        }
        catch (TimeoutException ex)
        {
            this.ReportExecutionFailure("A workflow step did not complete within its allotted time.", ex);
        }
        catch (Exception ex)
        {
            this.ReportExecutionFailure("The workflow execution failed.", ex);
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
                .WithMaxStalls()
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
    ///     Creates every agent that participates in the workflow and builds the magnetic group sub-workflow.
    /// </summary>
    /// <param name="token">A <see cref="CancellationToken" /> observed by the agent creation calls that support cancellation.</param>
    /// <returns>A task that represents the asynchronous creation operation.</returns>
    [Experimental("MAAI001")]
    private async Task CreateAgentsAsync(CancellationToken token)
    {

        _managerAgent = await _agentFactory.CreateAgentAsync(AgentPreset.ManagerAgent, "ManagerCoordinator", "Coordinates the investigation team and monitors progress", AgentToolSelection.None);
        _evidenceCollectorAgent = await _agentFactory.CreateAgentAsync(AgentPreset.WorkerAgent, "EvidenceCollector", "Collects evidence for the investigation", AgentInstructionConstants.TEAM_MEMBER_INSTRUCTIONS, AgentToolSelection.All);
        _adversarialAnalystAgent = await _agentFactory.CreateAgentAsync(AgentPreset.WorkerAgent, "AdversarialAnalyst", "The challenger of the collected evidence", AgentInstructionConstants.TEAM_MEMBER_INSTRUCTIONS, AgentToolSelection.All);
        _researcherAgent = await _agentFactory.CreateAgentAsync(AgentPreset.WorkerAgent, "Researcher", "Conducts research related to the investigation and fact checker.", AgentInstructionConstants.TEAM_MEMBER_INSTRUCTIONS, AgentToolSelection.All);
        _finalReportWriterAgent = await _agentFactory.CreateAgentAsync(AgentPreset.AggregatorAgent, "CaseReportAuthor", "Aggregates the results from all workers and generates the final case report.", AgentInstructionConstants.AGGREGATOR_INSTRUCTIONS);

        //Participants must be verified and created before the sub-workflow is built. The sub-workflow is built here, but it is not executed directly. Instead, it is added to the parent workflow as a sub-workflow executor. This allows the parent workflow to manage the execution of the sub-workflow and handle its outputs appropriately.
        Throw.IfNull(_managerAgent);
        Throw.IfNull(_evidenceCollectorAgent);
        Throw.IfNull(_adversarialAnalystAgent);
        Throw.IfNull(_researcherAgent);
        Throw.IfNull(_finalReportWriterAgent);

        //Important: The sub-workflow is built here, but it is not executed directly. Instead, it is added to the parent workflow as a sub-workflow executor. This allows the parent workflow to manage the execution of the sub-workflow and handle its outputs appropriately. It must be built AFTER all participants are created.
        _magSubworkflow = BuildSubWorkflow();

        _classifierAgent = await _agentFactory.CreateAgentAsync(AgentPreset.ClassifierAgent, AgentToolSelection.None, AgentInstructionConstants.SENTINELCORE_OBJECTIVE_GEN, null, token).ConfigureAwait(false);

        //  -----------------── Construct NON-Agent executors ────────────────────────────────────────────────────
        //



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
            this.ReportExecutionFailure($"The workflow run did not dispose within {RunShutdownTimeout.TotalSeconds:0} seconds, so the harness stopped waiting for it.");
        }
        catch (Exception ex)
        {
            this.ReportExecutionFailure("The workflow run failed while being disposed.", ex);
        }
    }








    /// <summary>
    ///     Lazily initializes every agent the workflow needs on first use, avoiding the sync-over-async deadlocks that
    ///     occur when building agents in the constructor.
    /// </summary>
    /// <remarks>
    ///     Initialization is serialized by <see cref="_initializationGate" /> so concurrent executions cannot create
    ///     duplicate agents, and the whole creation sequence is bounded by
    ///     <see cref="WorkflowBase.AgentInitializationTimeout" /> so an
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
            this.ReportExecutionFailure("Failed to initialize the custom group workflow agents.", e);
            throw;
        }
        finally
        {
            _initializationGate.Release();
        }
    }








    //Temporary logging
}