// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         MSDocsMCPServerTool.cs
// Author: Kyle L. Crowder
// Build Num:  080801



using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;




namespace SentinelCore.Agents.Core.Tools;





/// <summary>
///     Exposes Microsoft Learn MCP capabilities to the agent as a read-only tool.
/// </summary>
public sealed class MsDocsMcpServerTool : AIFunction
{
    private const string DefaultEndpoint = "https://learn.microsoft.com/api/mcp";

    private static readonly JsonElement SSchema = JsonDocument.Parse("""
                                                                     {
                                                                       "type": "object",
                                                                       "properties": {
                                                                         "operation": { "type": "string", "enum": ["list_tools", "search", "fetch"], "description": "Action to perform against the Microsoft Learn MCP server." },
                                                                         "query": { "type": "string", "description": "Natural-language search query for Microsoft Learn content." },
                                                                         "url": { "type": "string", "description": "Absolute Microsoft Learn page URL to fetch." },
                                                                         "toolName": { "type": "string", "description": "Optional MCP tool name to invoke. Defaults to search or fetch depending on the chosen operation." },
                                                                         "endpoint": { "type": "string", "description": "Optional MCP server endpoint. Defaults to https://learn.microsoft.com/api/mcp." },
                                                                         "maxTokenBudget": { "type": "integer", "description": "Optional token budget to send to the MCP server. Defaults to 2000." }
                                                                       },
                                                                       "required": ["operation"]
                                                                     }
                                                                     """)
            .RootElement;

    public override string Description
    {
        get => "Connects to the Microsoft Learn MCP server to discover available documentation tools and run read-only searches or page fetches.";
    }

    public override JsonElement JsonSchema
    {
        get => SSchema;
    }

    public override string Name
    {
        get => "mslearn_mcp_query";
    }








    protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
    {
        string operation = (arguments["operation"]?.ToString() ?? "list_tools").Trim().ToLowerInvariant();
        string endpoint = (arguments["endpoint"]?.ToString() ?? DefaultEndpoint).Trim();
        string? toolName = arguments["toolName"]?.ToString();
        string? query = arguments["query"]?.ToString();
        string? url = arguments["url"]?.ToString();
        int maxTokenBudget = Convert.ToInt32(arguments["maxTokenBudget"] != null ? arguments["maxTokenBudget"] : 2000);

        if (string.IsNullOrWhiteSpace(endpoint)) endpoint = DefaultEndpoint;

        try
        {
            using HttpClient httpClient = CreateHttpClient();
            await InitializeAsync(httpClient, endpoint, cancellationToken).ConfigureAwait(false);

            if (operation == "list_tools")
            {
                IReadOnlyList<string> tools = await ListToolsAsync(httpClient, endpoint, cancellationToken).ConfigureAwait(false);
                return new { endpoint, toolCount = tools.Count, tools };
            }

            if (operation == "search")
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return new { error = "query is required for search operations." };
                }

                string requestTool = string.IsNullOrWhiteSpace(toolName) ? "search" : toolName;
                Dictionary<string, object?> payload = new Dictionary<string, object?> { ["query"] = query, ["maxTokenBudget"] = maxTokenBudget };

                object? result = await CallToolAsync(httpClient, endpoint, requestTool, payload, cancellationToken).ConfigureAwait(false);
                return new { endpoint, tool = requestTool, query, result };
            }

            if (operation == "fetch")
            {
                if (string.IsNullOrWhiteSpace(url))
                {
                    return new { error = "url is required for fetch operations." };
                }

                string requestTool = string.IsNullOrWhiteSpace(toolName) ? "fetch" : toolName;
                Dictionary<string, object?> payload = new Dictionary<string, object?> { ["url"] = url };

                object? result = await CallToolAsync(httpClient, endpoint, requestTool, payload, cancellationToken).ConfigureAwait(false);
                return new { endpoint, tool = requestTool, url, result };
            }

            return new { error = $"Unsupported operation: {operation}" };
        }
        catch (Exception ex)
        {
            return new { error = ex.Message };
        }
    }








    private static async Task<object?> CallToolAsync(HttpClient httpClient, string endpoint, string toolName, IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        JsonElement response = await SendJsonRpcAsync(httpClient, endpoint, new { jsonrpc = "2.0", id = $"tool-call-{Guid.NewGuid():N}", method = "tools/call", @params = new { name = toolName, arguments } }, cancellationToken).ConfigureAwait(false);

        return response;
    }








    private static HttpClient CreateHttpClient()
    {
        HttpClient client = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };

        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        client.DefaultRequestHeaders.UserAgent.ParseAdd("SentinelCore/1.0");
        return client;
    }








    private static async Task InitializeAsync(HttpClient httpClient, string endpoint, CancellationToken cancellationToken)
    {
        var initializePayload = new { jsonrpc = "2.0", id = "init-1", method = "initialize", @params = new { protocolVersion = "2025-03-26", capabilities = new { }, clientInfo = new { name = "SentinelCore", version = "1.0" } } };

        await SendJsonRpcAsync(httpClient, endpoint, initializePayload, cancellationToken).ConfigureAwait(false);

        var initializedNotification = new { jsonrpc = "2.0", method = "notifications/initialized", @params = new { } };

        await SendJsonRpcAsync(httpClient, endpoint, initializedNotification, cancellationToken).ConfigureAwait(false);
    }








    private static async Task<IReadOnlyList<string>> ListToolsAsync(HttpClient httpClient, string endpoint, CancellationToken cancellationToken)
    {
        JsonElement response = await SendJsonRpcAsync(httpClient, endpoint, new { jsonrpc = "2.0", id = "tools-list-1", method = "tools/list", @params = new { } }, cancellationToken).ConfigureAwait(false);

        return response.TryGetProperty("result", out JsonElement resultNode) && resultNode.TryGetProperty("tools", out JsonElement toolsNode) && toolsNode.ValueKind == JsonValueKind.Array ? toolsNode.EnumerateArray().Select(item => item.TryGetProperty("name", out JsonElement nameNode) ? nameNode.GetString() : null).Where(name => !string.IsNullOrWhiteSpace(name)).Cast<string>().ToList() : Array.Empty<string>();
    }








    private static async Task<JsonElement> SendJsonRpcAsync(HttpClient httpClient, string endpoint, object payload, CancellationToken cancellationToken)
    {
        using JsonContent requestContent = JsonContent.Create(payload);
        using HttpResponseMessage response = await httpClient.PostAsync(endpoint, requestContent, cancellationToken).ConfigureAwait(false);
        string responseText = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"MCP request failed with status {(int)response.StatusCode}: {responseText}");
        }

        if (string.IsNullOrWhiteSpace(responseText))
        {
            return JsonDocument.Parse("{}").RootElement.Clone();
        }

        string candidate = responseText;
        if (candidate.Contains("data:", StringComparison.OrdinalIgnoreCase))
        {
            IEnumerable<string> lines = candidate.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Where(line => line.StartsWith("data:", StringComparison.OrdinalIgnoreCase)).Select(line => line[5..].Trim()).Where(line => !string.IsNullOrWhiteSpace(line));

            candidate = string.Join(Environment.NewLine, lines);
        }

        return JsonDocument.Parse(candidate).RootElement.Clone();
    }
}