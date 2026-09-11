// Solution: SentinelCore
// Project:   SentinelCore.Tests
// File:         FileModelConfigStoreTests.cs
// Author: Kyle L. Crowder
// Build Num:  091112



using System.IO;

using Microsoft.Extensions.Logging.Abstractions;

using SentinelCore.Contracts.Contracts;

using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;




namespace SentinelCore.Tests;





/// <summary>
///     Unit tests for <see cref="FileModelConfigStore" /> covering round-trip
///     persistence, missing-document handling, and corrupt-file recovery.
/// </summary>
[TestClass]
public sealed class FileModelConfigStoreTests
{

    [TestMethod]
    public void Constructor_NullLogger_Throws()
    {
        // Arrange
        string path = Path.Combine(Path.GetTempPath(), "unused.json");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new FileModelConfigStore(null!, path));
    }








    /// <summary>
    ///     Creates a store persisting to a unique temp file.
    /// </summary>
    /// <returns>The store and the path it persists to.</returns>
    private static (FileModelConfigStore Store, string Path) CreateStore()
    {
        string path = Path.Combine(Path.GetTempPath(), $"model-config-{Guid.NewGuid():N}.json");
        FileModelConfigStore store = new(NullLogger<FileModelConfigStore>.Instance, path);
        return (store, path);
    }








    [TestMethod]
    public void Load_CorruptFile_ReturnsNull()
    {
        // Arrange — a file that is not valid JSON.
        (FileModelConfigStore store, string path) = CreateStore();

        try
        {
            File.WriteAllText(path, "{ not valid json");

            // Act — a corrupt document must never block startup.
            ModelConfigDocument? document = store.Load();

            // Assert
            Assert.IsNull(document);
        }
        finally
        {
            File.Delete(path);
        }
    }








    [TestMethod]
    public void Load_MissingFile_ReturnsNull()
    {
        // Arrange
        (FileModelConfigStore store, string path) = CreateStore();

        try
        {
            // Act
            ModelConfigDocument? document = store.Load();

            // Assert
            Assert.IsNull(document);
        }
        finally
        {
            File.Delete(path);
        }
    }








    [TestMethod]
    public void SaveThenLoad_RoundTripsAgentModels()
    {
        // Arrange
        (FileModelConfigStore store, string path) = CreateStore();

        try
        {
            ModelConfigDocument document = new();
            document.AgentModels["TheCore"] = new ModelProfile("http://localhost:11434", "model-a", 0.2f, 8000, 1, 0.3f);
            document.AgentModels["Manager"] = new ModelProfile("https://api.example.com", "model-b", 0.1f);

            // Act
            store.Save(document);
            ModelConfigDocument? loaded = store.Load();

            // Assert
            Assert.IsNotNull(loaded);
            Assert.AreEqual(2, loaded.AgentModels.Count);

            ModelProfile core = loaded.AgentModels["TheCore"];
            Assert.AreEqual("http://localhost:11434", core.Endpoint);
            Assert.AreEqual("model-a", core.ModelId);
            Assert.AreEqual(0.2f, core.Temperature);
            Assert.AreEqual(8000, core.MaxOutputTokens);
        }
        finally
        {
            File.Delete(path);
        }
    }








    [TestMethod]
    public void Save_CreatesMissingDirectory()
    {
        // Arrange — a nested directory that does not exist yet.
        string directory = Path.Combine(Path.GetTempPath(), $"model-config-dir-{Guid.NewGuid():N}");
        string path = Path.Combine(directory, "config.json");
        FileModelConfigStore store = new(NullLogger<FileModelConfigStore>.Instance, path);

        try
        {
            ModelConfigDocument document = new();
            document.AgentModels["TheCore"] = new ModelProfile("http://localhost:11434", "model-a", 0.1f);

            // Act
            store.Save(document);

            // Assert
            Assert.IsTrue(File.Exists(path));
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }








    [TestMethod]
    public void Save_NullDocument_Throws()
    {
        // Arrange
        (FileModelConfigStore store, string path) = CreateStore();

        try
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => store.Save(null!));
        }
        finally
        {
            File.Delete(path);
        }
    }
}