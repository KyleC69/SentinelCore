// Solution: SentinelCore
// Project:   SentinelCore.Tests
// File:         CaseFlowEngineTests.cs
// Author: Kyle L. Crowder
// Build Num:  080801



using Moq;

using SentinelCore.Abstractions;
using SentinelCore.CaseFlow;




namespace SentinelCore.Tests.CaseFlow;





/// <summary>
///     Unit tests for <see cref="CaseFlowEngine" />.
/// </summary>
[TestClass]
public sealed class CaseFlowEngineTests
{
    private Mock<ICaseRepository> _caseRepositoryMock = null!;
    private CaseFlowEngine _engine = null!;
    private Mock<IEvidenceStore> _evidenceStoreMock = null!;
    private Mock<ISafetyMiddleware> _safetyMiddlewareMock = null!;








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
    public void Constructor_NullCaseRepository_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(() => new CaseFlowEngine(null!, _evidenceStoreMock.Object, _safetyMiddlewareMock.Object));
    }








    [TestMethod]
    public void Constructor_NullEvidenceStore_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(() => new CaseFlowEngine(_caseRepositoryMock.Object, null!, _safetyMiddlewareMock.Object));
    }








    [TestMethod]
    public void Constructor_NullSafetyMiddleware_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(() => new CaseFlowEngine(_caseRepositoryMock.Object, _evidenceStoreMock.Object, null!));
    }








    [TestMethod]
    public async Task CreateCaseAsync_CancellationRequested_PropagatesToken()
    {
        // Arrange
        Signal signal = new("test signal", "source-a");
        CancellationTokenSource cts = new();
        CancellationToken token = cts.Token;
        _caseRepositoryMock.Setup(r => r.CreateCaseWithSignalAsync(signal, It.IsAny<Case>(), token)).ReturnsAsync(1);

        // Act
        await _engine.CreateCaseAsync(signal, token);

        // Assert
        _caseRepositoryMock.Verify(r => r.CreateCaseWithSignalAsync(signal, It.IsAny<Case>(), token), Times.Once);
    }








    [TestMethod]
    public async Task CreateCaseAsync_GeneratesNonEmptyGuid()
    {
        // Arrange
        Signal signal = new("test signal", "source-a");
        _caseRepositoryMock.Setup(r => r.CreateCaseWithSignalAsync(signal, It.IsAny<Case>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);

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
    public async Task CreateCaseAsync_SetsCreatedAtAndUpdatedAtToUtcNow()
    {
        // Arrange
        Signal signal = new("test signal", "source-a");
        Case? capturedCase = null;
        _caseRepositoryMock.Setup(r => r.CreateCaseWithSignalAsync(signal, It.IsAny<Case>(), It.IsAny<CancellationToken>())).Callback<Signal, Case, CancellationToken>((_, c, _) => capturedCase = c).ReturnsAsync(1);

        DateTime before = DateTime.Now;

        // Act
        await _engine.CreateCaseAsync(signal);

        DateTime after = DateTime.Now;

        // Assert
        Assert.IsNotNull(capturedCase);
        Assert.IsTrue(capturedCase!.CreatedAt >= before && capturedCase.CreatedAt <= after);
        Assert.IsTrue(capturedCase.UpdatedAt >= before && capturedCase.UpdatedAt <= after);
    }








    [TestMethod]
    public async Task CreateCaseAsync_SetsStatusToOpen()
    {
        // Arrange
        Signal signal = new("test signal", "source-a");
        Case? capturedCase = null;
        _caseRepositoryMock.Setup(r => r.CreateCaseWithSignalAsync(signal, It.IsAny<Case>(), It.IsAny<CancellationToken>())).Callback<Signal, Case, CancellationToken>((_, c, _) => capturedCase = c).ReturnsAsync(1);

        // Act
        await _engine.CreateCaseAsync(signal);

        // Assert
        Assert.IsNotNull(capturedCase);
        Assert.AreEqual(CaseStatus.Open, capturedCase!.Status);
    }








    [TestMethod]
    public async Task CreateCaseAsync_ValidSignal_CallsRepositoryAndReturnsGuid()
    {
        // Arrange
        Signal signal = new("anomaly detected", "sensor-01");
        _caseRepositoryMock.Setup(r => r.CreateCaseWithSignalAsync(signal, It.IsAny<Case>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        Guid caseId = await _engine.CreateCaseAsync(signal);

        // Assert
        Assert.AreNotEqual(Guid.Empty, caseId);
        _caseRepositoryMock.Verify(r => r.CreateCaseWithSignalAsync(signal, It.IsAny<Case>(), It.IsAny<CancellationToken>()), Times.Once);
    }








    [TestInitialize]
    public void TestInitialize()
    {
        _caseRepositoryMock = new Mock<ICaseRepository>();
        _evidenceStoreMock = new Mock<IEvidenceStore>();
        _safetyMiddlewareMock = new Mock<ISafetyMiddleware>();
        _engine = new CaseFlowEngine(_caseRepositoryMock.Object, _evidenceStoreMock.Object, _safetyMiddlewareMock.Object);
    }
}