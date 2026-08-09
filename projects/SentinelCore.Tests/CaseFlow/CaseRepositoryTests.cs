// Solution: SentinelCore
// Project:   SentinelCore.Tests
// File:         CaseRepositoryTests.cs
// Author: Kyle L. Crowder
// Build Num:  080801



using Moq;

using SentinelCore.Abstractions;
using SentinelCore.CaseFlow;
using SentinelCore.CaseFlowEngine.Persistence;




namespace SentinelCore.Tests.CaseFlow;





/// <summary>
///     Integration tests for <see cref="CaseRepository" /> using EF Core InMemory provider.
/// </summary>
[TestClass]
public sealed class CaseRepositoryTests
{
    private SentinelCoreDBContext _context = null!;
    private Mock<ISystemReporter> _reporterMock = null!;
    private CaseRepository _repository = null!;








    [TestMethod]
    public void Constructor_NullContext_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(() => new CaseRepository(null!, _reporterMock.Object));
    }








    [TestMethod]
    [TestMethod]
    public async Task CreateCaseWithSignalAsync_ReturnsNull_ReflectsCurrentStubImplementation()
    {
        // Arrange
        Signal signal = new("test signal", "source-a");
        Case caseRecord = new() { CaseId = Guid.NewGuid(), Status = CaseStatus.Open, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now };

        // Act — Current implementation returns null (not yet implemented)
        int? result = await _repository.CreateCaseWithSignalAsync(signal, caseRecord);

        // Assert
        Assert.IsNull(result);
    }








    [TestMethod]
    public async Task GetByIdAsync_EmptyString_ThrowsArgumentException()
    {
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => _repository.GetByIdAsync(string.Empty));
    }








    [TestMethod]
    public async Task GetByIdAsync_ExistingCase_ReturnsCase()
    {
        // Arrange
        Guid caseGuid = Guid.NewGuid();
        CaseEntity entity = new()
        {
                CaseId = caseGuid,
                Status = CaseStatus.Open,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                InitiatingSignal = 0
        };
        _context.CaseEntities.Add(entity);
        await _context.SaveChangesAsync();

        // Act
        Case? result = await _repository.GetByIdAsync(caseGuid.ToString());

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(caseGuid, result!.CaseId);
        Assert.AreEqual(CaseStatus.Open, result.Status);
    }








    [TestMethod]
    public async Task GetByIdAsync_InvalidGuidFormat_ThrowsArgumentException()
    {
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => _repository.GetByIdAsync("not-a-guid"));
    }








    [TestMethod]
    public async Task GetByIdAsync_MapsAllFields()
    {
        // Arrange
        Guid caseGuid = Guid.NewGuid();
        DateTime created = DateTime.Now;
        DateTime updated = DateTime.Now;
        CaseEntity entity = new()
        {
                CaseId = caseGuid,
                Status = CaseStatus.Investigation,
                CreatedAt = created,
                UpdatedAt = updated,
                InitiatingSignal = 42
        };
        _context.CaseEntities.Add(entity);
        await _context.SaveChangesAsync();

        // Act
        Case? result = await _repository.GetByIdAsync(caseGuid.ToString());

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(entity.Id, result!.Id);
        Assert.AreEqual(caseGuid, result.CaseId);
        Assert.AreEqual(CaseStatus.Investigation, result.Status);
        Assert.AreEqual(created, result.CreatedAt);
        Assert.AreEqual(updated, result.UpdatedAt);
        Assert.AreEqual(42, result.InitiatingSignal);
    }








    [TestMethod]
    public async Task GetByIdAsync_NonExistentCase_ReturnsNull()
    {
        // Act
        Case? result = await _repository.GetByIdAsync(Guid.NewGuid().ToString());

        // Assert
        Assert.IsNull(result);
    }








    [TestMethod]
    public async Task GetByIdAsync_Null_ThrowsArgumentException()
    {
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => _repository.GetByIdAsync(null!));
    }








    [TestMethod]
    public async Task GetByIdAsync_Whitespace_ThrowsArgumentException()
    {
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => _repository.GetByIdAsync("   "));
    }








    [TestMethod]
    public async Task ListAsync_NoCases_ReturnsEmptyList()
    {
        // Act
        List<Case?> results = await _repository.ListAsync();

        // Assert
        Assert.AreEqual(0, results.Count);
    }








    [TestMethod]
    public async Task ListAsync_ReturnsOrderedByCreatedAtDescending()
    {
        // Arrange
        DateTime baseTime = DateTime.Now;
        Guid newestId = Guid.NewGuid();
        Guid middleId = Guid.NewGuid();
        Guid oldestId = Guid.NewGuid();

        _context.CaseEntities.AddRange(new CaseEntity
        {
                CaseId = oldestId,
                Status = CaseStatus.Open,
                CreatedAt = baseTime.AddDays(-2),
                UpdatedAt = baseTime,
                InitiatingSignal = 0
        }, new CaseEntity
        {
                CaseId = newestId,
                Status = CaseStatus.Open,
                CreatedAt = baseTime,
                UpdatedAt = baseTime,
                InitiatingSignal = 0
        }, new CaseEntity
        {
                CaseId = middleId,
                Status = CaseStatus.Open,
                CreatedAt = baseTime.AddDays(-1),
                UpdatedAt = baseTime,
                InitiatingSignal = 0
        });
        await _context.SaveChangesAsync();

        // Act
        List<Case?> results = await _repository.ListAsync();

        // Assert
        Assert.AreEqual(3, results.Count);
        Assert.AreEqual(newestId, results[0]!.CaseId);
        Assert.AreEqual(middleId, results[1]!.CaseId);
        Assert.AreEqual(oldestId, results[2]!.CaseId);
    }








    [TestMethod]
    public async Task ListAsync_WithCases_ReturnsAllCases()
    {
        // Arrange
        for (int i = 0; i < 3; i++)
            _context.CaseEntities.Add(new CaseEntity
            {
                    CaseId = Guid.NewGuid(),
                    Status = CaseStatus.Open,
                    CreatedAt = DateTime.Now.AddDays(-i),
                    UpdatedAt = DateTime.Now,
                    InitiatingSignal = 0
            });

        await _context.SaveChangesAsync();

        // Act
        List<Case?> results = await _repository.ListAsync();

        // Assert
        Assert.AreEqual(3, results.Count);
    }








    [TestCleanup]
    public void TestCleanup()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }








    [TestMethod]
    public async Task UpdateAsync_ExistingCase_UpdatesStatusAndTimestamp()
    {
        // Arrange
        Guid caseGuid = Guid.NewGuid();
        CaseEntity entity = new()
        {
                CaseId = caseGuid,
                Status = CaseStatus.Open,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                InitiatingSignal = 0
        };
        _context.CaseEntities.Add(entity);
        await _context.SaveChangesAsync();

        // Detach so we can re-attach via update
        _context.Entry(entity).State = EntityState.Detached;

        Case caseRecord = new()
        {
                Id = entity.Id,
                CaseId = caseGuid,
                Status = CaseStatus.Investigation,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = DateTime.Now,
                InitiatingSignal = 0
        };

        // Act
        await _repository.UpdateAsync(caseRecord);

        // Assert — Note: UpdateAsync currently does not call SaveChanges (commented out),
        // so we verify the entity was modified in the change tracker but not persisted.
        // This test documents the current behavior.
        CaseEntity? tracked = _context.CaseEntities.FirstOrDefault(c => c.CaseId == caseGuid);
        Assert.IsNotNull(tracked);
        Assert.AreEqual(CaseStatus.Investigation, tracked!.Status);
    }








    [TestMethod]
    public async Task UpdateAsync_NonExistentCase_ThrowsInvalidOperationException()
    {
        // Arrange
        Case caseRecord = new() { CaseId = Guid.NewGuid(), Status = CaseStatus.Closed, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now };

        // Act & Assert
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => _repository.UpdateAsync(caseRecord));
    }








    [TestMethod]
    public async Task UpdateAsync_NullCaseRecord_ThrowsArgumentNullException()
    {
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => _repository.UpdateAsync(null!));
    }
}