// Solution: SentinelCore
// Project:   SentinelCore.UI
// File:         FileModelConfigStore.cs
// Author: Kyle L. Crowder
// Build Num:  091200



using System.IO;
using System.Text.Json;

using Microsoft.Extensions.Logging;




namespace SentinelCore.UI.Services;





/// <summary>
///     File-backed implementation of <see cref="IModelConfigStore" /> that persists
///     the model configuration document as JSON under
///     <c>%APPDATA%\SentinelCore\model-configuration.json</c>.
///     Corrupt or unreadable documents are logged and treated as absent so the
///     application always starts with usable defaults.
/// </summary>
public sealed class FileModelConfigStore : IModelConfigStore
{

    private readonly string _filePath;

    private readonly ILogger<FileModelConfigStore> _logger;

    /// <summary>
    ///     The file name of the persisted document.
    /// </summary>
    public const string FileName = "model-configuration.json";

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };








    /// <summary>
    ///     Creates a store persisting to the default application-data location.
    /// </summary>
    /// <param name="logger">The logger for load/save failures.</param>
    public FileModelConfigStore(ILogger<FileModelConfigStore> logger) : this(logger, null)
    {
    }








    /// <summary>
    ///     Creates a store persisting to an explicit file path. When
    ///     <paramref name="filePath" /> is <c>null</c>, the default application-data
    ///     location is used.
    /// </summary>
    /// <param name="logger">The logger for load/save failures.</param>
    /// <param name="filePath">Explicit document path, or <c>null</c> for the default location.</param>
    public FileModelConfigStore(ILogger<FileModelConfigStore> logger, string? filePath)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _filePath = filePath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SentinelCore", FileName);
    }








    /// <inheritdoc />
    public ModelConfigDocument? Load()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                return null;
            }

            string json = File.ReadAllText(_filePath);
            ModelConfigDocument? document = JsonSerializer.Deserialize<ModelConfigDocument>(json, JsonOptions);

            if (document is null)
            {
                _logger.LogWarning("Model configuration document at {Path} deserialized to null; using defaults.", _filePath);
            }

            return document;
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            // A corrupt or locked document must never block startup — fall back to defaults.
            _logger.LogWarning(ex, "Failed to load the model configuration document from {Path}; using defaults.", _filePath);
            return null;
        }
    }








    /// <inheritdoc />
    public void Save(ModelConfigDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        try
        {
            string? directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = JsonSerializer.Serialize(document, JsonOptions);
            File.WriteAllText(_filePath, json);

            _logger.LogInformation("Saved model configuration to {Path}.", _filePath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogError(ex, "Failed to save the model configuration document to {Path}.", _filePath);
            throw;
        }
    }
}