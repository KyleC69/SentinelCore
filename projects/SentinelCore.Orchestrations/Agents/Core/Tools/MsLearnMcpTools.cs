// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         MsLearnMcpTools.cs
// Author: Kyle L. Crowder
// Build Num:  111504



using System.ComponentModel;
using System.Net.Http.Json;





namespace SentinelCore.Orchestrations.Agents.Core.Tools;








/// <summary>
///     Temporary in-process tools for Microsoft Learn documentation search and retrieval over the Microsoft Learn
///     MCP endpoint. Registered as the <c>sc_knowledge_</c> tool group.
/// </summary>
/// <remarks>
///     <para>
///         TEMPORARY IMPLEMENTATION: the tooling component design doc mandates that an ADR describe this as a
///         temporary implementation with a mandatory re-design in the future. The JSON-RPC <c>tools/call</c>
///         plumbing is deliberately kept as-is until that redesign; only the three former tool classes were
///         consolidated into this host and the MCP response envelope is not parsed (the raw response body is
///         returned verbatim).
///     </para>
///     <para>
///         The endpoint is read from the <see cref="EndpointEnvironmentVariable" /> environment variable and
///         defaults to <see cref="DefaultEndpoint" />.
///     </para>
/// </remarks>
public sealed class MsLearnMcpTools
{
    /// <summary>
    ///     The default public Microsoft Learn MCP endpoint.
    /// </summary>
    public const string DefaultEndpoint = "https://learn.microsoft.com/api/mcp";

    /// <summary>
    ///     The environment variable that overrides the Microsoft Learn MCP endpoint.
    /// </summary>
    public const string EndpointEnvironmentVariable = "MSLEARN_MCP_ENDPOINT";

    private static readonly HttpClient HttpClient = new();

    private readonly string _endpoint;








    /// <summary>
    ///     Initializes a new instance of the <see cref="MsLearnMcpTools" /> class.
    /// </summary>
    /// <param name="endpoint">
    ///     The Microsoft Learn MCP endpoint to call. When <c>null</c> or whitespace, the value of the
    ///     <see cref="EndpointEnvironmentVariable" /> environment variable is used when it is set to a non-empty
    ///     value, and <see cref="DefaultEndpoint" /> is used otherwise.
    /// </param>
    public MsLearnMcpTools(string? endpoint = null)
    {
        _endpoint = ResolveEndpoint(endpoint);
    }








    /// <summary>
    ///     Performs semantic search against Microsoft official technical documentation.
    /// </summary>
    /// <param name="query">The search query for retrieval.</param>
    /// <returns>A result whose payload is the raw MCP endpoint response body, or a failure result.</returns>
    [Description("Performs semantic search against Microsoft official technical documentation")]
    public async Task<ToolResult> SearchDocs([Description("The search query for retrieval")] string query)
    {
        object payload = new { jsonrpc = "2.0", id = "docs-search", method = "tools/call", @params = new { name = "microsoft_docs_search", arguments = new { query } } };

        return await CallLearnEndpoint(payload).ConfigureAwait(false);
    }








    /// <summary>
    ///     Fetches a Microsoft documentation page and converts it into markdown format.
    /// </summary>
    /// <param name="docUrl">The URL of the documentation page to fetch.</param>
    /// <returns>A result whose payload is the raw MCP endpoint response body, or a failure result.</returns>
    [Description("Fetch and convert a Microsoft documentation page into markdown format")]
    public async Task<ToolResult> FetchDocs([Description("The url of the doc you wish to fetch")] string docUrl)
    {
        object payload = new { jsonrpc = "2.0", id = "docs-fetch", method = "tools/call", @params = new { name = "microsoft_docs_fetch", arguments = new { url = docUrl } } };

        return await CallLearnEndpoint(payload).ConfigureAwait(false);
    }








    /// <summary>
    ///     Searches for official Microsoft and Azure code snippets and examples.
    /// </summary>
    /// <param name="query">The search term to search for on Microsoft Learn documentation.</param>
    /// <param name="language">Optional programming language filter; may be <c>null</c>.</param>
    /// <returns>A result whose payload is the raw MCP endpoint response body, or a failure result.</returns>
    [Description("Search for official Microsoft/Azure code snippets and examples")]
    public async Task<ToolResult> SearchCodeSamples([Description("Search term to search for on MS Learn docs")] string query, [Description("Optional programming language filter")] string? language)
    {
        object payload = new { jsonrpc = "2.0", id = "code-search", method = "tools/call", @params = new { name = "microsoft_code_sample_search", arguments = new { query, language } } };

        return await CallLearnEndpoint(payload).ConfigureAwait(false);
    }








    /// <summary>
    ///     Posts a JSON-RPC <c>tools/call</c> payload to the Microsoft Learn MCP endpoint and wraps the outcome in a
    ///     <see cref="ToolResult" />.
    /// </summary>
    /// <param name="payload">The JSON-RPC request payload to post.</param>
    /// <returns>A success result carrying the raw response body, or a failure result describing the error.</returns>
    private async Task<ToolResult> CallLearnEndpoint(object payload)
    {
        try
        {
            using HttpResponseMessage response = await HttpClient.PostAsync(_endpoint, JsonContent.Create(payload)).ConfigureAwait(false);
            string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            return ToolResult.Ok(json);
        }
        catch (Exception e)
        {
            return ToolResult.Fail($"The Microsoft Learn MCP endpoint call failed. Exception: {e.Message}");
        }
    }








    /// <summary>
    ///     Resolves the endpoint to call from the explicit argument, the environment variable, or the default.
    /// </summary>
    /// <param name="endpoint">The explicitly supplied endpoint, or <c>null</c>.</param>
    /// <returns>The endpoint to call.</returns>
    private static string ResolveEndpoint(string? endpoint)
    {
        if (!string.IsNullOrWhiteSpace(endpoint))
        {
            return endpoint;
        }

        string? fromEnvironment = Environment.GetEnvironmentVariable(EndpointEnvironmentVariable);

        return string.IsNullOrWhiteSpace(fromEnvironment) ? DefaultEndpoint : fromEnvironment;
    }
}
