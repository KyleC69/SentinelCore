// Solution: SentinelCore
// Project:   SentinelCore.Tests
// File:         McpServerRegistryTests.cs
// Author: Kyle L. Crowder
// Build Num:  091200



using System.Text.Json.Nodes;

using Microsoft.Extensions.Logging.Abstractions;

using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

using Moq;

using SentinelCore.Contracts.Mcp;
using SentinelCore.Orchestrations.Mcp;

using JsonRpcMessage = ModelContextProtocol.Protocol.JsonRpcMessage;
using JsonRpcNotification = ModelContextProtocol.Protocol.JsonRpcNotification;
using JsonRpcRequest = ModelContextProtocol.Protocol.JsonRpcRequest;
using JsonRpcResponse = ModelContextProtocol.Protocol.JsonRpcResponse;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;




// The McpClient constructor is an experimental SDK extensibility API (MCPEXP002).
// The fake client below subclasses McpClient for testing purposes only.
#pragma warning disable MCPEXP002




namespace SentinelCore.Tests;





/// <summary>
///     Unit tests for <see cref="McpServerRegistry" /> covering registration, removal,
///     persistence, status transitions, and per-agent tool filtering.
/// </summary>
[TestClass]
public sealed class McpServerRegistryTests
{

    /// <summary>
    ///     Creates a fake <see cref="McpClient" /> that answers <c>tools/list</c> requests with the
    ///     supplied tool names. The non-virtual <see cref="McpClient.ListToolsAsync(RequestOptions?, CancellationToken)" />
    ///     pipeline deserializes the JSON-RPC result into <see cref="McpClientTool" /> instances, so the
    ///     fake only needs to override the abstract transport members.
    /// </summary>
    /// <param name="toolNames">The tool names to advertise.</param>
    /// <returns>A connected fake client.</returns>
    private static McpClient CreateConnectedClient(IEnumerable<string> toolNames)
    {
        return new FakeMcpClient(toolNames.ToList());
    }








    /// <summary>
    ///     Creates a mock <see cref="IMcpConnectionFactory" /> that returns the supplied client.
    /// </summary>
    /// <param name="client">The client to return from <see cref="IMcpConnectionFactory.ConnectAsync" />.</param>
    /// <returns>The configured factory mock.</returns>
    private static Mock<IMcpConnectionFactory> CreateFactoryMock(McpClient client)
    {
        Mock<IMcpConnectionFactory> factoryMock = new();
        factoryMock.Setup(f => f.ConnectAsync(It.IsAny<McpServerDefinition>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(client));
        return factoryMock;
    }








    /// <summary>
    ///     Creates a registry wired to the supplied store and connection factory.
    /// </summary>
    /// <param name="store">The persistence store.</param>
    /// <param name="factory">The connection factory.</param>
    /// <returns>A new <see cref="McpServerRegistry" /> instance.</returns>
    private static McpServerRegistry CreateRegistry(IMcpServerRegistryStore store, IMcpConnectionFactory factory)
    {
        return new McpServerRegistry(store, factory, NullLogger<McpServerRegistry>.Instance);
    }








    [TestMethod]
    public async Task GetAsync_ExistingServer_ReturnsInfoAsync()
    {
        // Arrange
        InMemoryStore store = new();
        Mock<IMcpConnectionFactory> factoryMock = new();
        McpServerRegistry registry = CreateRegistry(store, factoryMock.Object);

        McpServerDefinition definition = new("server-1", "Test Server", McpServerTransportType.Stdio, "test.exe");
        await registry.RegisterAsync(definition);

        // Act
        McpServerInfo? info = await registry.GetAsync("server-1");

        // Assert
        Assert.IsNotNull(info);
        Assert.AreEqual("server-1", info.Definition.Id);
    }








    [TestMethod]
    public async Task GetAsync_MissingServer_ReturnsNullAsync()
    {
        // Arrange
        InMemoryStore store = new();
        Mock<IMcpConnectionFactory> factoryMock = new();
        McpServerRegistry registry = CreateRegistry(store, factoryMock.Object);

        // Act
        McpServerInfo? info = await registry.GetAsync("missing");

        // Assert
        Assert.IsNull(info);
    }








    [TestMethod]
    public async Task GetToolsForAgentAsync_AssignedServer_FiltersByAgentAsync()
    {
        // Arrange
        InMemoryStore store = new();
        McpClient client = CreateConnectedClient(["tool-b"]);
        Mock<IMcpConnectionFactory> factoryMock = CreateFactoryMock(client);
        McpServerRegistry registry = CreateRegistry(store, factoryMock.Object);

        McpServerDefinition definition = new("server-1", "Assigned Server", McpServerTransportType.Stdio, "test.exe", assignedAgentNames: new[] { "CoreChat" });

        await registry.RegisterAsync(definition);
        await registry.StartAsync("server-1");

        // Act
        IReadOnlyList<AITool> coreChatTools = await registry.GetToolsForAgentAsync("CoreChat");
        IReadOnlyList<AITool> otherTools = await registry.GetToolsForAgentAsync("OtherAgent");

        // Assert
        Assert.AreEqual(1, coreChatTools.Count);
        Assert.AreEqual(0, otherTools.Count);
    }








    [TestMethod]
    public async Task GetToolsForAgentAsync_StoppedServer_ReturnsNoToolsAsync()
    {
        // Arrange
        InMemoryStore store = new();
        McpClient client = CreateConnectedClient(["tool-c"]);
        Mock<IMcpConnectionFactory> factoryMock = CreateFactoryMock(client);
        McpServerRegistry registry = CreateRegistry(store, factoryMock.Object);

        McpServerDefinition definition = new("server-1", "Stopped Server", McpServerTransportType.Stdio, "test.exe");
        await registry.RegisterAsync(definition);

        // Act
        IReadOnlyList<AITool> tools = await registry.GetToolsForAgentAsync("AnyAgent");

        // Assert
        Assert.AreEqual(0, tools.Count);
    }








    [TestMethod]
    public async Task GetToolsForAgentAsync_UniversalServer_ReturnsToolsAsync()
    {
        // Arrange
        InMemoryStore store = new();
        McpClient client = CreateConnectedClient(["tool-a"]);
        Mock<IMcpConnectionFactory> factoryMock = CreateFactoryMock(client);
        McpServerRegistry registry = CreateRegistry(store, factoryMock.Object);

        McpServerDefinition definition = new("server-1", "Universal Server", McpServerTransportType.Stdio, "test.exe");
        await registry.RegisterAsync(definition);
        await registry.StartAsync("server-1");

        // Act
        IReadOnlyList<AITool> tools = await registry.GetToolsForAgentAsync("AnyAgent");

        // Assert
        Assert.AreEqual(1, tools.Count);
        Assert.AreEqual("tool-a", tools[0].Name);
    }








    [TestMethod]
    public async Task LoadPersistedAsync_RestoresDefinitionsAsync()
    {
        // Arrange
        InMemoryStore store = new();
        McpServerDefinition definition = new("server-1", "Test Server", McpServerTransportType.Stdio, "test.exe");
        await store.SaveAsync([definition]);

        Mock<IMcpConnectionFactory> factoryMock = new();
        McpServerRegistry registry = CreateRegistry(store, factoryMock.Object);

        // Act
        await registry.LoadPersistedAsync();

        // Assert
        IReadOnlyList<McpServerInfo> servers = await registry.ListAsync();
        Assert.AreEqual(1, servers.Count);
        Assert.AreEqual(McpServerStatus.Stopped, servers[0].Status);
    }








    [TestMethod]
    public async Task RegisterAsync_AddsServer_AndPersistsAsync()
    {
        // Arrange
        InMemoryStore store = new();
        Mock<IMcpConnectionFactory> factoryMock = new();
        McpServerRegistry registry = CreateRegistry(store, factoryMock.Object);

        McpServerDefinition definition = new("server-1", "Test Server", McpServerTransportType.Stdio, "test.exe");

        // Act
        await registry.RegisterAsync(definition);
        IReadOnlyList<McpServerInfo> servers = await registry.ListAsync();

        // Assert
        Assert.AreEqual(1, servers.Count);
        Assert.AreEqual("server-1", servers[0].Definition.Id);

        IReadOnlyList<McpServerDefinition> persisted = await store.LoadAsync();
        Assert.AreEqual(1, persisted.Count);
        Assert.AreEqual("Test Server", persisted[0].DisplayName);
    }








    [TestMethod]
    public async Task RemoveAsync_DeletesServer_AndPersistsAsync()
    {
        // Arrange
        InMemoryStore store = new();
        Mock<IMcpConnectionFactory> factoryMock = new();
        McpServerRegistry registry = CreateRegistry(store, factoryMock.Object);

        McpServerDefinition definition = new("server-1", "Test Server", McpServerTransportType.Stdio, "test.exe");
        await registry.RegisterAsync(definition);

        // Act
        await registry.RemoveAsync("server-1");

        // Assert
        IReadOnlyList<McpServerInfo> servers = await registry.ListAsync();
        Assert.AreEqual(0, servers.Count);
    }








    [TestMethod]
    public async Task StopAsync_DisconnectsClient_AndUpdatesStatusAsync()
    {
        // Arrange
        InMemoryStore store = new();
        McpClient client = CreateConnectedClient(["tool-d"]);
        Mock<IMcpConnectionFactory> factoryMock = CreateFactoryMock(client);
        McpServerRegistry registry = CreateRegistry(store, factoryMock.Object);

        McpServerDefinition definition = new("server-1", "Server", McpServerTransportType.Stdio, "test.exe");
        await registry.RegisterAsync(definition);
        await registry.StartAsync("server-1");

        // Act
        await registry.StopAsync("server-1");

        // Assert
        McpServerInfo? info = await registry.GetAsync("server-1");
        Assert.IsNotNull(info);
        Assert.AreEqual(McpServerStatus.Stopped, info.Status);
    }








    [TestMethod]
    public async Task UpdateAssignmentsAsync_MissingServer_ThrowsAsync()
    {
        // Arrange
        InMemoryStore store = new();
        Mock<IMcpConnectionFactory> factoryMock = new();
        McpServerRegistry registry = CreateRegistry(store, factoryMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => registry.UpdateAssignmentsAsync("missing", ["CoreChat"]));
    }








    [TestMethod]
    public async Task UpdateAssignmentsAsync_ReplacesAssignments_AndPersistsAsync()
    {
        // Arrange
        InMemoryStore store = new();
        Mock<IMcpConnectionFactory> factoryMock = new();
        McpServerRegistry registry = CreateRegistry(store, factoryMock.Object);

        McpServerDefinition definition = new("server-1", "Server", McpServerTransportType.Stdio, "test.exe");
        await registry.RegisterAsync(definition);

        // Act
        await registry.UpdateAssignmentsAsync("server-1", ["CoreChat", "Classifier"]);

        // Assert
        McpServerInfo? info = await registry.GetAsync("server-1");
        Assert.IsNotNull(info);
        CollectionAssert.AreEquivalent(new[] { "CoreChat", "Classifier" }, (System.Collections.ICollection)info.Definition.AssignedAgentNames);

        IReadOnlyList<McpServerDefinition> persisted = await store.LoadAsync();
        Assert.AreEqual(1, persisted.Count);
        Assert.AreEqual(2, persisted[0].AssignedAgentNames.Count);
    }








    /// <summary>
    ///     In-memory store that records saved definitions so persistence can be asserted
    ///     without touching the file system.
    /// </summary>
    private sealed class InMemoryStore : IMcpServerRegistryStore
    {
        private readonly List<McpServerDefinition> _definitions = new();








        /// <summary>
        ///     Loads the definitions recorded by the most recent save.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The recorded definitions.</returns>
        public Task<IReadOnlyList<McpServerDefinition>> LoadAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<McpServerDefinition>>(_definitions.ToList());
        }








        /// <summary>
        ///     Records the supplied definitions as the current persisted state.
        /// </summary>
        /// <param name="definitions">Definitions to record.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A completed task.</returns>
        public Task SaveAsync(IReadOnlyList<McpServerDefinition> definitions, CancellationToken cancellationToken = default)
        {
            _definitions.Clear();
            _definitions.AddRange(definitions);
            return Task.CompletedTask;
        }
    }





    /// <summary>
    ///     Fake <see cref="McpClient" /> transport that responds to <c>tools/list</c> requests with a
    ///     canned <see cref="ListToolsResult" /> payload. All other abstract members return inert values.
    /// </summary>
    private sealed class FakeMcpClient : McpClient
    {
        private readonly IReadOnlyList<string> _toolNames;








        /// <summary>
        ///     Initializes a new instance of the <see cref="FakeMcpClient" /> class.
        /// </summary>
        /// <param name="toolNames">The tool names to advertise via <c>tools/list</c>.</param>
        public FakeMcpClient(IReadOnlyList<string> toolNames)
        {
            _toolNames = toolNames;
        }








        /// <inheritdoc />
        public override Task<ClientCompletionDetails> Completion
        {
            get => Task.FromResult(new ClientCompletionDetails());
        }

        /// <inheritdoc />
        public override string? NegotiatedProtocolVersion
        {
            get => null;
        }

        /// <inheritdoc />
        public override ServerCapabilities ServerCapabilities
        {
            get => new();
        }

        /// <inheritdoc />
        public override Implementation ServerInfo
        {
            get => new() { Name = "fake-server", Version = "1.0" };
        }

        /// <inheritdoc />
        public override string? ServerInstructions
        {
            get => null;
        }

        /// <inheritdoc />
        public override string? SessionId
        {
            get => null;
        }








        /// <inheritdoc />
        public override ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }








        /// <inheritdoc />
        public override IAsyncDisposable RegisterNotificationHandler(string method, Func<JsonRpcNotification, CancellationToken, ValueTask> handler)
        {
            return new NullAsyncDisposable();
        }








        /// <inheritdoc />
        public override ValueTask<IDictionary<string, InputResponse>> ResolveInputRequestsAsync(IDictionary<string, InputRequest> inputRequests, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult<IDictionary<string, InputResponse>>(new Dictionary<string, InputResponse>());
        }








        /// <inheritdoc />
        public override Task SendMessageAsync(JsonRpcMessage message, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }








        /// <inheritdoc />
        public override Task<JsonRpcResponse> SendRequestAsync(JsonRpcRequest request, CancellationToken cancellationToken = default)
        {
            JsonRpcResponse response = new() { Id = request.Id, Result = BuildToolsListResult() };

            return Task.FromResult(response);
        }








        /// <summary>
        ///     Builds the JSON payload for a <c>tools/list</c> response describing the advertised tools.
        /// </summary>
        /// <returns>A <see cref="JsonNode" /> matching the <see cref="ListToolsResult" /> schema.</returns>
        private JsonNode BuildToolsListResult()
        {
            JsonArray tools = new();

            foreach (string toolName in _toolNames)
            {
                tools.Add(new JsonObject { ["name"] = toolName, ["inputSchema"] = new JsonObject { ["type"] = "object" } });
            }

            return new JsonObject { ["tools"] = tools };
        }
    }





    /// <summary>
    ///     Disposable returned by <see cref="FakeMcpClient.RegisterNotificationHandler" /> that does nothing.
    /// </summary>
    private sealed class NullAsyncDisposable : IAsyncDisposable
    {
        /// <inheritdoc />
        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }
}