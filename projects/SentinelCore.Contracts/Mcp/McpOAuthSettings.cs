// Solution: SentinelCore
// Project:   SentinelCore.Contracts
// File:         McpOAuthSettings.cs
// Author: Kyle L. Crowder
// Build Num:  092308



namespace SentinelCore.Contracts.Mcp;





/// <summary>
///     OAuth configuration for an HTTP-based MCP server that requires bearer-token authorization.
/// </summary>
public sealed class McpOAuthSettings
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="McpOAuthSettings" /> class.
    /// </summary>
    /// <param name="authorizationEndpoint">Authorization endpoint URL.</param>
    /// <param name="tokenEndpoint">Token endpoint URL.</param>
    /// <param name="clientId">OAuth client identifier.</param>
    /// <param name="scopes">Space-delimited OAuth scopes.</param>
    /// <param name="callbackUri">Loopback callback URI used by the client (e.g. <c>http://127.0.0.1:0</c>).</param>
    /// <param name="timeout">Maximum time to wait for the authorization callback.</param>
    public McpOAuthSettings(string authorizationEndpoint, string tokenEndpoint, string clientId, string? scopes = null, string? callbackUri = null, TimeSpan? timeout = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(authorizationEndpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenEndpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);

        AuthorizationEndpoint = authorizationEndpoint;
        TokenEndpoint = tokenEndpoint;
        ClientId = clientId;
        Scopes = scopes;
        CallbackUri = callbackUri;
        Timeout = timeout ?? TimeSpan.FromMinutes(2);
    }








    /// <summary>Gets the authorization endpoint URL.</summary>
    public string AuthorizationEndpoint { get; }

    /// <summary>
    ///     Gets the optional loopback callback URI. When <c>null</c> the implementation will
    ///     start a listener on an ephemeral loopback port.
    /// </summary>
    public string? CallbackUri { get; }

    /// <summary>Gets the OAuth client identifier.</summary>
    public string ClientId { get; }

    /// <summary>Gets the optional space-delimited OAuth scopes.</summary>
    public string? Scopes { get; }

    /// <summary>Gets the maximum time to wait for the authorization callback.</summary>
    public TimeSpan Timeout { get; }

    /// <summary>Gets the token endpoint URL.</summary>
    public string TokenEndpoint { get; }
}