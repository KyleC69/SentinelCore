// Solution: SentinelCore
// Project:   SentinelCore.Tests
// File:         PersistenceRegistrationTests.cs
// Author: Kyle L. Crowder
// Build Num:  091300



using System.Reflection;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Moq;

using SentinelCore.CaseFlowEngine.Cfe;
using SentinelCore.CaseFlowEngine.Infrastructure.Persistence;
using SentinelCore.Cfe.Persistence;
using SentinelCore.Contracts.Abstractions;
using SentinelCore.Contracts.CaseFlow;
using SentinelCore.Contracts.Cfe;
using SentinelCore.Contracts.Contracts;
using SentinelCore.Orchestrations.Infrastructure.DependencyInjection;

using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;




namespace SentinelCore.Tests.Architecture;





/// <summary>
///     Dependency-injection alignment tests for pattern-lock rule PL-7 (see
///     architecture/pattern-lock.md). They build the container the way the host
///     does and prove that the approved factory registration activates every
///     persistence service, that the legacy misalignment (persistence consumers
///     without a factory registration) fails fast, and that each locked consumer
///     really creates its context through IDbContextFactory.
/// </summary>
[TestClass]
public sealed class PersistenceRegistrationTests
{
    /// <summary>
    ///     LocalDB connection string used to configure the DbContext factory. No
    ///     database connection is ever opened by these tests.
    /// </summary>
    private const string TestConnectionString = "Server=(localdb)\\mssqllocaldb;Database=SentinelCore.PatternLockTests;Trusted_Connection=True";








    [TestMethod]
    public async Task CaseFlowEngine_Creates_Its_Context_From_The_Factory_Per_OperationAsync()
    {
        // Arrange — a strict factory mock that marks any context creation.
        Mock<IDbContextFactory<SentinelCoreDBContext>> factory = CreateMarkerFactory(out InvalidOperationException marker);
        CaseFlowEngine.Cfe.CaseFlowEngine engine = new(factory.Object);

        // Act & Assert — the engine must reach the context through the factory.
        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() => engine.GetCaseCountByStatusAsync(CaseStatus.Open));

        StringAssert.Contains(actual.Message, marker.Message);
        factory.Verify(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()), Times.Once);
    }








    /// <summary>
    ///     Creates a strict <see cref="IDbContextFactory{TContext}" /> mock whose
    ///     CreateDbContextAsync throws the marker exception, so any consumer code
    ///     path that touches a DbContext is forced through the factory.
    /// </summary>
    /// <param name="marker">The marker exception the mock throws on context creation.</param>
    /// <returns>The strict factory mock.</returns>
    private static Mock<IDbContextFactory<SentinelCoreDBContext>> CreateMarkerFactory(out InvalidOperationException marker)
    {
        marker = new InvalidOperationException("factory-invoked");

        Mock<IDbContextFactory<SentinelCoreDBContext>> factory = new(MockBehavior.Strict);
        factory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>())).ThrowsAsync(marker);

        return factory;
    }








    [TestMethod]
    public async Task EvidenceStore_Creates_Its_Context_From_The_Factory_Per_OperationAsync()
    {
        // Arrange
        Mock<IDbContextFactory<SentinelCoreDBContext>> factory = CreateMarkerFactory(out InvalidOperationException marker);
        EvidenceStore store = new(factory.Object);

        // Act & Assert
        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() => store.GetByCaseIdAsync(Guid.NewGuid().ToString()));

        StringAssert.Contains(actual.Message, marker.Message);
        factory.Verify(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()), Times.Once);
    }








    [TestMethod]
    public void Factory_Consumers_Fail_Activation_When_The_Factory_Registration_Is_Missing()
    {
        // Arrange — the persistence consumers are registered without the DbContext
        // factory: the exact registration/consumption misalignment PL-7 exists to prevent.
        ServiceCollection services = new();
        services.AddSentinelCore(new SentinelCoreSettings { OrchestrationType = OrchestrationType.TheCore });

        using ServiceProvider provider = services.BuildServiceProvider();

        // Act & Assert — the misalignment must fail fast at activation time.
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<ICaseFlowEngine>());

        StringAssert.Contains(exception.Message, "IDbContextFactory");
    }








    [TestMethod]
    public void Locked_Persistence_Consumers_Inject_Only_The_DbContext_Factory()
    {
        // Arrange — PL-7 rule 2: the factory is the only approved DbContext dependency.
        Type factoryType = typeof(IDbContextFactory<SentinelCoreDBContext>);
        Type[] lockedConsumers =
        [
                typeof(CaseFlowEngine.Cfe.CaseFlowEngine),
                typeof(EvidenceStore),
                typeof(PatternMemoryStore),
                typeof(SignalRepository)
        ];

        foreach (Type consumer in lockedConsumers)
        {
            // Act
            ConstructorInfo[] constructors = consumer.GetConstructors();

            // Assert — exactly one public constructor whose single parameter is the factory.
            Assert.IsTrue(constructors.Length == 1, $"{consumer.Name} must expose exactly one public constructor.");

            ParameterInfo[] parameters = constructors[0].GetParameters();
            Assert.IsTrue(parameters.Length == 1, $"{consumer.Name} must inject only the DbContext factory — direct context injection is prohibited.");

            Assert.AreEqual(factoryType, parameters[0].ParameterType, $"{consumer.Name} must inject IDbContextFactory<SentinelCoreDBContext> (PL-7 rule 2).");
        }
    }








    [TestMethod]
    public async Task PatternMemoryStore_Creates_Its_Context_From_The_Factory_Per_OperationAsync()
    {
        // Arrange
        Mock<IDbContextFactory<SentinelCoreDBContext>> factory = CreateMarkerFactory(out InvalidOperationException marker);
        PatternMemoryStore store = new(factory.Object);

        // Act & Assert
        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() => store.GetByCaseIdAsync("123"));

        StringAssert.Contains(actual.Message, marker.Message);
        factory.Verify(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()), Times.Once);
    }








    [TestMethod]
    public void Persistence_Services_Resolve_With_The_Approved_Factory_Registration()
    {
        // Arrange — mirror the composition root: factory registration plus the
        // SentinelCore service registrations.
        ServiceCollection services = new();
        services.AddSentinelCore(new SentinelCoreSettings { OrchestrationType = OrchestrationType.TheCore });
        services.AddDbContextFactory<SentinelCoreDBContext>(options => options.UseSqlServer(TestConnectionString));

        using ServiceProvider provider = services.BuildServiceProvider();

        // Act
        ICaseFlowEngine engine = provider.GetRequiredService<ICaseFlowEngine>();
        IEvidenceStore evidenceStore = provider.GetRequiredService<IEvidenceStore>();
        IPatternMemoryStore patternMemoryStore = provider.GetRequiredService<IPatternMemoryStore>();
        IDbContextFactory<SentinelCoreDBContext> factoryA = provider.GetRequiredService<IDbContextFactory<SentinelCoreDBContext>>();
        IDbContextFactory<SentinelCoreDBContext> factoryB = provider.GetRequiredService<IDbContextFactory<SentinelCoreDBContext>>();

        // Assert — every persistence service activates against the factory pattern.
        Assert.IsNotNull(engine);
        Assert.IsNotNull(evidenceStore);
        Assert.IsNotNull(patternMemoryStore);

        // The factory is a singleton, so transient consumers can capture it safely.
        Assert.AreSame(factoryA, factoryB, "The DbContext factory must be a singleton lifetime.");
    }








    [TestMethod]
    public async Task SignalRepository_Creates_Its_Context_From_The_Factory_Per_OperationAsync()
    {
        // Arrange
        Mock<IDbContextFactory<SentinelCoreDBContext>> factory = CreateMarkerFactory(out InvalidOperationException marker);
        SignalRepository repository = new(factory.Object);

        // Act & Assert
        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() => repository.AddAsync(new Signal("pattern-lock conformance", "unit-test")));

        StringAssert.Contains(actual.Message, marker.Message);
        factory.Verify(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}