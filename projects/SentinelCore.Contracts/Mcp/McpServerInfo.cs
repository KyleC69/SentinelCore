// Solution: SentinelCore
// Project:   SentinelCore.Contracts
// File:         McpServerInfo.cs
// Author: Kyle L. Crowder
// Build Num:  091300



namespace SentinelCore.Contracts.Mcp;





/// <summary>
///     Runtime information describing an MCP server and its current connection state.
/// </summary>
public sealed class McpServerInfo
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="McpServerInfo" /> class.
    /// </summary>
    /// <param name="definition">Immutable configuration for the server.</param>
    /// <param name="status">Current runtime lifecycle status.</param>
    /// <param name="toolNames">Names of tools currently available from the server.</param>
    /// <param name="lastError">Optional last error message when status is <see cref="McpServerStatus.Error" />.</param>
    public McpServerInfo(McpServerDefinition definition, McpServerStatus status, IReadOnlyList<string> toolNames, string? lastError = null)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(toolNames);

        Definition = definition;
        Status = status;
        ToolNames = toolNames;
        LastError = lastError;
    }








    /// <summary>Gets the immutable configuration for the server.</summary>
    public McpServerDefinition Definition { get; }

    /// <summary>Gets the optional last error message.</summary>
    public string? LastError { get; }

    /// <summary>Gets the current runtime lifecycle status.</summary>
    public McpServerStatus Status { get; }

    /// <summary>Gets the names of tools currently available from the server.</summary>
    public IReadOnlyList<string> ToolNames { get; }
}