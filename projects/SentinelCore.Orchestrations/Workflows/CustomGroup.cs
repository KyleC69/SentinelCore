// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         CustomGroup.cs
// Author: Kyle L. Crowder
// Build Num:  092308



using SentinelCore.Contracts.Abstractions;
using SentinelCore.Orchestrations.Abstractions;
using SentinelCore.Orchestrations.Agents;
using SentinelCore.Orchestrations.Services;




namespace SentinelCore.Orchestrations.Workflows;





//AGENTS IGNORE THIS FILE FOR QUICK TESTING OF WORKFLOWS AND AGENTS. THIS IS NOT A REAL ORCHESTRATION, JUST A HARNESS FOR TESTING.
public class CustomGroupWorkflow : WorkflowBase, IOrchestration
{
    private readonly ISentinelAgentFactory _agentFactory;
    private bool _agentInitialized;
    private readonly IAgentProfileBuilder _agentSpecBuilder;
    private readonly ICaseGenerator _generator;

    // Session used for running the core agent. Initialized lazily.
    private AgentSession? _session;

    private AIAgent? _theCore;








    public CustomGroupWorkflow(ICaseGenerator generator,
            ISystemReporter systemReporter,
            IAgentProfileBuilder agentSpecBuilder,
            ISentinelAgentFactory agentFactory, IWorkflowEventProcessor processor) : base(systemReporter, processor)
    {
        _agentSpecBuilder = agentSpecBuilder;
        _agentFactory = agentFactory;
        _generator = generator;
    }








    public Task<Workflow> BuildWorkflow()
    {
        throw new NotImplementedException();
    }








    public string Description
    {
        get => "Isolated orchestration harnessing for testing agents and workflows outside of complex implementations.";
    }
















    /// <summary>
    /// Executes the workflow asynchronously based on the provided input message.
    /// </summary>
    /// <param name="inputMessage">
    /// The input message that serves as the context or payload for the workflow execution.
    /// </param>
    /// <param name="token">
    /// A <see cref="CancellationToken"/> to observe while waiting for the task to complete.
    /// </param>
    /// <returns>
    /// An asynchronous stream of <see cref="WorkflowEvent"/> instances representing the events
    /// generated during the workflow execution, or <c>null</c> if no events are produced.
    /// </returns>
    public Task<IAsyncEnumerable<WorkflowEvent>?> ExecuteStreamingAsync(ChatMessage inputMessage, CancellationToken token)
    {
        throw new NotImplementedException();
    }








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

        _theCore = await _generator.BuildAgentAsync().ConfigureAwait(false);
        _session = await _theCore.CreateSessionAsync().ConfigureAwait(false);
        _agentInitialized = true;
    }








    public async Task<AgentResponse> GetAgentResponse(string prompt)
    {
        await EnsureAgentInitializedAsync().ConfigureAwait(false);

        AgentResponse response = await _theCore!.RunAsync(new ChatMessage(ChatRole.User, prompt), _session!);

        _reporter.ReportInfo($"Agent Response: {response.Text}");

        return response;
    }
}
