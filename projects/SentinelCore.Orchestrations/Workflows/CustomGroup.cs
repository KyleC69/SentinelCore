using Microsoft.Extensions.Logging;

using SentinelCore.Abstractions;
using SentinelCore.Agents;
using SentinelCore.Events;




namespace SentinelCore.Workflows;

public class CustomGroupWorkflow : WorkflowBase, IOrchestration
{
    private readonly IAgentProfileBuilder _agentSpecBuilder;
    private readonly ISentinelAgentFactory _agentFactory;


<<<<<<< HEAD
    private AIAgent _theCore;
    // Session used for running the core agent. Initialized in BuildWorkflow.
    private AgentSession? _session;
=======
    private AgentSession _session;

    private AIAgent _theCore;
>>>>>>> 9eed7c9 (Refactor ToolResult handling and expand orchestrations)



    public CustomGroupWorkflow(ICaseGenerator generator, ISystemReporter systemReporter, IAgentProfileBuilder agentSpecBuilder, ISentinelAgentFactory agentFactory) : base(systemReporter)
    {
        _agentSpecBuilder = agentSpecBuilder;
        _agentFactory = agentFactory;



        _theCore = generator.BuildAgent();





    }





    public async Task<AgentResponse> GetAgentResponse(string prompt)
    {




<<<<<<< HEAD
        var response = await _theCore.RunAsync(new ChatMessage(ChatRole.User, prompt), _session!);

        Console.WriteLine($"Agent Response: {response.Text}");
=======
        var response = await _theCore.RunAsync(new ChatMessage(ChatRole.User, prompt), _session);

Console.WriteLine($"Agent Response: {response.Text}");
>>>>>>> 9eed7c9 (Refactor ToolResult handling and expand orchestrations)

        return response;


    }





    public string Description
    {
        get => "Isolated orchestration harnessing for testing agents and workflows outside of complex implementations.";
    }

    public string Name
    {
        get => "Custom Group Workflow";
    }








    public async Task<Workflow> BuildWorkflow()
    {
        AgentProfile coreprofile = _agentSpecBuilder.BuildAgentSpec("TheCore", AgentRole.Core);
<<<<<<< HEAD
        //     coreprofile.Instructions = coreInstructions;
=======
   //     coreprofile.Instructions = coreInstructions;
>>>>>>> 9eed7c9 (Refactor ToolResult handling and expand orchestrations)
        AIAgent theCore = _agentFactory.BuildFromProfile(coreprofile);

        AgentSession session = await theCore.CreateSessionAsync().ConfigureAwait(false);

        _reporter.ReportInfo("Building Custom Group Workflow...");

        var workflow = new WorkflowBuilder(theCore).WithName(Name).WithDescription(Description).Build();

<<<<<<< HEAD
        // Store the session for later use by GetAgentResponse.
        _session = session;
=======
>>>>>>> 9eed7c9 (Refactor ToolResult handling and expand orchestrations)

        _reporter.ReportInfo("Custom Group Workflow built successfully.");
        return await Task.FromResult(workflow);


    }













    public async Task ExecuteAsync(ChatMessage promptSignal, CancellationToken token)
    {

        var reposne = await GetAgentResponse(promptSignal.Text);

        Console.WriteLine(reposne);
    }
}
