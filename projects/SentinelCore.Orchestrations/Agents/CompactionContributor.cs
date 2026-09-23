// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         CompactionContributor.cs
// Author: Kyle L. Crowder
// Build Num:  092308



#pragma warning disable MEAI001, MAAI001 // MAF evaluation types — suppress to proceed

using Microsoft.Agents.AI.Compaction;




namespace SentinelCore.Orchestrations.Agents;





/// <summary>
///     Adds the <see cref="CompactionProvider" /> to the agent's context providers.
///     Compaction manages conversation context window by trimming older messages
///     when token limits are exceeded.
/// </summary>
public sealed class CompactionContributor : IAgentConstructionContributor
{



    /// <inheritdoc />
    public Task ContributeAsync(AgentConstructionContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        PipelineCompactionStrategy pipeline = new(new ToolResultCompactionStrategy(CompactionTriggers.TokensExceed(0x200)), new SlidingWindowCompactionStrategy(CompactionTriggers.TurnsExceed(25)), new TruncationCompactionStrategy(CompactionTriggers.TokensExceed(0x128000)));

        context.ContextProviders.Add(new CompactionProvider(pipeline));

        return Task.CompletedTask;
    }








    /// <inheritdoc />
    public int Order
    {
        get => 50;
    }
}




#pragma warning restore MEAI001, MAAI001