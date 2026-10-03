// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         ResilienceClientContributor.cs
// Author: Kyle L. Crowder
// Build Num:  100310



using SentinelCore.Contracts.Abstractions;




namespace SentinelCore.Orchestrations.Agents;





/// <summary>
///     Wraps the agent chat client with resilience behavior during agent construction.
/// </summary>
/// <remarks>
///     This contributor keeps endpoint validation and transient retry policy at the agent boundary,
///     not inside workflow executors.
/// </remarks>
public sealed class ResilienceClientContributor : IAgentConstructionContributor
{
    private readonly ISystemReporter _reporter;








    /// <summary>
    ///     Initializes a new instance of the <see cref="ResilienceClientContributor" /> class.
    /// </summary>
    /// <param name="reporter">The system reporter used by the resilience wrapper for diagnostics.</param>
    public ResilienceClientContributor(ISystemReporter reporter)
    {
        _reporter = reporter ?? throw new ArgumentNullException(nameof(reporter));
    }








    /// <summary>
    ///     Wraps <see cref="AgentConstructionContext.WrappedClient" /> with <see cref="ResilientChatClient" />.
    /// </summary>
    /// <param name="context">The construction context holding the current wrapped client and model profile.</param>
    /// <param name="cancellationToken">A cancellation token for cooperative cancellation.</param>
    /// <returns>A completed task.</returns>
    public Task ContributeAsync(AgentConstructionContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.WrappedClient = new ResilientChatClient(context.WrappedClient, context.Model, _reporter, context.Preset.AgentName);
        return Task.CompletedTask;
    }








    /// <summary>
    ///     Gets the execution order for this contributor.
    /// </summary>
    public int Order
    {
        get => 21;
    }
}