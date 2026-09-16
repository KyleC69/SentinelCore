// Solution: SentinelCore
// Project:   SentinelCore.Contracts
// File:         McpServerTransportType.cs
// Author: Kyle L. Crowder
// Build Num:  091418



namespace SentinelCore.Contracts.Mcp;





/// <summary>
///     Identifies the transport protocol used to communicate with an MCP server.
/// </summary>
public enum McpServerTransportType
{
    /// <summary>
    ///     The server is a local executable connected via standard input/output.
    /// </summary>
    Stdio,

    /// <summary>
    ///     The server is a remote HTTP endpoint using StreamableHttp or Server-Sent Events.
    /// </summary>
    Http
}