// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         CaseGenerator.cs
// Author: Kyle L. Crowder
// Build Num:  082808



using Microsoft.Agents.AI.Compaction;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using ModelContextProtocol.Client;

using SentinelCore.Abstractions;
using SentinelCore.Cfe;
using SentinelCore.Tools;




namespace SentinelCore.Agents;





public interface ICaseGenerator
{

    Task<AIAgent> BuildAgentAsync();
}





/// <summary>
///     Ad-hoc agent for bulk case generation by AI for baseline and db generation.
///     Encapsulates the specialty agent for case generation based on system scans.
/// </summary>
[Obsolete("Only temporary single agent, will be removed")]
public class CaseGenerator : ICaseGenerator, IDisposable
{

    public string GeneratorInstructions = """
                                          You are an assistant in the SentinelCore investigation platform. You are an expert systems analyst.
                                          You are part of an investigation platform for Windows. You have the ability to examine the environment around you and the surrounding systems.
                                          You are going to be given a prompt with a scope to focus in on and you are going to examine those areas for anomalous readings or irregularities.
                                          You have tools to examine the system and you will use those tools to gather information and generate cases.
                                          You will not investigate the case yourself, you will only create the case and provide the information you have gathered to the case.

                                          """;

    private AIAgent? _agent;

    private readonly ISentinelAgentFactory _agentFactory;
    private McpClient _client = null!;
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
    ///     Builds an AI agent for generating cases asynchronously.
    /// </summary>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains an <see cref="AIAgent" />
    ///     configured to generate cases.
    /// </returns>
    public async Task<AIAgent> BuildAgentAsync()
    {


        //Start mcp server tooling
        _client = await McpClient.CreateAsync(new StdioClientTransport(new()
        {
            //Should be running from the output directory
            Name = "SentinelCoreMCP",
            Command = "SentinelCoreMCP.exe",
            WorkingDirectory = AppContext.BaseDirectory,
            Arguments = ["--stdio"]
        }, _factory))
                .ConfigureAwait(false);




        // Build a profile for the CaseGenerator agent.
        AgentProfile profile = _profileBuilder.BuildAgentSpec("CaseGenerator");
        var mcpTools = await GetMcpToolsAsync().ConfigureAwait(false);
        CaseTool caseTool = new(_engine);

        profile.Instructions = GeneratorInstructions;
        profile.Model = _options.Value?.DefaultModel!;
        profile.Tools = [AIFunctionFactory.Create(caseTool.CreateCase), .. mcpTools]; // Combine MCP tools with the CaseTool



#pragma warning disable MAAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        ToolResultCompactionStrategy tools = new(CompactionTriggers.TokensExceed(120000), minimumPreservedGroups: 2);
#pragma warning restore MAAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        // Wrap the compaction strategy in a CompactionProvider (an AIContextProvider) and
        // register it on the profile so the SentinelAgentFactory wires it into
        // ChatClientAgentOptions.AIContextProviders. CompactionProvider derives from
        // AIContextProvider (not MessageAIContextProvider), so it cannot be added via
        // AIAgentBuilder.UseAIContextProviders; it must go through ChatClientAgentOptions.



#pragma warning disable MAAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        CompactionProvider compactionProvider = new(tools, loggerFactory: _factory);
#pragma warning restore MAAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        profile.AIContextProviders.Add(compactionProvider);

        // Build the agent using the factory.
        _agent = await _agentFactory.BuildFromProfileAsync(profile).ConfigureAwait(false);
        return _agent!;
    }








    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
    public void Dispose()
    {
        _factory.Dispose();
        IDisposable? clientDisposable = _client as IDisposable;
        if (!ReferenceEquals(clientDisposable, null))
        {
            clientDisposable.Dispose();
        }
        else
        {
            _ = _client?.DisposeAsync().AsTask();
        }
    }








    public async Task<string> GetAgentResponseold(string prompt)
    {
        AgentResponse response = null!;
        try
        {
            ChatMessage msg = new(ChatRole.User, prompt);




            response = await _agent!.RunAsync(msg);

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


            IList<McpClientTool> mcpTools = await _client.ListToolsAsync().ConfigureAwait(false);
            return mcpTools.Cast<AITool>().ToList();
        }
        catch (Exception ex)
        {
            _reporter.ReportWarning("MCP server (SentinelCore-MCP.exe) is not available. Proceeding without MCP tools.", ex);
            return new List<AITool>();
        }
    }
}