// Solution: SentinelCore
// Project:   SentinelCore.Tests
// File:         PatternMatcherTests.cs
// Author: Kyle L. Crowder
// Build Num:  091112



using Microsoft.Extensions.Logging;

using Moq;

using SentinelCore.Contracts.Abstractions;
using SentinelCore.Orchestrations.Agents.Middleware;




namespace SentinelCore.Tests;





[TestClass]
public class SemanticPatternMatcherTests
{
    private readonly SemanticPatternMatcher _matcher;
    private readonly Mock<ILogger<SemanticPatternMatcher>> _mockLogger;
    private readonly Mock<IPatternMemoryStore> _mockPatternStore;








    public SemanticPatternMatcherTests()
    {
        _mockPatternStore = new Mock<IPatternMemoryStore>();
        _mockLogger = new Mock<ILogger<SemanticPatternMatcher>>();
        _matcher = new SemanticPatternMatcher(_mockPatternStore.Object, _mockLogger.Object);
    }








    [TestMethod]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsExactly<ArgumentNullException>(() => new SemanticPatternMatcher(_mockPatternStore.Object, null!));
    }








    [TestMethod]
    public void Constructor_WithNullPatternStore_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsExactly<ArgumentNullException>(() => new SemanticPatternMatcher(null!, _mockLogger.Object));
    }








    [TestMethod]
    public async Task SearchAsync_WithEmptySignal_ReturnsEmptyResults()
    {
        // Act
        IReadOnlyList<PatternMatchResult> results = await _matcher.SearchAsync("");

        // Assert
        Assert.IsNotNull(results);
        Assert.AreEqual(0, results.Count);
    }








    [TestMethod]
    public async Task SearchAsync_WithNullSignal_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => _matcher.SearchAsync(null!));
    }








    [TestMethod]
    public async Task SearchAsync_WithValidSignalButNoEmbeddings_ReturnsEmptyResults_TODO_Implement()
    {
        // This test documents the expected behavior when embeddings become available
        // Currently returns empty due to stub implementation

        // Act
        IReadOnlyList<PatternMatchResult> results = await _matcher.SearchAsync("help me troubleshoot a network issue");

        // Assert - Currently stub returns empty, full impl will return matches
        Assert.IsNotNull(results);
        Assert.AreEqual(0, results.Count);
    }








    [TestMethod]
    public async Task SearchAsync_WithWhitespaceSignal_ReturnsEmptyResults()
    {
        // Act
        IReadOnlyList<PatternMatchResult> results = await _matcher.SearchAsync("   ");

        // Assert
        Assert.IsNotNull(results);
        Assert.AreEqual(0, results.Count);
    }








    [TestMethod]
    public async Task SearchByEmbeddingAsync_WhenStoreReturnsEmpty_ReturnsEmptyResults()
    {
        // Arrange
        float[] embedding = [0.1f, 0.2f, 0.3f, 0.4f];
        _mockPatternStore.Setup(s => s.SearchAsync(embedding, 5, It.IsAny<CancellationToken>())).ReturnsAsync(new List<PatternMemoryResult>());

        // Act
        IReadOnlyList<PatternMatchResult> results = await _matcher.SearchByEmbeddingAsync(embedding);

        // Assert
        Assert.IsNotNull(results);
        Assert.AreEqual(0, results.Count);
    }








    [TestMethod]
    public async Task SearchByEmbeddingAsync_WhenStoreReturnsResults_TransformsToPatternMatchResults()
    {
        // Arrange
        float[] embedding = [0.1f, 0.2f, 0.3f, 0.4f];
        var storeResults = new List<PatternMemoryResult> { new() { CaseId = 1, PatternId = 100, Summary = "Network troubleshooting pattern", Timestamp = DateTime.UtcNow }, new() { CaseId = 2, PatternId = 101, Summary = "Database connection issue", Timestamp = DateTime.UtcNow } };

        _mockPatternStore.Setup(s => s.SearchAsync(embedding, 5, It.IsAny<CancellationToken>())).ReturnsAsync(storeResults);

        // Act
        IReadOnlyList<PatternMatchResult> results = await _matcher.SearchByEmbeddingAsync(embedding);

        // Assert
        Assert.IsNotNull(results);
        Assert.AreEqual(2, results.Count);
        Assert.AreEqual(PatternMatchMethod.Semantic, results[0].MatchMethod);
        Assert.AreEqual(100, results[0].Context.PatternId);
        Assert.AreEqual(1, results[0].Context.CaseId);
        Assert.AreEqual("Network troubleshooting pattern", results[0].Context.Summary);
    }








    [TestMethod]
    public async Task SearchByEmbeddingAsync_WhenStoreThrows_ReturnsEmptyResults()
    {
        // Arrange
        float[] embedding = [0.1f, 0.2f];
        _mockPatternStore.Setup(s => s.SearchAsync(embedding, 5, It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("Database error"));

        // Act
        IReadOnlyList<PatternMatchResult> results = await _matcher.SearchByEmbeddingAsync(embedding);

        // Assert
        Assert.IsNotNull(results);
        Assert.AreEqual(0, results.Count);
    }








    [TestMethod]
    public async Task SearchByEmbeddingAsync_WithEmptyEmbedding_ReturnsEmptyResults()
    {
        // Act
        IReadOnlyList<PatternMatchResult> results = await _matcher.SearchByEmbeddingAsync([]);

        // Assert
        Assert.IsNotNull(results);
        Assert.AreEqual(0, results.Count);
    }








    [TestMethod]
    public async Task SearchByEmbeddingAsync_WithNullEmbedding_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => _matcher.SearchByEmbeddingAsync(null!));
    }
}





[TestClass]
public class PatternContextTests
{
    [TestMethod]
    public void PatternContext_DefaultKeyPhrases_IsEmptyList()
    {
        // Arrange & Act
        PatternContext context = new() { PatternId = 1, CaseId = 1, Summary = "Test pattern" };

        // Assert
        Assert.IsNotNull(context.KeyPhrases);
        Assert.AreEqual(0, context.KeyPhrases.Count);
    }








    [TestMethod]
    public void PatternContext_DefaultTags_IsEmptyList()
    {
        // Arrange & Act
        PatternContext context = new() { PatternId = 1, CaseId = 1, Summary = "Test pattern" };

        // Assert
        Assert.IsNotNull(context.Tags);
        Assert.AreEqual(0, context.Tags.Count);
    }








    [TestMethod]
    public void PatternContext_WithTags_StoresCorrectly()
    {
        // Arrange & Act
        PatternContext context = new() { PatternId = 1, CaseId = 1, Summary = "Network issue", Tags = ["network", "troubleshooting"] };

        // Assert
        Assert.AreEqual(2, context.Tags.Count);
        Assert.IsTrue(context.Tags.Contains("network"));
        Assert.IsTrue(context.Tags.Contains("troubleshooting"));
    }
}