// Solution: SentinelCore
// Project:   SentinelCore.Tests
// File:         McpServersViewModelTests.cs
// Author: Kyle L. Crowder
// Build Num:  091112



using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using SentinelCore.Contracts.Mcp;
using SentinelCore.UI.Models;

using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;




namespace SentinelCore.Tests;





/// <summary>
///     Unit tests for <see cref="McpServersViewModel" /> covering constructor null-guards,
///     command gating, server list loading, and add/remove interactions with the registry.
/// </summary>
[TestClass]
public sealed class McpServersViewModelTests
{

    [TestMethod]
    public void AddServerCommand_NotAddingServer_CannotExecute()
    {
        // Arrange
        (Mock<IMcpServerRegistry> registry, Mock<ISentinelAgentCatalog> catalog, Mock<IDispatcherService> dispatcher, Mock<IDialogService> dialog, Mock<IFolderBrowserService> folderBrowser, ILogger<McpServersViewModel> logger) = CreateDependencies();
        McpServersViewModel viewModel = new(registry.Object, catalog.Object, logger, dispatcher.Object, dialog.Object, folderBrowser.Object);

        // Act
        bool canExecute = viewModel.AddServerCommand.CanExecute(null);

        // Assert
        Assert.IsFalse(canExecute);

        viewModel.Dispose();
    }








    [TestMethod]
    public async Task AddServerCommand_ValidInput_RegistersServerAsync()
    {
        // Arrange
        (Mock<IMcpServerRegistry> registry, Mock<ISentinelAgentCatalog> catalog, Mock<IDispatcherService> dispatcher, Mock<IDialogService> dialog, Mock<IFolderBrowserService> folderBrowser, ILogger<McpServersViewModel> logger) = CreateDependencies();
        McpServersViewModel viewModel = new(registry.Object, catalog.Object, logger, dispatcher.Object, dialog.Object, folderBrowser.Object);

        viewModel.ShowAddServerCommand.Execute(null);
        viewModel.NewServerDisplayName = "Test Server";
        viewModel.NewServerCommandOrEndpoint = "test.exe";

        // Act
        viewModel.AddServerCommand.Execute(null);

        // Assert
        registry.Verify(r => r.RegisterAsync(It.Is<McpServerDefinition>(d => d.DisplayName == "Test Server"), It.IsAny<CancellationToken>()), Times.Once);
        Assert.IsFalse(viewModel.IsAddingServer);
        Assert.IsTrue(viewModel.ShowResult);
        StringAssert.Contains(viewModel.ResultMessage, "Test Server");

        viewModel.Dispose();
    }








    [TestMethod]
    public void Constructor_NullCatalog_Throws()
    {
        // Arrange
        (Mock<IMcpServerRegistry> registry, _, Mock<IDispatcherService> dispatcher, Mock<IDialogService> dialog, Mock<IFolderBrowserService> folderBrowser, ILogger<McpServersViewModel> logger) = CreateDependencies();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new McpServersViewModel(registry.Object, null!, logger, dispatcher.Object, dialog.Object, folderBrowser.Object));
    }








    [TestMethod]
    public void Constructor_NullDispatcher_Throws()
    {
        // Arrange
        (Mock<IMcpServerRegistry> registry, Mock<ISentinelAgentCatalog> catalog, _, Mock<IDialogService> dialog, Mock<IFolderBrowserService> folderBrowser, ILogger<McpServersViewModel> logger) = CreateDependencies();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new McpServersViewModel(registry.Object, catalog.Object, logger, null!, dialog.Object, folderBrowser.Object));
    }








    [TestMethod]
    public void Constructor_NullLogger_Throws()
    {
        // Arrange
        (Mock<IMcpServerRegistry> registry, Mock<ISentinelAgentCatalog> catalog, Mock<IDispatcherService> dispatcher, Mock<IDialogService> dialog, Mock<IFolderBrowserService> folderBrowser, _) = CreateDependencies();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new McpServersViewModel(registry.Object, catalog.Object, null!, dispatcher.Object, dialog.Object, folderBrowser.Object));
    }








    [TestMethod]
    public void Constructor_NullRegistry_Throws()
    {
        // Arrange
        (_, Mock<ISentinelAgentCatalog> catalog, Mock<IDispatcherService> dispatcher, Mock<IDialogService> dialog, Mock<IFolderBrowserService> folderBrowser, ILogger<McpServersViewModel> logger) = CreateDependencies();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new McpServersViewModel(null!, catalog.Object, logger, dispatcher.Object, dialog.Object, folderBrowser.Object));
    }








    /// <summary>
    ///     Creates the mocked dependencies used by the view-model tests.
    /// </summary>
    /// <returns>
    ///     A tuple of the registry mock, agent catalog mock, dispatcher mock, and the
    ///     no-op logger passed to the view-model constructor.
    /// </returns>
    private static (Mock<IMcpServerRegistry> Registry, Mock<ISentinelAgentCatalog> Catalog, Mock<IDispatcherService> Dispatcher, Mock<IDialogService> Dialog, Mock<IFolderBrowserService> FolderBrowser, ILogger<McpServersViewModel> Logger) CreateDependencies()
    {
        Mock<IMcpServerRegistry> registry = new();
        Mock<ISentinelAgentCatalog> catalog = new();
        Mock<IDispatcherService> dispatcher = new();
        Mock<IDialogService> dialog = new();
        Mock<IFolderBrowserService> folderBrowser = new();

        dispatcher.Setup(d => d.CheckAccess()).Returns(true);
        registry.Setup(r => r.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        catalog.Setup(c => c.GetAgentNamesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(["CoreChat", "Classifier"]);

        dialog.Setup(d => d.Confirm(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(true);

        return (registry, catalog, dispatcher, dialog, folderBrowser, NullLogger<McpServersViewModel>.Instance);
    }








    [TestMethod]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        // Arrange
        (Mock<IMcpServerRegistry> registry, Mock<ISentinelAgentCatalog> catalog, Mock<IDispatcherService> dispatcher, Mock<IDialogService> dialog, Mock<IFolderBrowserService> folderBrowser, ILogger<McpServersViewModel> logger) = CreateDependencies();
        McpServersViewModel viewModel = new(registry.Object, catalog.Object, logger, dispatcher.Object, dialog.Object, folderBrowser.Object);

        // Act & Assert
        viewModel.Dispose();
        viewModel.Dispose();
    }








    [TestMethod]
    public async Task RefreshCommand_LoadsServers_AndAgentsAsync()
    {
        // Arrange
        (Mock<IMcpServerRegistry> registry, Mock<ISentinelAgentCatalog> catalog, Mock<IDispatcherService> dispatcher, Mock<IDialogService> dialog, Mock<IFolderBrowserService> folderBrowser, ILogger<McpServersViewModel> logger) = CreateDependencies();
        McpServersViewModel viewModel = new(registry.Object, catalog.Object, logger, dispatcher.Object, dialog.Object, folderBrowser.Object);

        McpServerDefinition definition = new("server-1", "Test Server", McpServerTransportType.Stdio, "test.exe");
        McpServerInfo info = new(definition, McpServerStatus.Connected, ["tool-a"]);

        registry.Setup(r => r.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync([info]);

        // Act
        viewModel.RefreshCommand.Execute(null);

        // Assert
        Assert.AreEqual(1, viewModel.Servers.Count);
        Assert.AreEqual("server-1", viewModel.Servers[0].Id);
        Assert.AreEqual(McpServerStatus.Connected, viewModel.Servers[0].Status);
        Assert.AreEqual("tool-a", viewModel.Servers[0].ToolNamesText);
        Assert.AreEqual(2, viewModel.AvailableAgents.Count);
        Assert.AreEqual("CoreChat", viewModel.AvailableAgents[0]);

        viewModel.Dispose();
    }








    [TestMethod]
    public async Task RefreshCommand_OffThread_BusyStateUsesDispatcher()
    {
        // Arrange
        (Mock<IMcpServerRegistry> registry, Mock<ISentinelAgentCatalog> catalog, Mock<IDispatcherService> dispatcher, Mock<IDialogService> dialog, Mock<IFolderBrowserService> folderBrowser, ILogger<McpServersViewModel> logger) = CreateDependencies();
        dispatcher.Setup(d => d.CheckAccess()).Returns(false);
        dispatcher.Setup(d => d.Invoke(It.IsAny<Action>())).Callback<Action>(action => action());
        McpServersViewModel viewModel = new(registry.Object, catalog.Object, logger, dispatcher.Object, dialog.Object, folderBrowser.Object);

        // Act
        await Task.Run(() => viewModel.RefreshCommand.Execute(null));

        // Assert
        dispatcher.Verify(d => d.Invoke(It.IsAny<Action>()), Times.AtLeastOnce);

        viewModel.Dispose();
    }








    [TestMethod]
    public async Task RemoveServerCommand_NoSelection_CannotExecute()
    {
        // Arrange
        (Mock<IMcpServerRegistry> registry, Mock<ISentinelAgentCatalog> catalog, Mock<IDispatcherService> dispatcher, Mock<IDialogService> dialog, Mock<IFolderBrowserService> folderBrowser, ILogger<McpServersViewModel> logger) = CreateDependencies();
        McpServersViewModel viewModel = new(registry.Object, catalog.Object, logger, dispatcher.Object, dialog.Object, folderBrowser.Object);

        // Act
        bool canExecute = viewModel.RemoveServerCommand.CanExecute(null);

        // Assert
        Assert.IsFalse(canExecute);

        viewModel.Dispose();
    }








    [TestMethod]
    public async Task RemoveServerCommand_WithSelection_RemovesServerAsync()
    {
        // Arrange
        (Mock<IMcpServerRegistry> registry, Mock<ISentinelAgentCatalog> catalog, Mock<IDispatcherService> dispatcher, Mock<IDialogService> dialog, Mock<IFolderBrowserService> folderBrowser, ILogger<McpServersViewModel> logger) = CreateDependencies();
        McpServersViewModel viewModel = new(registry.Object, catalog.Object, logger, dispatcher.Object, dialog.Object, folderBrowser.Object);

        McpServerDefinition definition = new("server-1", "Test Server", McpServerTransportType.Stdio, "test.exe");
        McpServerInfo info = new(definition, McpServerStatus.Stopped, []);

        registry.Setup(r => r.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync([info]);

        viewModel.OnNavigatedTo(null);
        McpServerRow? row = viewModel.Servers.FirstOrDefault(s => s.Id == "server-1");
        Assert.IsNotNull(row, "Expected the loaded server row to be present.");
        viewModel.SelectedServer = row;

        // Act
        viewModel.RemoveServerCommand.Execute(null);

        // Assert
        registry.Verify(r => r.RemoveAsync("server-1", It.IsAny<CancellationToken>()), Times.Once);

        viewModel.Dispose();
    }








    [TestMethod]
    public void ShowAddServerCommand_ThenAddServerCommand_CanExecute()
    {
        // Arrange
        (Mock<IMcpServerRegistry> registry, Mock<ISentinelAgentCatalog> catalog, Mock<IDispatcherService> dispatcher, Mock<IDialogService> dialog, Mock<IFolderBrowserService> folderBrowser, ILogger<McpServersViewModel> logger) = CreateDependencies();
        McpServersViewModel viewModel = new(registry.Object, catalog.Object, logger, dispatcher.Object, dialog.Object, folderBrowser.Object);

        // Act
        viewModel.ShowAddServerCommand.Execute(null);
        viewModel.NewServerDisplayName = "Test Server";
        viewModel.NewServerCommandOrEndpoint = "test.exe";

        // Assert
        Assert.IsTrue(viewModel.IsAddingServer);
        Assert.IsTrue(viewModel.AddServerCommand.CanExecute(null));

        viewModel.Dispose();
    }








    [TestMethod]
    public async Task StartServerCommand_WithSelection_StartsServerAsync()
    {
        // Arrange
        (Mock<IMcpServerRegistry> registry, Mock<ISentinelAgentCatalog> catalog, Mock<IDispatcherService> dispatcher, Mock<IDialogService> dialog, Mock<IFolderBrowserService> folderBrowser, ILogger<McpServersViewModel> logger) = CreateDependencies();
        McpServersViewModel viewModel = new(registry.Object, catalog.Object, logger, dispatcher.Object, dialog.Object, folderBrowser.Object);

        McpServerDefinition definition = new("server-1", "Test Server", McpServerTransportType.Stdio, "test.exe");
        McpServerInfo info = new(definition, McpServerStatus.Stopped, []);

        registry.Setup(r => r.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync([info]);

        viewModel.OnNavigatedTo(null);
        McpServerRow? row = viewModel.Servers.FirstOrDefault(s => s.Id == "server-1");
        Assert.IsNotNull(row, "Expected the loaded server row to be present.");
        viewModel.SelectedServer = row;

        // Act
        viewModel.StartServerCommand.Execute(null);

        // Assert
        registry.Verify(r => r.StartAsync("server-1", It.IsAny<CancellationToken>()), Times.Once);

        viewModel.Dispose();
    }








    [TestMethod]
    public async Task StopServerCommand_WithConnectedSelection_StopsServerAsync()
    {
        // Arrange
        (Mock<IMcpServerRegistry> registry, Mock<ISentinelAgentCatalog> catalog, Mock<IDispatcherService> dispatcher, Mock<IDialogService> dialog, Mock<IFolderBrowserService> folderBrowser, ILogger<McpServersViewModel> logger) = CreateDependencies();
        McpServersViewModel viewModel = new(registry.Object, catalog.Object, logger, dispatcher.Object, dialog.Object, folderBrowser.Object);

        McpServerDefinition definition = new("server-1", "Test Server", McpServerTransportType.Stdio, "test.exe");
        McpServerInfo info = new(definition, McpServerStatus.Connected, ["tool-a"]);

        registry.Setup(r => r.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync([info]);

        viewModel.OnNavigatedTo(null);
        McpServerRow? row = viewModel.Servers.FirstOrDefault(s => s.Id == "server-1");
        Assert.IsNotNull(row, "Expected the loaded server row to be present.");
        viewModel.SelectedServer = row;

        // Act
        viewModel.StopServerCommand.Execute(null);

        // Assert
        registry.Verify(r => r.StopAsync("server-1", It.IsAny<CancellationToken>()), Times.Once);

        viewModel.Dispose();
    }
}