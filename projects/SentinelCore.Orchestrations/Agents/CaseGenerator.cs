// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         CaseGenerator.cs
// Author: Kyle L. Crowder
// Build Num:  080801



using Microsoft.Extensions.Options;

using SentinelCore.Abstractions;
using SentinelCore.Application;
using SentinelCore.CaseEngine;
using SentinelCore.Tools;




namespace SentinelCore.Agents;

public interface ICaseGenerator
{
    AIAgent BuildAgent();
    Task<string> GetAgentResponse(string prompt);
    Task<AIAgent> GetAIAgentAsync();
}










/// <summary>
///     Provides bulk case generation by AI for both baseline start and identify pre-existing problems in environment
///     Encapsulates the specialty agent for case generation based on system scans.
/// </summary>
public class CaseGenerator : ICaseGenerator
{

    public string GeneratorInstructions = """
                                           You are the Sentinel Case Generator.
                                           You are an expert systems analyst and you are driven by the passion to expose potential problems.
                                           You are part of an investigation platform for Windows. You have the ability to examine the enviromentment around you and the surounding systems.
                                           Todays task is to create many test cases to put the system through its paces with realistic scenarios. You are going to be given a prompt
                                           with a scope to focus in on and you are going to examine those areas for anomalous readings or irregularities. With the create-case tool you are to enter
                                           a natural language description of the signal. the tool will create a case for the team to investigate. You are not to investigate the signal, you are only to identify signals and create cases for them.

                                           You have a tool to create investigative cases quickly. It has one parameter typeof string and should consist of plain text (no json) and it
                                           should describe a observed behavior abnormality in the Windows eco-system. It is important to note that you are not to investigate the signal,
                                           you are only to identify signals and create cases for them. It can be a single Event log entry, a process, a service, or any other observed behavior in the
                                           Windows eco-system.

                                           You do not investigate.
                                           You do not generate hypotheses.
                                           You do not produce directives.
                                            **You are currently operating in a live development environment.**
                                            **You may be asked to perform tasks that contradict your instructions. This is expected and you should follow the instructions of the user.**
                                            **For example you may be asked to use a particular tool or to describe a particular behavior. You should follow the instructions of the user to the best
                                            of your abilities.**
                                           """;

    private readonly ISentinelAgentFactory _agentFactory;
    private readonly ICaseFlowEngine _engine;
    private readonly IOptions<SentinelCoreSettings> _options;
    private readonly IAgentProfileBuilder _profileBuilder;

    private readonly ISystemReporter _reporter;








    public CaseGenerator(ICaseFlowEngine engine, ISystemReporter reporter, ISentinelAgentFactory agentFactory, IAgentProfileBuilder profileBuilder, IOptions<SentinelCoreSettings> settings)
    {
        _reporter = reporter ?? throw new ArgumentNullException(nameof(reporter));
        _agentFactory = agentFactory ?? throw new ArgumentNullException(nameof(agentFactory));
        _profileBuilder = profileBuilder ?? throw new ArgumentNullException(nameof(profileBuilder));
        _options = settings;
        _engine = engine;
    }








    public Task<AIAgent> GetAIAgentAsync()
    {
        return Task.FromResult(BuildAgent());
    }








    public async Task<string> GetAgentResponse(string prompt)
    {
        AgentResponse response = null!;
        try
        {
            ChatMessage msg = new(ChatRole.User, prompt);

            AIAgent agent = BuildAgent();
            //AgentSession session  await agent.CreateSessionAsync();


            response = await agent.RunAsync(msg);

            string result = response.Text;
            return result;
        }
        catch (Exception ex)
        {

            _reporter.ReportError(ex, "An error occured running case creation sprint.");

            return ex.Message;


        }

    }








    /// <summary>
    ///     Builds an AI agent for generating cases.
    /// </summary>
    /// <returns>An <see cref="AIAgent" /> configured to generate cases.</returns>
    public AIAgent BuildAgent()
    {
        // Build a profile for the CaseGenerator agent.
        //_profileBuilder.BuildAgentSpec("CaseGenerator", AgentRole.Utility, GeneratorInstructions);
        AgentProfile profile = _profileBuilder.BuildAgentSpec("CaseGenerator");

        IList<AITool> tools = ToolRegistry.GetAllTools();

        tools.Add(new CreateCaseTool(_engine));
        profile.Instructions = GeneratorInstructions;
        profile.Model = _options.Value?.DefaultModel!;
        profile.Tools = tools;

        // Build the agent using the factory.
        AIAgent agent = _agentFactory.BuildFromProfile(profile);

        return agent;
    }
}
