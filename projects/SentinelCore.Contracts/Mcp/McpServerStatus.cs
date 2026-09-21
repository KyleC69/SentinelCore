// Solution: SentinelCore
// Project:   SentinelCore.Contracts
// File:         McpServerStatus.cs
// Author: Kyle L. Crowder
// Build Num:  091418



namespace SentinelCore.Contracts.Mcp;





/// <summary>
///     Represents the runtime lifecycle status of an MCP server connection.
/// </summary>
public enum McpServerStatus
{
    /// <summary>
    ///     The server is not connected and no connection attempt is in progress.
    /// </summary>
    Stopped,

    /// <summary>
    ///     A connection attempt is in progress. For OAuth-enabled HTTP servers this may include the
    ///     authorization-code/PKCE browser flow.
    /// </summary>
    Starting,

    /// <summary>
    ///     The server is connected and tools have been enumerated successfully.
    /// </summary>
    Connected,

    /// <summary>
    ///     The last connection attempt failed. The error is captured in <see cref="McpServerInfo.LastError" />.
    /// </summary>
    Error
}