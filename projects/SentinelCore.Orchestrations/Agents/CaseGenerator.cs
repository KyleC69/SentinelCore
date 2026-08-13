// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         CaseGenerator.cs
// Author: Kyle L. Crowder
// Build Num:  081312



using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using ModelContextProtocol.Client;

using SentinelCore.Abstractions;
using SentinelCore.CaseEngine;
using SentinelCore.Tools;




namespace SentinelCore.Agents;





public interface ICaseGenerator
{
    [Obsolete("Use BuildAgentAsync instead to avoid sync-over-async deadlocks.")]
    AIAgent BuildAgent();








    Task<AIAgent> BuildAgentAsync();


    Task<AIAgent> GetAIAgentAsync();


    Task<string> GetAgentResponse(string prompt);
}





/// <summary>
///     Provides bulk case generation by AI for both baseline start and identify pre-existing problems in environment
///     Encapsulates the specialty agent for case generation based on system scans.
/// </summary>
public class CaseGenerator : ICaseGenerator
{

    public string GeneratorInstructions = """
                                          You are an assistant in the SentinelCore investigation platform. You are an expert systems analyst.
                                          You are part of an investigation platform for Windows. You have the ability to examine the environment around you and the surrounding systems.
                                          You are going to be given a prompt with a scope to focus in on and you are going to examine those areas for anomalous readings or irregularities.
                                          You have tools to examine the system and you will use those tools to gather information and generate cases.
                                          You will use the create_case tool to establish an investigation to be investigated by other members.
                                          You will not investigate the case yourself, you will only create the case and provide the information you have gathered to the case.

                                          """;

    private readonly ISentinelAgentFactory _agentFactory;
    private readonly ICaseFlowEngine _engine;
    private readonly ILoggerFactory _factory;
    private readonly IOptions<SentinelCoreSettings> _options;
    private readonly IAgentProfileBuilder _profileBuilder;

    private readonly ISystemReporter _reporter;








    public CaseGenerator(ILoggerFactory factory, ICaseFlowEngine engine, ISystemReporter reporter, ISentinelAgentFactory agentFactory, IAgentProfileBuilder profileBuilder, IOptions<SentinelCoreSettings> settings)
    {
        _reporter = reporter ?? throw new ArgumentNullException(nameof(reporter));
        _agentFactory = agentFactory ?? throw new ArgumentNullException(nameof(agentFactory));
        _profileBuilder = profileBuilder ?? throw new ArgumentNullException(nameof(profileBuilder));
        _options = settings;
        _engine = engine;
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }








    /// <summary>
    ///     Synchronous wrapper for <see cref="BuildAgentAsync" />.
    ///     Kept for backward compatibility with <see cref="ICaseGenerator" />.
    /// </summary>
    /// <returns>An <see cref="AIAgent" /> configured to generate cases.</returns>
    [Obsolete("Use BuildAgentAsync instead to avoid sync-over-async deadlocks.")]
    public AIAgent BuildAgent()
    {
        return BuildAgentAsync().GetAwaiter().GetResult();
    }








    /// <summary>
    ///     Builds an AI agent for generating cases asynchronously.
    /// </summary>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains an <see cref="AIAgent" />
    ///     configured to generate cases.
    /// </returns>
    public async Task<AIAgent> BuildAgentAsync()
    {
        // Build a profile for the CaseGenerator agent.
        //_profileBuilder.BuildAgentSpec("CaseGenerator", AgentRole.Utility, GeneratorInstructions);
        AgentProfile profile = _profileBuilder.BuildAgentSpec("CaseGenerator");
        var mcpTools = await GetMcpToolsAsync().ConfigureAwait(false);

        List<AITool> tools = new();

        tools.Add(new CreateCaseTool(_engine));
        profile.Instructions = GeneratorInstructions;
        profile.Model = _options.Value?.DefaultModel!;
        profile.Tools = [.. tools, .. mcpTools];

        // Build the agent using the factory.
        AIAgent agent = await _agentFactory.BuildFromProfileAsync(profile).ConfigureAwait(false);

        return agent;
    }








    public Task<AIAgent> GetAIAgentAsync()
    {
        return BuildAgentAsync();
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
    ///     Asynchronously retrieves a list of tools from the Model Context Protocol (MCP) client.
    ///     Returns an empty list if the MCP server is not available, rather than hanging or
    ///     throwing during startup.
    /// </summary>
    /// <returns>A list of <see cref="AITool" /> instances retrieved from the MCP client, or an empty list on failure.    </returns>
    private async Task<IList<AITool>> GetMcpToolsAsync()
    {
        try
        {
            await using McpClient mcpClient = await McpClient.CreateAsync(new StdioClientTransport(new()
                    {
                            //Should be running from the output directory
                            Name = "SentinelCore-MCP", Command = "SentinelCore-MCP.exe", WorkingDirectory = AppContext.BaseDirectory, Arguments = ["--stdio"]
                    }, _factory))
                    .ConfigureAwait(false);

            IList<McpClientTool> mcpTools = await mcpClient.ListToolsAsync().ConfigureAwait(false);
            return mcpTools.Cast<AITool>().ToList();
        }
        catch (Exception ex)
        {
            _reporter.ReportWarning("MCP server (SentinelCore-MCP.exe) is not available. Proceeding without MCP tools.", ex);
            return [];
        }
    }
}