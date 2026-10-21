using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using ModelContextProtocol.Client;

using SentinelCore.Contracts.Mcp;
using SentinelCore.Orchestrations.Mcp;

using Xunit;

using XunitAssert = Xunit.Assert;




namespace SentinelCore.Tests
{


    public class McpServerRegistryTests : IDisposable
    {
        private readonly McpServerRegistry _registry;
        private readonly InMemoryMcpServerRegistryStore _store;
        private readonly TestMcpConnectionFactory _connectionFactory;
        private readonly string _testServerId = "test-server";
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();








        public McpServerRegistryTests()
        {
            _store = new InMemoryMcpServerRegistryStore();
            _connectionFactory = new TestMcpConnectionFactory();
            var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<McpServerRegistry>();

            _registry = new McpServerRegistry(_store, _connectionFactory, logger);

            // Define a test MCP server that connects to a local test server
            // We assume an MCP server is running on localhost:3000 via stdio transport
            var testDefinition = new McpServerDefinition(id: _testServerId, displayName: "Test MCP Server", transportType: McpServerTransportType.Stdio, commandOrEndpoint: "echo", arguments: Array.Empty<string>(), workingDirectory: null, environmentVariables: null, oauthSettings: null, assignedAgentNames: new List<string>());

            _store.AddOrUpdateDefinition(testDefinition);
        }








        [Fact]
        public async Task ToolCache_IsPopulated_AfterStartingServer()
        {
            // Arrange
            await _registry.LoadPersistedAsync(_cts.Token);

            // Act
            await _registry.StartAsync(_testServerId, _cts.Token);

            // Give a moment for the tool listing to complete (though StartAsync awaits it)
            await Task.Delay(100);

            // Assert
            XunitAssert.NotEmpty(_registry.GetToolsFromCache());
            XunitAssert.True(_registry.ToolCount > 0);
        }








        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
            // Attempt to stop the test server to clean up
            try
            {
                _registry.StopAsync(_testServerId, CancellationToken.None).Wait();
            }
            catch
            {
                // Ignore errors during cleanup
            }
        }
    }





    // In-memory implementation of IMcpServerRegistryStore
    public class InMemoryMcpServerRegistryStore : IMcpServerRegistryStore
    {
        private readonly Dictionary<string, McpServerDefinition> _definitions = new();








        public Task<IReadOnlyList<McpServerDefinition>> LoadAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<McpServerDefinition>>(_definitions.Values.ToList());
        }








        public Task SaveAsync(IReadOnlyList<McpServerDefinition> definitions, CancellationToken cancellationToken = default)
        {
            _definitions.Clear();
            foreach (var def in definitions)
            {
                _definitions[def.Id] = def;
            }

            return Task.CompletedTask;
        }








        public void AddOrUpdateDefinition(McpServerDefinition definition)
        {
            _definitions[definition.Id] = definition;
        }
    }





    // Connection factory that creates real MCP clients for stdio transport
    public class TestMcpConnectionFactory : IMcpConnectionFactory
    {
        public async Task<McpClient> ConnectAsync(McpServerDefinition definition, CancellationToken cancellationToken = default)
        {
            if (definition.TransportType != McpServerTransportType.Stdio)
            {
                throw new NotSupportedException($"Transport type {definition.TransportType} is not supported in test.");
            }

            // Create a stdio client pointing to the command
            var transport = new StdioClientTransport(new StdioClientTransportOptions { Name = definition.Id, Command = definition.CommandOrEndpoint, Arguments = definition.Arguments.ToArray() });
            var client = await McpClient.CreateAsync(transport);
            return client;
        }
    }


}
