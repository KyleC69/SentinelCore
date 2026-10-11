// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         JsonFileMcpServerRegistryStore.cs
// Author: Kyle L. Crowder
// Build Num:  100908



using System.Text.Json;

using Microsoft.Extensions.Logging;

using SentinelCore.Contracts.Mcp;




namespace SentinelCore.Orchestrations.Mcp;





/// <summary>
/// Represents a store for MCP server definitions that persists data to a JSON file.
/// </summary>
/// <remarks>
/// This class provides functionality to load and save MCP server definitions using the JSON format.
/// It ensures proper logging and handles potential exceptions during file operations.
/// </remarks>
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







    /// <summary>
    /// Loads and deserializes the persisted MCP registry from disk and returns the server definitions.
    /// </summary>
    /// <remarks>Logs an informational message when the registry file is missing and logs errors for JSON or
    /// I/O exceptions. Uses JsonSerializer with the configured JsonOptions and opens the file for reading.</remarks>
    /// <param name="cancellationToken">A token to cancel the asynchronous read and deserialization operation.</param>
    /// <returns>A task that resolves to a read-only list of McpServerDefinition. Returns an empty list if the file is missing or
    /// if deserialization or I/O fails.</returns>
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








    /// <summary>
    /// Saves and serializes the MCP server definitions to disk as a JSON file.
    /// </summary>
    /// <remarks>Logs an informational message upon successful persistence and handles potential I/O exceptions.</remarks>
    /// <param name="definitions">The MCP server definitions to persist.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous write operation.</param>
    /// <returns>A task that represents the asynchronous save operation.</returns>
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