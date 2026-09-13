// Solution: SentinelCore
// Project:   SentinelCore.Tests
// File:         ModelConfigViewModelTests.cs
// Author: Kyle L. Crowder
// Build Num:  091300



using System.IO;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

using SentinelCore.Contracts.Contracts;
using SentinelCore.Contracts.Mcp;
using SentinelCore.UI.Models;

using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;




namespace SentinelCore.Tests;





/// <summary>
///     Unit tests for <see cref="ModelConfigViewModel" /> covering constructor
///     null-guards, card loading from the agent catalog, save persistence, and
///     live-settings application.
/// </summary>
[TestClass]
public sealed class ModelConfigViewModelTests
{

    [TestMethod]
    public void Constructor_NullCatalog_Throws()
    {
        // Arrange
        (_, Mock<IModelConfigStore> store, _, IOptions<SentinelCoreSettings> options) = CreateDependencies();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ModelConfigViewModel(null!, store.Object, options, NullLogger<ModelConfigViewModel>.Instance));
    }








    [TestMethod]
    public void Constructor_NullLogger_Throws()
    {
        // Arrange
        (Mock<ISentinelAgentCatalog> catalog, Mock<IModelConfigStore> store, _, IOptions<SentinelCoreSettings> options) = CreateDependencies();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ModelConfigViewModel(catalog.Object, store.Object, options, null!));
    }








    [TestMethod]
    public void Constructor_NullOptions_Throws()
    {
        // Arrange
        (Mock<ISentinelAgentCatalog> catalog, Mock<IModelConfigStore> store, _, _) = CreateDependencies();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ModelConfigViewModel(catalog.Object, store.Object, null!, NullLogger<ModelConfigViewModel>.Instance));
    }








    [TestMethod]
    public void Constructor_NullStore_Throws()
    {
        // Arrange
        (Mock<ISentinelAgentCatalog> catalog, _, _, IOptions<SentinelCoreSettings> options) = CreateDependencies();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ModelConfigViewModel(catalog.Object, null!, options, NullLogger<ModelConfigViewModel>.Instance));
    }








    /// <summary>
    ///     Creates the mocked dependencies used by the view-model tests.
    /// </summary>
    /// <returns>
    ///     A tuple of the catalog mock, store mock, settings, and logger.
    /// </returns>
    private static (Mock<ISentinelAgentCatalog> Catalog, Mock<IModelConfigStore> Store, SentinelCoreSettings Settings, IOptions<SentinelCoreSettings> Options) CreateDependencies()
    {
        Mock<ISentinelAgentCatalog> catalog = new();
        Mock<IModelConfigStore> store = new();

        catalog.Setup(c => c.GetAgentNamesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(["TheCore", "Manager", "Worker1"]);

        SentinelCoreSettings settings = new();
        OptionsWrapper<SentinelCoreSettings> options = new(settings);

        return (catalog, store, settings, options);
    }








    [TestMethod]
    public async Task OnNavigatedTo_LoadsCardPerCatalogAgentAsync()
    {
        // Arrange
        (Mock<ISentinelAgentCatalog> catalog, Mock<IModelConfigStore> store, _, IOptions<SentinelCoreSettings> options) = CreateDependencies();
        ModelConfigViewModel viewModel = new(catalog.Object, store.Object, options, NullLogger<ModelConfigViewModel>.Instance);

        // Act
        viewModel.OnNavigatedTo(null);

        // Give the fire-and-forget load a moment to complete.
        await Task.Delay(50);

        // Assert — one card per catalog agent, in catalog order.
        Assert.AreEqual(3, viewModel.Cards.Count);
        Assert.AreEqual("TheCore", viewModel.Cards[0].AgentName);
        Assert.AreEqual("Manager", viewModel.Cards[1].AgentName);
        Assert.AreEqual("Worker1", viewModel.Cards[2].AgentName);
    }








    [TestMethod]
    public async Task OnNavigatedTo_PrePopulatesConfiguredAgentsAsync()
    {
        // Arrange
        (Mock<ISentinelAgentCatalog> catalog, Mock<IModelConfigStore> store, SentinelCoreSettings settings, IOptions<SentinelCoreSettings> options) = CreateDependencies();
        settings.AgentModels["TheCore"] = new ModelProfile("http://localhost:11434", "test-model", 0.2f);

        ModelConfigViewModel viewModel = new(catalog.Object, store.Object, options, NullLogger<ModelConfigViewModel>.Instance);

        // Act
        viewModel.OnNavigatedTo(null);
        await Task.Delay(50);

        // Assert — the configured agent's card carries the saved values.
        AgentModelCard coreCard = viewModel.Cards.Single(c => c.AgentName == "TheCore");
        Assert.IsTrue(coreCard.IsConfigured);
        Assert.AreEqual("http://localhost:11434", coreCard.Endpoint);
        Assert.AreEqual("test-model", coreCard.ModelId);

        // Unconfigured agents start blank.
        AgentModelCard managerCard = viewModel.Cards.Single(c => c.AgentName == "Manager");
        Assert.IsFalse(managerCard.IsConfigured);
    }








    [TestMethod]
    public async Task SaveCommand_CompleteCards_PersistsAndAppliesAsync()
    {
        // Arrange
        (Mock<ISentinelAgentCatalog> catalog, Mock<IModelConfigStore> store, SentinelCoreSettings settings, IOptions<SentinelCoreSettings> options) = CreateDependencies();
        ModelConfigViewModel viewModel = new(catalog.Object, store.Object, options, NullLogger<ModelConfigViewModel>.Instance);

        viewModel.OnNavigatedTo(null);
        await Task.Delay(50);

        foreach (AgentModelCard card in viewModel.Cards)
        {
            card.Endpoint = "http://localhost:11434";
            card.ModelId = "test-model";
        }

        // Act
        viewModel.SaveCommand.Execute(null);
        await Task.Delay(50);

        // Assert — the document was persisted with every agent.
        store.Verify(s => s.Save(It.Is<ModelConfigDocument>(d => d.AgentModels.Count == 3 && d.AgentModels.ContainsKey("TheCore") && d.AgentModels.ContainsKey("Manager") && d.AgentModels.ContainsKey("Worker1"))), Times.Once);

        // The live settings were updated so the next agent build uses them.
        Assert.AreEqual(3, settings.AgentModels.Count);
        Assert.AreEqual("test-model", settings.AgentModels["TheCore"].ModelId);

        Assert.IsTrue(viewModel.ShowResult);
        StringAssert.Contains(viewModel.ResultMessage, "All agent models saved.");
    }








    [TestMethod]
    public async Task SaveCommand_IncompleteCard_StaysUnconfiguredAsync()
    {
        // Arrange
        (Mock<ISentinelAgentCatalog> catalog, Mock<IModelConfigStore> store, SentinelCoreSettings settings, IOptions<SentinelCoreSettings> options) = CreateDependencies();
        ModelConfigViewModel viewModel = new(catalog.Object, store.Object, options, NullLogger<ModelConfigViewModel>.Instance);

        viewModel.OnNavigatedTo(null);
        await Task.Delay(50);

        // Configure only TheCore; leave Manager and Worker1 blank.
        AgentModelCard coreCard = viewModel.Cards.Single(c => c.AgentName == "TheCore");
        coreCard.Endpoint = "http://localhost:11434";
        coreCard.ModelId = "test-model";

        // Act
        viewModel.SaveCommand.Execute(null);
        await Task.Delay(50);

        // Assert — only the complete card was persisted and applied.
        store.Verify(s => s.Save(It.Is<ModelConfigDocument>(d => d.AgentModels.Count == 1)), Times.Once);
        Assert.AreEqual(1, settings.AgentModels.Count);

        Assert.IsTrue(viewModel.ShowResult);
        StringAssert.Contains(viewModel.ResultMessage, "Manager");
        StringAssert.Contains(viewModel.ResultMessage, "Worker1");
    }








    [TestMethod]
    public async Task SaveCommand_StoreThrows_ReportsErrorAsync()
    {
        // Arrange
        (Mock<ISentinelAgentCatalog> catalog, Mock<IModelConfigStore> store, _, IOptions<SentinelCoreSettings> options) = CreateDependencies();
        store.Setup(s => s.Save(It.IsAny<ModelConfigDocument>())).Throws(new IOException("disk full"));

        ModelConfigViewModel viewModel = new(catalog.Object, store.Object, options, NullLogger<ModelConfigViewModel>.Instance);

        viewModel.OnNavigatedTo(null);
        await Task.Delay(50);

        AgentModelCard coreCard = viewModel.Cards.Single(c => c.AgentName == "TheCore");
        coreCard.Endpoint = "http://localhost:11434";
        coreCard.ModelId = "test-model";

        // Act
        viewModel.SaveCommand.Execute(null);
        await Task.Delay(50);

        // Assert
        Assert.IsTrue(viewModel.ShowResult);
        StringAssert.Contains(viewModel.ResultMessage, "Error saving configuration");
    }
}