// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         CustomGroup.cs
// Author: Kyle L. Crowder
// Build Num:  100812



using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using Microsoft.Agents.AI.Workflows.Specialized.Magentic;

using SentinelCore.Contracts.Abstractions;
using SentinelCore.Contracts.Events;
using SentinelCore.Orchestrations.Abstractions;
using SentinelCore.Orchestrations.Agents;
using SentinelCore.Orchestrations.Services;




namespace SentinelCore.Orchestrations.Workflows;





//AGENTS IGNORE THIS FILE FOR QUICK TESTING OF WORKFLOWS AND AGENTS. THIS IS NOT A REAL ORCHESTRATION, JUST A HARNESS FOR TESTING.
public class CustomGroupWorkflow : WorkflowBase, IOrchestration
{
    private readonly ISentinelAgentFactory _agentFactory;
    private bool _agentInitialized;
    private AIAgent _aggregator;
    private AIAgent _classifier;
    private readonly ISentinelCoreEvents _coreEvents;

    private AIAgent _mgr;

    // Session used for running the core agent. Initialized lazily.
    private AgentSession _session;
    private AIAgent _worker1;
    private AIAgent _worker2;
    private AIAgent _worker3;








    public CustomGroupWorkflow(ISystemReporter systemReporter, ISentinelCoreEvents coreEvents, ISentinelAgentFactory agentFactory, IWorkflowEventProcessor processor) : base(systemReporter, processor)
    {
        _agentFactory = agentFactory;
        _coreEvents = coreEvents;
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


        Workflow workflow = BuildWorkflow();
        CheckpointManager mgr = CheckpointManager.CreateInMemory();

        //   var wf = workflow.AsAIAgent(id: "evidence", name: "Evidence Agent", description: "Handles evidence-related tasks. testing mag group as agent", includeExceptionDetails: true, executionEnvironment: InProcessExecution.Lockstep);

        string sessionid = Guid.NewGuid().ToString();


        StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, inputMessage, sessionid, token);

        await run.TrySendMessageAsync(new TurnToken(true));

        string? lastResponseId = null;
        WorkflowOutputEvent? finalOutput = null;

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
                    ///           Debug.WriteLine($"Executor '{executorInvokedEvent.ExecutorId}' invoked.");
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

        await EnsureAgentInitializedAsync();



    }








    public string Name
    {
        get => "Custom Group Workflow";
    }








    [Experimental("MAAI001")]
    private Workflow BuildWorkflow()
    {
        MagenticPromptOverrides over = new() { FinalAnswerPrompt = Orchestrations.MagenticPromptOverrides.FinalAnswerPrompt };




        Workflow workflow = new MagenticWorkflowBuilder(_mgr).AddParticipants(_worker1, _worker2, _worker3, _aggregator).WithName("Magentic Orchestration Workflow").WithDescription("Coordinates a researcher and coder to solve a complex analytical task.").WithOutputFrom(_aggregator).RequirePlanSignoff(false).WithPromptOverrides(over).WithMaxRounds(10).WithMaxStalls().WithMaxResets(2).Build();

        var classflow = AgentWorkflowBuilder.BuildSequential(_classifier, workflow.AsAIAgent("Investigation-Group", "Investigators", "The magentic investigation group"));

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
            _mgr = await _agentFactory.CreateAgentAsync("MagGroupManager");


            _worker1 = await _agentFactory.CreateAgentAsync("WindowsOSWorker1");
            _worker2 = await _agentFactory.CreateAgentAsync("WindowsOSWorker2");
            _worker3 = await _agentFactory.CreateAgentAsync("WindowsOSWorker3");
            _aggregator = await _agentFactory.CreateAgentAsync("GroupChatAggregator");
            _classifier = await _agentFactory.CreateAgentAsync("Classifier", ClassifierInstruction,ChatResponseFormatJson.ForJsonSchema<InvestigationObjective>(), CancellationToken.None).ConfigureAwait(false);
            // Create a utility agent for additional support within the workflow.
            var util = await _agentFactory.CreateAgentAsync("UtilityAgent",ClassifierInstruction,null,token);
            //Workflow in-memory session for the manager agent to coordinate the workflow.
            _session = await _mgr.CreateSessionAsync().ConfigureAwait(false);


            _agentInitialized = true;
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.Message);
        }
    }


    private const string ClassifierInstruction = """
                                                 You are a classifier agent that is responsible for determining what a signal is about and providing the investigation team
                                                 with the Investigation Objective object. A signal in the Sentinel Core Forensic Investigation Platform (this application) can be an Event log entry, an alert from windows,
                                                 a text file log entry, or other automated input device. Generally a signal is an indicator from any subsystem in a Windows Operating System
                                                 that there may be a problem, or something happend. It is your task is to determine what subsystem the signal is from, and what the signal is trying to tell us.

                                                 You will respond with the following information in a JSON object with the following fields:

                                                 Source: The subsystem or component that generated the signal (e.g., "Windows Event Log", "Security Alert", "Application Log").
                                                 Severity: The severity level of the signal (e.g., "Critical", "Warning", "Information").
                                                 Timestamp: The date and time when the signal was generated (in ISO 8601 format). If Known, otherwise leave blank.
                                                 Summary: A brief summary of the signal, describing what it may be indicating.
                                                 """;





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
