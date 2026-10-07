// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         CustomGroup.cs
// Author: Kyle L. Crowder
// Build Num:  100416



using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using SentinelCore.Contracts.Abstractions;
using SentinelCore.Contracts.Events;
using SentinelCore.Orchestrations.Abstractions;
using SentinelCore.Orchestrations.Agents;
using SentinelCore.Orchestrations.Orchestrations;
using SentinelCore.Orchestrations.Services;




namespace SentinelCore.Orchestrations.Workflows;





//AGENTS IGNORE THIS FILE FOR QUICK TESTING OF WORKFLOWS AND AGENTS. THIS IS NOT A REAL ORCHESTRATION, JUST A HARNESS FOR TESTING.
public class CustomGroupWorkflow : WorkflowBase, IOrchestration
{
    private readonly ISentinelAgentFactory _agentFactory;
    private bool _agentInitialized;
    private readonly IAgentProfileBuilder _agentSpecBuilder;

    // Session used for running the core agent. Initialized lazily.
    private AgentSession? _session;

    private AIAgent? _mgr;
    private AIAgent? _worker1;
    private AIAgent? _worker2;
    private AIAgent? _worker3;
    private AIAgent _aggregator;
    private readonly ISentinelCoreEvents _coreEvents;








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


        var workflow = buildWorkflow();


        var wf = workflow.AsAIAgent(id: "evidence", name: "Evidence Agent", description: "Handles evidence-related tasks. testing mag group as agent", includeExceptionDetails: true, executionEnvironment: InProcessExecution.Lockstep);
        try
        {

            AgentSession session = await wf.CreateSessionAsync(token);
            await foreach (AgentResponseUpdate update in wf.RunStreamingAsync(inputMessage, session))
            {
                // Process streaming updates from each agent in the workflow
                if (!string.IsNullOrEmpty(update.Text))
                {
                    Debug.Write(update.Text);
                    await SaveLinesToFileAsync(Path.Combine("F:\\Solutions", "SentinelTrace.md"), new[] { update.Text }, token);
                }
            }


            _coreEvents.RaiseWorkflowOutputEvent(new WorkflowOutputEventArgs(inputMessage));

        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            throw;
        }



        return null;




    }































    /// <summary>
    /// Initialize the agent asynchronously; returns immediately if cancellation is already requested.
    /// </summary>
    /// <remarks>Calls EnsureAgentInitializedAsync to perform the actual initialization work.</remarks>
    /// <param name="cancellationToken">CancellationToken to observe while performing initialization; if already canceled the method returns without
    /// performing work.</param>
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








    public Task<Workflow> BuildWorkflow()
    {
        throw new NotImplementedException();
    }








    /// <summary>
    ///     Lazily initializes the agent on first use, avoiding sync-over-async
    ///     deadlocks that occur when building the agent in the constructor.
    /// </summary>

    private async Task EnsureAgentInitializedAsync()
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
            _aggregator = await _agentFactory.CreateAgentAsync("WindowsOSWorker4");




            //Workflow in-memory session for the manager agent to coordinate the workflow.
            _session = await _mgr.CreateSessionAsync().ConfigureAwait(false);


            _agentInitialized = true;
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.Message);
        }
    }








    /// <summary>
    ///     Appends the specified lines to a file asynchronously.
    /// </summary>
    /// <param name="filePath">The path to the file where lines should be saved.</param>
    /// <param name="lines">The collection of lines to append to the file.</param>
    /// <param name="token">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="Task" /> representing the asynchronous operation.</returns>
    public async Task SaveLinesToFileAsync(string filePath, IEnumerable<string> lines, CancellationToken token)
    {
        try
        {
            await File.AppendAllLinesAsync(filePath, lines, token).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save lines to file {filePath}: {e.Message}");
            throw;
        }
    }

    [Experimental("MAAI001")]
    private Workflow buildWorkflow()
    {
        MagenticPromptOverrides over = new MagenticPromptOverrides()
        {
            TaskLedgerPlanPrompt = MagenticPromptDefaults.TaskLedgerPlanPrompt,
            TaskLedgerPlanUpdatePrompt = MagenticPromptDefaults.TaskLedgerPlanUpdatePrompt,
            ProgressLedgerPrompt = MagenticPromptDefaults.ProgressLedgerPrompt,
            FinalAnswerPrompt = MagenticPromptDefaults.FinalAnswerPrompt,
            TaskLedgerFactsPrompt = MagenticPromptDefaults.TaskLedgerFactsPrompt,
            TaskLedgerFactsUpdatePrompt = MagenticPromptDefaults.TaskLedgerFactsUpdatePrompt,
            TaskLedgerFullPrompt = MagenticPromptDefaults.TaskLedgerFullPrompt
        };

        Workflow workflow = new MagenticWorkflowBuilder(_mgr)
        .AddParticipants(_worker1, _worker2, _worker3, _aggregator)
        .WithName("Magentic Orchestration Workflow")
        .WithDescription("Coordinates a researcher and coder to solve a complex analytical task.")
        .WithOutputFrom(_aggregator)
        .RequirePlanSignoff(false)
        .WithMaxRounds(10)
        .WithMaxStalls(3)
        .WithMaxResets(2)
        .Build();


        return workflow;
    }











    [Experimental("MAAI001")]
    public async Task RunWorkflow()
    {















    }

}
