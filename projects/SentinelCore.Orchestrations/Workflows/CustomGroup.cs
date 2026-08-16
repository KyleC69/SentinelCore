// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         CustomGroup.cs
// Author: Kyle L. Crowder
// Build Num:  081602



using SentinelCore.Abstractions;
using SentinelCore.Agents;
using SentinelCore.Application;




namespace SentinelCore.Workflows;





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








    public CustomGroupWorkflow(ICaseGenerator generator, ISystemReporter systemReporter, IAgentProfileBuilder agentSpecBuilder, ISentinelAgentFactory agentFactory) : base(systemReporter)
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








    public async Task<WorkflowExecutionResult?> ExecuteAsync(ChatMessage promptSignal, CancellationToken token)
    {

        AgentResponse response = await GetAgentResponse(promptSignal.Text);

        Console.WriteLine(response.Text);

        return new WorkflowExecutionResult([new ChatMessage(ChatRole.Assistant, response.Text)], eventLog: []);
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

        Console.WriteLine($"Agent Response: {response.Text}");

        Console.WriteLine($"Agent Response: {response.Text}");

        return response;
    }
}