// Solution: SentinelCore
// Project:   SentinelCore.Contracts
// File:         McpServerDefinition.cs
// Author: Kyle L. Crowder
// Build Num:  091418



namespace SentinelCore.Contracts.Mcp;





/// <summary>
///     Immutable configuration data describing an MCP server that can be registered
///     for use by Sentinel agents.
/// </summary>
public sealed class McpServerDefinition
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="McpServerDefinition" /> class.
    /// </summary>
    /// <param name="id">Unique identifier for the server.</param>
    /// <param name="displayName">Human-readable display name.</param>
    /// <param name="transportType">Transport used to reach the server.</param>
    /// <param name="commandOrEndpoint">
    ///     For <see cref="McpServerTransportType.Stdio" /> this is the executable command;
    ///     for <see cref="McpServerTransportType.Http" /> this is the absolute URL of the MCP endpoint.
    /// </param>
    /// <param name="arguments">Command-line arguments for stdio servers.</param>
    /// <param name="workingDirectory">Working directory for stdio servers.</param>
    /// <param name="environmentVariables">Environment variables for stdio servers.</param>
    /// <param name="oauthSettings">Optional OAuth settings for HTTP servers.</param>
    /// <param name="assignedAgentNames">
    ///     Optional collection of logical agent names that are allowed to use this server.
    ///     When empty, the server is available to all agents.
    /// </param>
    public McpServerDefinition(string id, string displayName, McpServerTransportType transportType, string commandOrEndpoint, IReadOnlyList<string>? arguments = null, string? workingDirectory = null, IReadOnlyDictionary<string, string>? environmentVariables = null, McpOAuthSettings? oauthSettings = null, IReadOnlyList<string>? assignedAgentNames = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(commandOrEndpoint);

        Id = id;
        DisplayName = displayName;
        TransportType = transportType;
        CommandOrEndpoint = commandOrEndpoint;
        Arguments = arguments ?? [];
        WorkingDirectory = workingDirectory;
        EnvironmentVariables = environmentVariables ?? new Dictionary<string, string>();
        OAuthSettings = oauthSettings;
        AssignedAgentNames = assignedAgentNames ?? [];
    }








    /// <summary>Gets the command-line arguments for stdio servers.</summary>
    public IReadOnlyList<string> Arguments { get; }

    /// <summary>
    ///     Gets the collection of logical agent names allowed to use this server.
    ///     An empty collection means the server is available to all agents.
    /// </summary>
    public IReadOnlyList<string> AssignedAgentNames { get; }

    /// <summary>
    ///     Gets the executable command for stdio servers or the MCP endpoint URL for HTTP servers.
    /// </summary>
    public string CommandOrEndpoint { get; }

    /// <summary>Gets the human-readable display name.</summary>
    public string DisplayName { get; }

    /// <summary>Gets the environment variables for stdio servers.</summary>
    public IReadOnlyDictionary<string, string> EnvironmentVariables { get; }

    /// <summary>Gets the unique identifier for the server.</summary>
    public string Id { get; }

    /// <summary>Gets the optional OAuth settings for protected HTTP servers.</summary>
    public McpOAuthSettings? OAuthSettings { get; }

    /// <summary>Gets the transport used to reach the server.</summary>
    public McpServerTransportType TransportType { get; }

    /// <summary>Gets the working directory for stdio servers, if any.</summary>
    public string? WorkingDirectory { get; }
}