// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         RagSearchTool.cs
// Author: Kyle L. Crowder
// Build Num:  091418



using System.ComponentModel;
using System.Text;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using SentinelCore.Contracts.Contracts;
using SentinelCore.Orchestrations.Tools;




namespace SentinelCore.Orchestrations.Rag;





/// <summary>
///     RAG search tool that allows agents to query the knowledge base.
///     Provides on-demand search capability for retrieving relevant documentation and context.
/// </summary>
public sealed class RagSearchTool : AITool
{
    private readonly ILogger<RagSearchTool> _logger;
    private readonly RagSearchOptions _options;
    private readonly IRagSearchService _searchService;








    /// <summary>
    ///     Initializes a new instance of the <see cref="RagSearchTool" /> class.
    /// </summary>
    /// <param name="searchService">The RAG search service.</param>
    /// <param name="options">RAG search configuration options.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public RagSearchTool(IRagSearchService searchService, IOptions<RagSearchOptions> options, ILogger<RagSearchTool> logger)
    {
        ArgumentNullException.ThrowIfNull(searchService);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _searchService = searchService;
        _options = options.Value;
        _logger = logger;
    }









    public override string Description { get; } = "Searches the knowledge base for relevant documentation, policies, and reference materials. " + "Use this tool when you need to find information about procedures, guidelines, specifications, " + "or any documented knowledge. The search returns the most relevant results with context.";


    public override string Name { get; } = "SearchKnowledgeBase";








    /// <summary>
    ///     Formats search results into a readable string for the agent.
    /// </summary>
    private string FormatSearchResults(string query, IReadOnlyList<RagSearchResult> results)
    {
        StringBuilder sb = new();
        sb.AppendLine("## Knowledge Base Search Results");
        sb.AppendLine();
        sb.AppendLine($"**Query:** {query}");
        sb.AppendLine($"**Results Found:** {results.Count}");
        sb.AppendLine();

        for (int i = 0; i < results.Count; i++)
        {
            RagSearchResult result = results[i];
            sb.AppendLine($"### {i + 1}. {result.Title}");
            sb.AppendLine($"**Relevance:** {result.RelevanceScore:P0}");

            if (!string.IsNullOrEmpty(result.Source))
            {
                sb.AppendLine($"**Source:** {result.Source}");
            }

            // Truncate content if too long
            string content = result.Content;
            int maxContentLength = 2000;
            if (content.Length > maxContentLength)
            {
                content = content[..maxContentLength] + "...";
            }

            sb.AppendLine();
            sb.AppendLine(content);
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }

        sb.AppendLine("*End of search results*");

        return sb.ToString();
    }








    /// <summary>
    ///     Searches the knowledge base for documents matching the query.
    /// </summary>
    /// <param name="query">
    ///     The search query. Be specific and include relevant keywords for better results.
    ///     Example: "What is the incident response procedure for data breaches?"
    /// </param>
    /// <param name="maxResults">
    ///     Maximum number of results to return (default: 5).
    ///     Use fewer results for focused queries, more for broad searches.
    /// </param>
    /// <returns>A formatted string containing search results or an empty message if no results found.</returns>
    [Description("Search the knowledge base for relevant documentation and context")]
    public async Task<ToolResult> SearchKnowledgeBase([Description("The search query to find relevant knowledge base entries")] string query, [Description("Maximum number of results to return (default: 5)")] int maxResults = 5)
    {
        if (!_options.Enabled)
        {
            _logger.LogDebug("RAG search tool called but RAG is disabled");
            return ToolResult.Fail("RAG search is currently disabled.");
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            return ToolResult.Fail("Search query cannot be empty. Please provide a search term or question.");
        }

        try
        {
            _logger.LogDebug("Knowledge base search requested: {Query}, maxResults: {MaxResults}", query, maxResults);

            IReadOnlyList<RagSearchResult> results = await _searchService.SearchAsync(query, maxResults, CancellationToken.None).ConfigureAwait(false);

            if (results.Count == 0)
            {
                _logger.LogDebug("No results found for query: {Query}", query);
                return ToolResult.Ok($"No relevant knowledge base entries found for: \"{query}\".\n\n" + "Try:\n" + "• Using different keywords\n" + "• Being more specific in your search terms\n" + "• Checking if the knowledge base has been populated");
            }

            string formattedResults = FormatSearchResults(query, results);
            _logger.LogDebug("Returning {Count} results for query: {Query}", results.Count, query);

            return ToolResult.Ok(formattedResults);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing knowledge base search for query: {Query}", query);
            return ToolResult.Fail($"An error occurred while searching the knowledge base: {ex.Message}");
        }
    }
}
