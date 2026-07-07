// Solution: SentinelCoreLib
// Project:   SentinelCoreLib
// File:         IToolRegistry.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



using Microsoft.Extensions.AI;




namespace SentinelCoreLib.Application.Abstractions;





/// <summary>
///     Registry of deterministic tools exposed to agents.
/// </summary>
public interface IToolRegistry
{

    /// <summary>
    ///     Returns true if a tool with the specified name is registered.
    /// </summary>
    /// <param name="name">The tool name.</param>
    /// <returns>True if the tool is registered; otherwise false.</returns>
    bool Contains(string name);








    /// <summary>
    ///     Gets the tool with the specified name, or null if not found.
    /// </summary>
    /// <param name="name">The tool name.</param>
    /// <returns>The matching tool, or null.</returns>
    AITool? GetTool(string name);








    IList<AITool>? GetToolByDomain(string domain);








    /// <summary>
    ///     Gets the tools whose names are contained in the provided list.
    /// </summary>
    /// <param name="names">The requested tool names.</param>
    /// <returns>The matching tools.</returns>
    IReadOnlyList<AITool> GetToolsByName(IEnumerable<string> names);
}