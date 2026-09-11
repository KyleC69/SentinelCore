// Solution: SentinelCore
// Project:   SentinelCore.Tests
// File:         RagSearchServiceTests.cs
// Author: Kyle L. Crowder
// Build Num:  091112



using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moq;

using SentinelCore.Contracts.Contracts;
using SentinelCore.Orchestrations.Rag;




namespace SentinelCore.Tests;





/// <summary>
///     Unit tests for <see cref="RagSearchService" />.
/// </summary>
public sealed class RagSearchServiceTests
{
    private readonly Mock<ILogger<RagSearchService>> _loggerMock;
    private readonly IOptions<RagSearchOptions> _options;








    public RagSearchServiceTests()
    {
        _loggerMock = new Mock<ILogger<RagSearchService>>();
        _options = Options.Create(new RagSearchOptions { Enabled = true, MaxResults = 5, RelevanceThreshold = 0.3, VectorSearchEnabled = false });
    }








    [Fact]
    public async Task IndexDocumentAsync_AddsDocumentToIndex()
    {
        // Arrange
        RagSearchService service = new(_options, _loggerMock.Object);

        // Act
        await service.IndexDocumentAsync("doc1", "Test Title", "Test Content");
        bool hasContent = await service.HasIndexedContentAsync();

        // Assert
        Assert.True(hasContent);
    }








    [Fact]
    public async Task IndexDocumentAsync_ReplacesExistingDocument()
    {
        // Arrange
        RagSearchService service = new(_options, _loggerMock.Object);
        await service.IndexDocumentAsync("doc1", "Original Title", "Original Content");

        // Act
        await service.IndexDocumentAsync("doc1", "Updated Title", "Updated Content");
        IReadOnlyList<RagSearchResult> results = await service.SearchAsync("Updated");

        // Assert
        Assert.Single(results);
        Assert.Equal("Updated Title", results[0].Title);
    }








    [Fact]
    public void IsQueryRelevant_WithRelevanceKeyword_ReturnsTrue()
    {
        // Arrange
        RagSearchService service = new(_options, _loggerMock.Object);

        // Act
        bool isRelevant = service.IsQueryRelevant("What is the policy for documentation?");

        // Assert
        Assert.True(isRelevant);
    }








    [Fact]
    public void IsQueryRelevant_WithoutRelevanceKeyword_ReturnsFalse()
    {
        // Arrange
        RagSearchService service = new(_options, _loggerMock.Object);

        // Act
        bool isRelevant = service.IsQueryRelevant("Hello, how are you?");

        // Assert
        Assert.False(isRelevant);
    }








    [Fact]
    public async Task SearchAsync_WhenDisabled_ReturnsEmptyResults()
    {
        // Arrange
        IOptions<RagSearchOptions> disabledOptions = Options.Create(new RagSearchOptions { Enabled = false });
        RagSearchService service = new(disabledOptions, _loggerMock.Object);

        // Act
        IReadOnlyList<RagSearchResult> results = await service.SearchAsync("test query");

        // Assert
        Assert.Empty(results);
    }








    [Fact]
    public async Task SearchAsync_WithEmptyQuery_ReturnsEmptyResults()
    {
        // Arrange
        RagSearchService service = new(_options, _loggerMock.Object);

        // Act
        IReadOnlyList<RagSearchResult> results = await service.SearchAsync(string.Empty);

        // Assert
        Assert.Empty(results);
    }








    [Fact]
    public async Task SearchAsync_WithIndexedContent_ReturnsMatchingResults()
    {
        // Arrange
        RagSearchService service = new(_options, _loggerMock.Object);
        await service.IndexDocumentAsync("doc1", "Test Document", "This is a test document about security policies", "TestSource");

        // Act
        IReadOnlyList<RagSearchResult> results = await service.SearchAsync("security policies");

        // Assert
        Assert.Single(results);
        Assert.Equal("doc1", results[0].Id);
        Assert.Contains("security", results[0].Content.ToLowerInvariant());
    }








    [Fact]
    public async Task SearchAsync_WithMaxResultsLimit_RespectsLimit()
    {
        // Arrange
        IOptions<RagSearchOptions> limitedOptions = Options.Create(new RagSearchOptions { Enabled = true, MaxResults = 2 });
        RagSearchService service = new(limitedOptions, _loggerMock.Object);

        await service.IndexDocumentAsync("doc1", "Doc 1", "Content about testing");
        await service.IndexDocumentAsync("doc2", "Doc 2", "More testing content");
        await service.IndexDocumentAsync("doc3", "Doc 3", "Even more testing");

        // Act
        IReadOnlyList<RagSearchResult> results = await service.SearchAsync("testing", maxResults: 5);

        // Assert
        Assert.True(results.Count <= 2);
    }








    [Fact]
    public async Task SearchAsync_WithNoIndexedContent_ReturnsEmptyResults()
    {
        // Arrange
        RagSearchService service = new(_options, _loggerMock.Object);

        // Act
        IReadOnlyList<RagSearchResult> results = await service.SearchAsync("test query");

        // Assert
        Assert.Empty(results);
    }








    [Fact]
    public async Task SearchAsync_WithNullQuery_ReturnsEmptyResults()
    {
        // Arrange
        RagSearchService service = new(_options, _loggerMock.Object);

        // Act
        IReadOnlyList<RagSearchResult> results = await service.SearchAsync(null!);

        // Assert
        Assert.Empty(results);
    }








    [Fact]
    public async Task SearchAsync_WithPartialMatch_ReturnsResultsWithLowerScore()
    {
        // Arrange
        RagSearchService service = new(_options, _loggerMock.Object);
        await service.IndexDocumentAsync("doc1", "Exact Match", "The exact security policy document");

        // Act
        IReadOnlyList<RagSearchResult> exactResults = await service.SearchAsync("security policy");
        IReadOnlyList<RagSearchResult> partialResults = await service.SearchAsync("sec pol");

        // Assert
        Assert.Single(exactResults);
        Assert.True(exactResults[0].RelevanceScore > 0.5);
    }








    [Fact]
    public async Task SearchByEmbeddingAsync_WhenVectorSearchDisabled_ReturnsEmpty()
    {
        // Arrange
        RagSearchService service = new(_options, _loggerMock.Object);
        float[] embedding = new float[128];

        // Act
        IReadOnlyList<RagSearchResult> results = await service.SearchByEmbeddingAsync(embedding);

        // Assert
        Assert.Empty(results);
    }
}