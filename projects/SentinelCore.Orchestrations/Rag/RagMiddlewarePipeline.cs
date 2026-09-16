// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         RagMiddlewarePipeline.cs
// Author: Kyle L. Crowder
// Build Num:  091418



using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using SentinelCore.Contracts.Contracts;




namespace SentinelCore.Orchestrations.Rag;





/// <summary>
///     Configures the RAG middleware pipeline for agent context enhancement.
///     This class provides factory methods for creating RAG middleware components
///     with proper dependency injection configuration.
/// </summary>
public static class RagMiddlewarePipeline
{
    /// <summary>
    ///     Creates a RAG context injector if RAG is enabled in options.
    /// </summary>
    /// <param name="options">RAG search configuration options.</param>
    /// <param name="searchService">The RAG search service.</param>
    /// <param name="loggerFactory">Logger factory for creating loggers.</param>
    /// <returns>A new <see cref="RagContextInjector" /> instance, or null if disabled.</returns>
    public static RagContextInjector? CreateContextInjector(IOptions<RagSearchOptions> options, IRagSearchService searchService, ILoggerFactory loggerFactory)
    {
        if (!options.Value.AutoInjectEnabled)
        {
            return null;
        }

        ILogger<RagContextInjector> logger = loggerFactory.CreateLogger<RagContextInjector>();
        return new RagContextInjector(searchService, options, logger);
    }








    /// <summary>
    ///     Gets all RAG-related tools for an agent.
    /// </summary>
    /// <param name="searchService">The RAG search service.</param>
    /// <param name="options">RAG search configuration options.</param>
    /// <param name="loggerFactory">Logger factory for creating loggers.</param>
    /// <returns>A list of AI tools to add to an agent's toolset.</returns>
    public static IReadOnlyList<AITool> GetTools(IRagSearchService searchService, IOptions<RagSearchOptions> options, ILoggerFactory loggerFactory)
    {
        if (!options.Value.Enabled)
        {
            return Array.Empty<AITool>();
        }

        var tools = new List<AITool>();

        ILogger<RagSearchTool> logger = loggerFactory.CreateLogger<RagSearchTool>();
        tools.Add(new RagSearchTool(searchService, options, logger));

        return tools;
    }
}