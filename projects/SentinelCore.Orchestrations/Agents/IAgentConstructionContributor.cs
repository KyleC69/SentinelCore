// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         IAgentConstructionContributor.cs
// Author: Kyle L. Crowder
// Build Num:  092200



#nullable enable




namespace SentinelCore.Orchestrations.Agents;





/// <summary>
///     Contributes a specific piece of agent configuration during construction.
///     Implementations are registered in DI and resolved by the factory.
///     Each contributor owns exactly one concern (logging, pattern memory, MCP tools, etc.).
///     <para>
///         Contributors handle construction-time concerns only. Invocation-time concerns
///         (instructions, response format) are handled by executors at <c>RunAsync</c> call time.
///     </para>
/// </summary>
public interface IAgentConstructionContributor
{
    /// <summary>
    ///     Gets the order in which this contributor runs relative to others.
    ///     Lower values run first. Contributors must document their ordering requirements.
    ///     <list type="bullet">
    ///         <item>10-19: Instruction contributors</item>
    ///         <item>20-29: Chat client wrapper contributors (logging, etc.)</item>
    ///         <item>30-39: Context provider contributors (pattern memory, etc.)</item>
    ///         <item>40-49: Tool contributors (MCP, RAG, etc.)</item>
    ///         <item>50-59: Infrastructure contributors (compaction, etc.)</item>
    ///     </list>
    /// </summary>
    int Order { get; }

    /// <summary>
    ///     Contributes to the agent construction context. The contributor inspects
    ///     the preset and adds its piece (client wrappers, context providers, tools).
    /// </summary>
    /// <param name="context">The accumulator for construction decisions.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ContributeAsync(AgentConstructionContext context, CancellationToken cancellationToken);
}
