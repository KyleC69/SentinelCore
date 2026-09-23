// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         McpToolContributor.cs
// Author: Kyle L. Crowder
// Build Num:  092308



using SentinelCore.Contracts.Mcp;




namespace SentinelCore.Orchestrations.Agents;





/// <summary>
///     Resolves MCP tools from the <see cref="IMcpServerRegistry" /> and adds them
///     to the agent's tool list. Tools are filtered by the agent's logical name.
/// </summary>
public sealed class McpToolContributor : IAgentConstructionContributor
{
    private readonly IMcpServerRegistry _mcpServerRegistry;








    /// <summary>
    ///     Initializes a new instance of the <see cref="McpToolContributor" /> class.
    /// </summary>
    /// <param name="mcpServerRegistry">The MCP server registry for resolving connected server tools.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="mcpServerRegistry" /> is null.</exception>
    public McpToolContributor(IMcpServerRegistry mcpServerRegistry)
    {
        _mcpServerRegistry = mcpServerRegistry ?? throw new ArgumentNullException(nameof(mcpServerRegistry));
    }








    /// <inheritdoc />
    public async Task ContributeAsync(AgentConstructionContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        IReadOnlyList<AITool> serverTools = await _mcpServerRegistry.GetToolsForAgentAsync(context.Preset.AgentName, cancellationToken).ConfigureAwait(false);

        if (serverTools.Count > 0)
        {
            context.Tools.AddRange(serverTools);
        }
    }








    /// <inheritdoc />
    public int Order
    {
        get => 40;
    }
}