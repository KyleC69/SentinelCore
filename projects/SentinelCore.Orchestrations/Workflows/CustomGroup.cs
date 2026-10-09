// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         CustomGroup.cs
// Author: Kyle L. Crowder
// Build Num:  100908



using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using Microsoft.Agents.AI.Workflows.Specialized.Magentic;

using SentinelCore.Contracts.Abstractions;
using SentinelCore.Orchestrations.Abstractions;
using SentinelCore.Orchestrations.Agents.AgentPresets;
using SentinelCore.Orchestrations.Services;




namespace SentinelCore.Orchestrations.Workflows;





//AGENTS IGNORE THIS FILE FOR QUICK TESTING OF WORKFLOWS AND AGENTS. THIS IS NOT A REAL ORCHESTRATION, JUST A HARNESS FOR TESTING.
public class CustomGroupWorkflow : WorkflowBase, IOrchestration
{
    private readonly ISentinelAgentFactory _agentFactory;
    private bool _agentInitialized;
    private AIAgent _aggregator;
    private AIAgent _classifier;

    private AIAgent _mgr;

    // Session used for running the core agent. Initialized lazily.
    private AIAgent _worker1;
    private AIAgent _worker2;
    private AIAgent _worker3;








    public CustomGroupWorkflow(ISystemReporter systemReporter, ISentinelAgentFactory agentFactory, IWorkflowEventProcessor processor) : base(systemReporter, processor)
    {
        _agentFactory = agentFactory;

        _agentInitialized = false;
        _aggregator = null!;
        _classifier = null!;
        _mgr = null!;
        _worker1 = null!;
        _worker2 = null!;
        _worker3 = null!;
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
        string? lastResponseId = null;
        WorkflowOutputEvent? finalOutput = null;


        StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, inputMessage, sessionid, token);
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

        await EnsureAgentInitializedAsync(cancellationToken);



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

        Workflow classflow = AgentWorkflowBuilder.BuildSequential(_classifier, workflow.AsAIAgent("Investigation-Group", "Investigators", "The magentic investigation group"));

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
            var test = _agentFactory.CreateAgentAsync(AgentPreset.CoreAgent, token);
            _mgr = await _agentFactory.CreateAgentAsync(AgentPreset.ManagerAgent, token);

            _worker1 = await _agentFactory.CreateAgentAsync(AgentPreset.WorkerAgent, token);
            _worker2 = await _agentFactory.CreateAgentAsync(AgentPreset.WorkerAgent, token);
            _worker3 = await _agentFactory.CreateAgentAsync(AgentPreset.WorkerAgent, token);
            _aggregator = await _agentFactory.CreateAgentAsync(AgentPreset.AggregatorAgent, token);
            // Classifier agent now uses the preset‑only overload; any custom instructions are supplied via the preset definition.
            _classifier = await _agentFactory.CreateAgentAsync(AgentPreset.ClassifierAgent, token).ConfigureAwait(false);
            // Utility agent for additional support.
            AIAgent util = await _agentFactory.CreateAgentAsync(AgentPreset.UtilityAgent, token);
            //Workflow in-memory session for the manager agent to coordinate the workflow.
            await _mgr.CreateSessionAsync().ConfigureAwait(false);


            _agentInitialized = true;
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.Message);
        }
    }








    // The classifier preset now contains any required instructions; this constant is no longer used.








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