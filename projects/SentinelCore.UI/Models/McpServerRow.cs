// Solution: SentinelCore
// Project:   SentinelCore.UI
// File:         McpServerRow.cs
// Author: Kyle L. Crowder
// Build Num:  091300



using SentinelCore.Contracts.Mcp;




namespace SentinelCore.UI.Models;





/// <summary>
///     Represents a single MCP server in the registry list shown on the
///     <see cref="SentinelCore.UI.Views.McpServersPage" />.
/// </summary>
public sealed class McpServerRow
{

    /// <summary>
    ///     The logical agent names assigned to use this server, formatted for display.
    /// </summary>
    public string AssignedAgentsText { get; set; } = string.Empty;

    /// <summary>
    ///     The command for stdio servers or the endpoint URL for HTTP servers.
    /// </summary>
    public string CommandOrEndpoint { get; set; } = string.Empty;

    /// <summary>
    ///     The human-readable display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    ///     The unique identifier of the server.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    ///     The last error message, if any.
    /// </summary>
    public string? LastError { get; set; }

    /// <summary>
    ///     The current runtime status of the server.
    /// </summary>
    public McpServerStatus Status { get; set; }

    /// <summary>
    ///     The names of tools available from the server, formatted for display.
    /// </summary>
    public string ToolNamesText { get; set; } = string.Empty;

    /// <summary>
    ///     The transport protocol (stdio or HTTP).
    /// </summary>
    public McpServerTransportType TransportType { get; set; }
}