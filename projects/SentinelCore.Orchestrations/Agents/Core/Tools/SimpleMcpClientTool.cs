// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         SimpleMcpClientTool.cs
// Author: Kyle L. Crowder
// Build Num:  081312



using System.ComponentModel;
using System.Net.Http.Json;
using System.Text.Json;




namespace SentinelCore.Agents.Core.Tools;





public sealed class MicrosoftDocsSearchTool : AITool
{
    private readonly string _endpoint;








    public MicrosoftDocsSearchTool(string endpoint)
    {
        _endpoint = endpoint;
    }








    [Description("Performs semantic search against Microsoft official technical documentation")]
    public async Task<object?> microsoft_docs_search([Description("query (string): The search query for retrieval")] string? input)
    {
        string query = input ?? "";

        using HttpClient client = new();
        var payload = new { jsonrpc = "2.0", id = "docs-search", method = "tools/call", @params = new { name = "microsoft_docs_search", arguments = new { query } } };

        HttpResponseMessage response = await client.PostAsync(_endpoint, JsonContent.Create(payload)).ConfigureAwait(false);
        string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        return JsonSerializer.Deserialize<object>(json);
    }
}





public sealed class MicrosoftDocsFetchTool : AITool
{
    private readonly string _endpoint;








    public MicrosoftDocsFetchTool(string endpoint)
    {
        _endpoint = endpoint;
    }








    [Description("Fetch and convert a Microsoft documentation page into markdown format")]
    public async Task<string?> microsoft_docs_fetch([Description("The url of the doc you wish to fetch.")] string? docUrl)
    {
        string url = docUrl ?? "";

        using HttpClient client = new HttpClient();
        var payload = new { jsonrpc = "2.0", id = "docs-fetch", method = "tools/call", @params = new { name = "microsoft_docs_fetch", arguments = new { url } } };

        HttpResponseMessage response = await client.PostAsync(_endpoint, JsonContent.Create(payload)).ConfigureAwait(false);
        string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        return JsonSerializer.Deserialize<string>(json);
    }
}





public sealed class MicrosoftCodeSampleSearchTool : AITool
{
    private readonly string _endpoint;








    public MicrosoftCodeSampleSearchTool(string endpoint)
    {
        _endpoint = endpoint;
    }








    [Description("Search for official Microsoft/Azure code snippets and examples")]
    public async Task<object?> microsoft_code_sample_search([Description("Search term to search for on MS Learn docs")] string query, [Description("Optional programming language filter")] string? language)
    {
        // Expecting input to be an anonymous object with { query, language }

        using HttpClient client = new HttpClient();
        var payload = new { jsonrpc = "2.0", id = "code-search", method = "tools/call", @params = new { name = "microsoft_code_sample_search", arguments = new { query, language } } };

        HttpResponseMessage response = await client.PostAsync(_endpoint, JsonContent.Create(payload)).ConfigureAwait(false);
        string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        return JsonSerializer.Deserialize<object>(json);
    }
}