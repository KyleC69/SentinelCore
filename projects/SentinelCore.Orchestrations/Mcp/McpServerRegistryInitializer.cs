// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         McpServerRegistryInitializer.cs
// Author: Kyle L. Crowder
// Build Num:  091300



using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using SentinelCore.Contracts.Mcp;




namespace SentinelCore.Orchestrations.Mcp;





/// <summary>
///     Hosted service that loads persisted MCP server definitions on application startup.
///     It intentionally does not auto-start connections so the UI remains in control.
/// </summary>
public sealed class McpServerRegistryInitializer : IHostedService
{
    private readonly ILogger<McpServerRegistryInitializer> _logger;
    private readonly IMcpServerRegistry _registry;








    /// <summary>
    ///     Initializes a new instance of the <see cref="McpServerRegistryInitializer" /> class.
    /// </summary>
    /// <param name="registry">The MCP server registry.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    public McpServerRegistryInitializer(IMcpServerRegistry registry, ILogger<McpServerRegistryInitializer> logger)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(logger);

        _registry = registry;
        _logger = logger;
    }








    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_registry is McpServerRegistry serverRegistry)
        {
            _logger.LogInformation("Loading persisted MCP server definitions.");
            await serverRegistry.LoadPersistedAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            _logger.LogWarning("MCP registry initializer expected {ExpectedType} but received {ActualType}.", typeof(McpServerRegistry).Name, _registry.GetType().Name);
        }
    }








    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}