// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         AISystemsInitialization.cs
// Author: Kyle L. Crowder
// Build Num:  100310



using Microsoft.Extensions.Hosting;

using SentinelCore.Contracts.Abstractions;
using SentinelCore.Contracts.Mcp;
using SentinelCore.Orchestrations.Abstractions;




namespace SentinelCore.Orchestrations.Application;





public sealed class AISystemsInitialization : BackgroundService
{
    private readonly IMcpServerRegistry _mcpRegistry;
    private readonly IOrchestrationControl _orchestrationControl;
    private readonly ISystemReporter _reporter;








    /// <summary>
    ///     Initializes a new instance of the <see cref="AISystemsInitialization" /> class.
    /// </summary>
    /// <param name="orchestrationControl">The orchestration control.</param>
    /// <param name="reporter">The system reporter.</param>
    /// <param name="mcpServerRegistry">The MCP server registry.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public AISystemsInitialization(IOrchestrationControl orchestrationControl, ISystemReporter reporter, IMcpServerRegistry mcpServerRegistry)
    {
        _orchestrationControl = orchestrationControl ?? throw new ArgumentNullException(nameof(orchestrationControl));
        _reporter = reporter ?? throw new ArgumentNullException(nameof(reporter));
        _mcpRegistry = mcpServerRegistry ?? throw new ArgumentNullException(nameof(mcpServerRegistry));
    }








    /// <summary>
    ///     This method is called when the <see cref="T:Microsoft.Extensions.Hosting.IHostedService" /> starts. The
    ///     implementation should return a task that represents
    ///     the lifetime of the long running operation(s) being performed.
    /// </summary>
    /// <param name="stoppingToken">
    ///     Triggered when
    ///     <see cref="M:Microsoft.Extensions.Hosting.IHostedService.StopAsync(System.Threading.CancellationToken)" /> is
    ///     called.
    /// </param>
    /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the long running operations.</returns>
    /// <remarks>
    ///     See <see href="https://learn.microsoft.com/dotnet/core/extensions/workers">Worker Services in .NET</see> for
    ///     implementation guidelines.
    /// </remarks>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await EnsureMcpServersStartedAsync(stoppingToken);
        await _orchestrationControl.InitializeOrchestrationAsync(stoppingToken);
    }








    /// <summary>
    ///     Ensures all registered MCP servers are started before the orchestration executes.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task EnsureMcpServersStartedAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<McpServerInfo> servers = await _mcpRegistry.ListAsync(cancellationToken).ConfigureAwait(false);

        foreach (McpServerInfo server in servers)
        {
            try
            {
                await _mcpRegistry.StartAsync(server.Definition.Id, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _reporter.ReportWarning($"Failed to start MCP server '{server.Definition.Id}': {ex.Message}");
            }
        }
    }
}