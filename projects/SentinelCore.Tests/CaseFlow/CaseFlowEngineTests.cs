// Solution: SentinelCore
// Project:   SentinelCore.Tests
// File:         CaseFlowEngineTests.cs
// Author: Kyle L. Crowder
// Build Num:  081312



using Microsoft.EntityFrameworkCore;

using SentinelCore.Abstractions;
using SentinelCore.CaseEngine;
using SentinelCore.CaseFlowEngine.Persistence;




namespace SentinelCore.Tests.CaseFlow;





/// <summary>
///     Unit tests for <see cref="CaseFlowEngine" />.
/// </summary>
[TestClass]
public sealed class CaseFlowEngineTests
{
    private SentinelCoreDBContext _context = null!;
    private CaseFlowEngine _engine = null!;
    private Mock<IEvidenceStore> _evidenceStoreMock = null!;








    [TestMethod]
    public async Task AdvanceCaseAsync_ThrowsNotImplementedException()
    {
        // Act & Assert
        await Assert.ThrowsExceptionAsync<NotImplementedException>(() => _engine.AdvanceCaseAsync(Guid.NewGuid(), CaseStatus.Investigation));
    }








    [TestMethod]
    public async Task AdvanceCaseAsync_WithCancellation_StillThrowsNotImplementedException()
    {
        // Act & Assert
        await Assert.ThrowsExceptionAsync<NotImplementedException>(() => _engine.AdvanceCaseAsync(Guid.NewGuid(), CaseStatus.Review, CancellationToken.None));
    }








    [TestMethod]
    public void Constructor_NullDbContext_Throws()
    {
        Assert.ThrowsException<ArgumentNullException>(() => new CaseFlowEngine(_evidenceStoreMock.Object, null!));
    }








    [TestMethod]
    public void Constructor_NullEvidenceStore_Throws()
    {
        Assert.ThrowsException<ArgumentNullException>(() => new CaseFlowEngine(null!, _context));
    }








    [TestMethod]
    public async Task CreateCaseAsync_GeneratesNonEmptyGuid()
    {
        // Arrange
        Signal signal = new("test signal", "source-a");

        // Act
        Guid caseId = await _engine.CreateCaseAsync(signal);

        // Assert
        Assert.AreNotEqual(Guid.Empty, caseId);
    }








    [TestMethod]
    public async Task CreateCaseAsync_NullSignal_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => _engine.CreateCaseAsync(null!));
    }








    [TestMethod]
    public async Task GetCaseCountByStatusAsync_ReturnsZeroForEmptyDb()
    {
        // Act
        int count = await _engine.GetCaseCountByStatusAsync(CaseStatus.Open);

        // Assert
        Assert.AreEqual(0, count);
    }








    [TestCleanup]
    public void TestCleanup()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }








    [TestInitialize]
    public void TestInitialize()
    {
        DbContextOptions<SentinelCoreDBContext> options = new DbContextOptionsBuilder<SentinelCoreDBContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        _context = new SentinelCoreDBContext(options);
        _evidenceStoreMock = new Mock<IEvidenceStore>();
        _engine = new CaseFlowEngine(_evidenceStoreMock.Object, _context);
    }
}