// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         JsonFileMcpServerRegistryStore.cs
// Author: Kyle L. Crowder
// Build Num:  092308



using System.Text.Json;

using Microsoft.Extensions.Logging;

using SentinelCore.Contracts.Mcp;




namespace SentinelCore.Orchestrations.Mcp;





/// <summary>
///     Persists MCP server definitions to a JSON file using System.Text.Json.
/// </summary>
public sealed class JsonFileMcpServerRegistryStore : IMcpServerRegistryStore
{

    private readonly string _filePath;
    private readonly ILogger<JsonFileMcpServerRegistryStore> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true, PropertyNameCaseInsensitive = true };








    /// <summary>
    ///     Initializes a new instance of the <see cref="JsonFileMcpServerRegistryStore" /> class.
    /// </summary>
    /// <param name="filePath">The full path to the JSON persistence file.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    public JsonFileMcpServerRegistryStore(string filePath, ILogger<JsonFileMcpServerRegistryStore> logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(logger);

        _filePath = filePath;
        _logger = logger;
    }








    public async Task<IReadOnlyList<McpServerDefinition>> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
        {
            _logger.LogInformation("No persisted MCP registry found at {FilePath}; returning empty list.", _filePath);
            return Array.Empty<McpServerDefinition>();
        }

        try
        {
            await using FileStream stream = File.OpenRead(_filePath);
            List<McpServerDefinition>? definitions = await JsonSerializer.DeserializeAsync<List<McpServerDefinition>>(stream, JsonOptions, cancellationToken).ConfigureAwait(false);

            return definitions ?? [];
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            _logger.LogError(ex, "Failed to load MCP registry from {FilePath}; returning empty list.", _filePath);
            return Array.Empty<McpServerDefinition>();
        }
    }








    public async Task SaveAsync(IReadOnlyList<McpServerDefinition> definitions, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definitions);

        string? directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        byte[] json = JsonSerializer.SerializeToUtf8Bytes(definitions, JsonOptions);
        await File.WriteAllBytesAsync(_filePath, json, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Persisted {Count} MCP server definition(s) to {FilePath}.", definitions.Count, _filePath);
    }
}