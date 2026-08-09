// Solution: SentinelCore
// Project:   SentinelCore.Tests
// File:         EvidenceStoreTests.cs
// Author: Kyle L. Crowder
// Build Num:  080801



using SentinelCore.CaseFlow;
using SentinelCore.CaseFlowEngine.Persistence;
using SentinelCore.Infrastructure.Persistence;




namespace SentinelCore.Tests.CaseFlow;





/// <summary>
///     Integration tests for <see cref="EvidenceStore" /> using EF Core InMemory provider.
/// </summary>
[TestClass]
public sealed class EvidenceStoreTests
{
    private SentinelCoreDBContext _context = null!;
    private EvidenceStore _store = null!;








    [TestMethod]
    public async Task AddAsync_MultipleEvidenceForSameCase_PersistsAll()
    {
        // Arrange — seed a case
        Guid caseGuid = Guid.NewGuid();
        CaseEntity caseEntity = new()
        {
                CaseId = caseGuid,
                Status = CaseStatus.Open,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                InitiatingSignal = 0
        };
        _context.CaseEntities.Add(caseEntity);
        await _context.SaveChangesAsync();

        Evidence evidence1 = new()
        {
                EvidenceId = 1,
                Type = "LogEntry",
                Source = "sensor-01",
                ContentJson = "{}",
                Provenance = "auto",
                Timestamp = DateTime.Now
        };

        Evidence evidence2 = new()
        {
                EvidenceId = 2,
                Type = "Metric",
                Source = "metric-collector",
                ContentJson = """{"cpu":"95%"}""",
                Provenance = "auto-collected",
                Timestamp = DateTime.Now
        };

        // Act
        await _store.AddAsync(caseGuid, evidence1);
        await _store.AddAsync(caseGuid, evidence2);

        // Assert
        Assert.AreEqual(2, _context.EvidenceEntities.Count());
    }








    [TestMethod]
    public async Task AddAsync_NonExistentCase_ThrowsInvalidOperationException()
    {
        // Arrange
        Evidence evidence = new()
        {
                EvidenceId = 1,
                Type = "LogEntry",
                Source = "sensor-01",
                ContentJson = "{}",
                Provenance = "auto",
                Timestamp = DateTime.Now
        };

        // Act & Assert
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => _store.AddAsync(Guid.NewGuid(), evidence));
    }








    [TestMethod]
    public async Task AddAsync_NullEvidence_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => _store.AddAsync(Guid.NewGuid(), null!));
    }








    [TestMethod]
    public async Task AddAsync_StringOverload_ThrowsNotImplementedException()
    {
        // Arrange
        Evidence evidence = new()
        {
                Type = "LogEntry",
                Source = "sensor-01",
                ContentJson = "{}",
                Provenance = "auto",
                Timestamp = DateTime.Now
        };

        // Act & Assert
        await Assert.ThrowsExceptionAsync<NotImplementedException>(() => ((IEvidenceStore)_store).AddAsync("some-id", evidence));
    }








    [TestMethod]
    public async Task AddAsync_ValidCaseAndEvidence_PersistsEvidence()
    {
        // Arrange — seed a case first
        Guid caseGuid = Guid.NewGuid();
        CaseEntity caseEntity = new()
        {
                CaseId = caseGuid,
                Status = CaseStatus.Open,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                InitiatingSignal = 0
        };
        _context.CaseEntities.Add(caseEntity);
        await _context.SaveChangesAsync();

        Evidence evidence = new()
        {
                EvidenceId = 1,
                Type = "LogEntry",
                Source = "sensor-01",
                ContentJson = """{"message":"anomaly detected"}""",
                Provenance = "auto-collected",
                Timestamp = DateTime.Now
        };

        // Act
        await _store.AddAsync(caseGuid, evidence);

        // Assert
        Assert.AreEqual(1, _context.EvidenceEntities.Count());
        EvidenceEntity persisted = _context.EvidenceEntities.First();
        Assert.AreEqual(caseEntity.CaseRecordId, persisted.CaseRecordId);
        Assert.AreEqual("LogEntry", persisted.Type);
        Assert.AreEqual("sensor-01", persisted.Source);
        Assert.AreEqual("""{"message":"anomaly detected"}""", persisted.ContentJson);
        Assert.AreEqual("auto-collected", persisted.Provenance);
    }








    [TestMethod]
    public void Constructor_NullContext_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(() => new EvidenceStore(null!));
    }








    [TestMethod]
    public async Task GetByCaseIdAsync_ThrowsNotImplementedException()
    {
        // Act & Assert
        await Assert.ThrowsExceptionAsync<NotImplementedException>(() => ((IEvidenceStore)_store).GetByCaseIdAsync("some-id"));
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
        _store = new EvidenceStore(_context);
    }
}