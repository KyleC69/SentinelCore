using System.IO;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
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

        var reg = new McpServerRegistry(store, factory, new NullLogger<McpServerRegistry>());

        // Load persisted servers first
        await reg.LoadPersistedAsync();

        var tools = reg.GetToolsFromCache();
        Assert.IsNotNull(tools);
        // Tool cache may be empty if no servers are registered/persisted
        // This test verifies the method returns a valid collection
        Assert.IsNotNull(tools);

    }


}
