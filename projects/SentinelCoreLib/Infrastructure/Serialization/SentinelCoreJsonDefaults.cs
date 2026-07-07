// Solution: SentinelCoreLib
// Project:   SentinelCoreLib
// File:         SentinelCoreJsonDefaults.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



using System.Text.Json;




namespace SentinelCoreLib.Infrastructure.Serialization;





/// <summary>
///     Provides shared JSON serialization options for the library.
/// </summary>
public static class SentinelCoreJsonDefaults
{
    /// <summary>
    ///     Gets the default serializer options used for persistence and tool results.
    /// </summary>
    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web) { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = false };
}