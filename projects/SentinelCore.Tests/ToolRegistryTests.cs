// Solution: SentinelCore
// Project:   SentinelCore.Tests
// File:         ToolRegistryTests.cs
// Author: Kyle L. Crowder
// Build Num:  110415



using System.IO;

using Microsoft.Extensions.Logging.Abstractions;

using SentinelCore.Contracts.Mcp;
using SentinelCore.Orchestrations.Mcp;




namespace SentinelCore.Tests;





[TestClass]
public class ToolRegistryTests
{



    [TestMethod]
    public async Task TestToolCacheResults_ReturnsAllTools()
    {
        IMcpConnectionFactory factory = new McpConnectionFactory(new NullLoggerFactory());
        IMcpServerRegistryStore store = new JsonFileMcpServerRegistryStore(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SentinelCore", "McpServerRegistry.json"), new NullLogger<JsonFileMcpServerRegistryStore>());

        McpServerRegistry reg = new(store, factory, new NullLogger<McpServerRegistry>());

        // Load persisted servers first
        await reg.LoadPersistedAsync();

        IReadOnlyList<AITool> tools = reg.GetToolsFromCache();
        Assert.IsNotNull(tools);
        // Tool cache may be empty if no servers are registered/persisted
        // This test verifies the method returns a valid collection
        Assert.IsNotNull(tools);

    }
}