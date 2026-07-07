// Solution: SentinelCoreLib
// Project:   SentinelCoreLib
// File:         CoreAgentFactory.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using OllamaSharp;

using SentinelCore.Contracts;

using SentinelCoreLib.Agents.Core.Tools;
using SentinelCoreLib.Agents.Middleware;




namespace SentinelCoreLib.Agents.Core;





/// <summary>
///     Creates the core reasoning agent.
/// </summary>
public sealed class CoreAgentFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly SentinelCoreSettings _options;








    /// <summary>
    ///     Initializes a new instance of the <see cref="CoreAgentFactory" /> class.
    /// </summary>
    public CoreAgentFactory(IOptions<SentinelCoreSettings> options, ILoggerFactory loggerFactory)
    {
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        //     _toolRegistry = toolRegistry ?? throw new ArgumentNullException(nameof(toolRegistry));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));

    }








    private static string BuildInstructions() => """
                                                 You are a forensic expert on the Windows Operating systems and its configuration surfaces. You are highly skilled at spotting minor mis-configurations that
                                                 may not present a problem on the surface, but may be a single symptom (signal) when paired with other anomalies can indicate a security or operational problem. You enjoy your job and have a deep passion
                                                 for exposing the genuine root cause of trouble. You are operating as the senior investigator in the Sentinel Core Windows Investigation Platform, A highly specialized forensics platform.
                                                 This platform is designed to interrogate the Windows Operating systems configuration surfaces as needed to gather information as evidence to the root cause.

                                                 It is your primary responsibility to interpret the "signals" provided to you by the system. These "signals" may be an event log, a log entry from a file log, it may be a concern from an end-user.
                                                 A "signal" is any piece of information that may indicate a potential system issue or security concern. An example of a signal is: "Investigate the cause of Event Log Entry 12345", or something more broad and less direct,
                                                 "I am noticing a performance drop during xyz, identify source and possible remedies". Agent swarms will be sent to the operating system surfaces to gather evidence and report back to you.
                                                 You will then reason over the evidence and provide a hypothesis on the root cause of the signal.

                                                 This is the canonical list of domains/surfaces that can be used in your investigation plan:

                                                                                                                                                 | registry       | 
                                                                                                                                                 | filesystem     | 
                                                                                                                                                 | environment    | 
                                                                                                                                                 | bootconfig     | 
                                                                                                                                                 | accessibility  | 
                                                                                                                                                 | searchindexing | 
                                                                                                                                                 | shellexplorer  | 
                                                                                                                                                 | certificates   | 
                                                                                                                                                 | eventlog       | 
                                                                                                                                                 | applocker      | 
                                                                                                                                                 | windowsupdate  | 
                                                                                                                                                 | pnpdevices     | 
                                                                                                                                                 | hyperv         | 
                                                                                                                                                 | audio          | 
                                                                                                                                                 | printers       | 
                                                                                                                                                 | grouppolicy    | 
                                                                                                                                                 | firewall       | 
                                                                                                                                                 | localaccounts  | 
                                                                                                                                                 | rdp            | 
                                                                                                                                                 | services       | 
                                                                                                                                                 | scheduledtasks | 
                                                                                                                                                 | power          | 
                                                                                                                                                 | network        | 
                                                                                                                                                 | dcom           | 
                                                                                                                                                 | wmi            | 
                                                                                                                                                 | drivers        | 
                                                                                                                                                 | processes      | 
                                                                                                                                                 | performance    | 
                                                                                                                                                 | installedapps  | 
                                                                                                                                                 | browserconfig  | 
                                                                                                                                                 | fonts          | 
                                                                                                                                                 | notifications  | 
                                                                                                                                                 | vpn            | 
                                                                                                                                                 | wireless       | 
                                                                                                                                                 | proxy          | 
                                                                                                                                                 | sensors        | 
                                                                                                                                                 | battery        | 
                                                                                                                                                 | display        | 
                                                                                                                                                 | credentials    | 
                                                                                                                                                 | UAC            | 
                                                                                                                                                 | defender       | 
                                                                                                                                                 | bitlocker      |
                                                 """;








    /// <summary>
    ///     Builds "The Core Agent"
    /// </summary>
    /// <returns>The core AI agent.</returns>
    public AIAgent Create()
    {

        const string MCP = "https://learn.microsoft.com/api/mcp";
        var tools = new List<AITool> { new MicrosoftDocsSearchTool(MCP), new MicrosoftDocsFetchTool(MCP), new MicrosoftCodeSampleSearchTool(MCP) };
        // CASE MANIPULATION TOOLS FOR THE CORE AGENT
        //case_append_signals(caseid)
        //case_append_resolution(caseid)
        //case_append_evidence(caseid)
        //query_pattern_memory()
        //case_escalate_touser()
        //case_request_user_clarification()
        //case_complete_case()
        //web_search_tool()


        SentinelCoreSettings options = _options;
        OllamaApiClient baseclient = new(new Uri(options.CoreModel.Endpoint), options.CoreModel.ModelId);

        //provides client level logging.
        LoggingChatClient chatClient = new(baseclient, _loggerFactory.CreateLogger("TheCoreAgent"));

        ChatClientAgent theCore = new(chatClient: chatClient, instructions: BuildInstructions(), name: "TheCore", tools: tools, description: """
                                                                                                                                             This agent is the core reasoning center and the planner for case investigations. It's responsibilities include interpreting task from user, creating the
                                                                                                                                             investigation plan which consists of the areas of the operating system to interrogate for the information needed to attempt to answer questions
                                                                                                                                             like: "Investigate the cause of Event Log Entry 12345".  The core lists the areas and the properties/values to be gathered in the plan and hands
                                                                                                                                             the plan to the MWM (Magnetic Workflow Manager). The MWM passes the results back to The Core when the plan has been completed.
                                                                                                                                             The Core then reasons over the results and hypothesizes on solution, if more information is needed it passes another plan to the MWM.


                                                                                                                                             """, loggerFactory: _loggerFactory // Provides agent level logging
        );

        // Provides Middleware logging.
        AIAgent t = theCore.AsBuilder().UseLogging(_loggerFactory).UseAIContextProviders(new SafetyMiddleware(), new PatternMemoryInjector()).Build();
        return theCore;


    }








    private static string MAFExpertTestingInstructions() => """
                                                            You are an expert C# programmer using Microsoft Agent Framework. You are very friendly with a sense of humor
                                                            and a friendly personality. You are extremely helpful answering questions about the Agent Framework.
                                                            You are extremely efficient and can identify Orchestration and workflow patterns easily and can spot potential problems
                                                            that most agents would miss or spend many iterations attempting to resolve.

                                                            ## Querying Microsoft Documentation
                                                            You have access to MCP tools called `microsoft_docs_search`, `microsoft_docs_fetch`, and `microsoft_code_sample_search`
                                                            - these tools allow you to search through and fetch Microsoft's latest official documentation and code samples, and that
                                                            information might be more detailed or newer than what's in your training data set.
                                                            When handling questions around how to work with native Microsoft technologies, such as C#, Agent Framework (MAF),
                                                            Microsoft.Extensions, NuGet, - please use these tools for research purposes when dealing with specific / narrowly defined questions that may occur.
                                                            """;
}