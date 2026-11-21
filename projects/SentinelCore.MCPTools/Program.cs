// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         Program.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

using SentinelCoreMCP.Tools;





internal class Program
{

    private static void ConfigureServerInfo(McpServerOptions options)
    {
        options.ServerInfo = new Implementation
        {
            Name = "io.github.kylec69/SentinelCoreMCP",
            Version = "1.0.12",
            Title = "SentinelCoreMCP",
            Description = "Windows security and configuration reconnaissance server implementing the Model Context Protocol (MCP).",
            WebsiteUrl = "https://github.com/kylec69/SentinelCore-MCP"
        };

        // Generated from the registered tools at first use; single source of truth.
        // Stale name lists in this literal caused callers to attempt commands the tools reject.
        options.ServerInstructions = ServerManifestBuilder.GetManifest().Instructions;
    }








    private static async Task Main(string[] args)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

        // Configure all logs to go to stderr (stdout is used for the MCP protocol messages).
        builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

        _ = builder.Services.AddMcpServer(options => { ConfigureServerInfo(options); }).WithStdioServerTransport().WithToolsFromAssembly();

        await builder.Build().RunAsync();
    }
}
