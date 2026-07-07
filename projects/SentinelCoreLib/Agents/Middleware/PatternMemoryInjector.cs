// Solution: SentinelCoreLib
// Project:   SentinelCoreLib
// File:         PatternMemoryInjector.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;




namespace SentinelCoreLib.Agents.Middleware;





/// <summary>
///     Searches pattern memory for previous cases matching user task (signal) to investigate and will inject the case for
///     "The Core" to reason over
/// </summary>
public class PatternMemoryInjector : MessageAIContextProvider
{

    protected override ValueTask<AIContext> InvokingCoreAsync(AIContextProvider.InvokingContext context, CancellationToken cancellationToken = default)
    {
        return base.InvokingCoreAsync(context, cancellationToken);
    }








    protected override ValueTask<IEnumerable<ChatMessage>> InvokingCoreAsync(InvokingContext context, CancellationToken cancellationToken = default)
    {
        return base.InvokingCoreAsync(context, cancellationToken);
    }








    protected override ValueTask<AIContext> ProvideAIContextAsync(AIContextProvider.InvokingContext context, CancellationToken cancellationToken = default)
    {
        return base.ProvideAIContextAsync(context, cancellationToken);
    }








    protected override ValueTask<IEnumerable<ChatMessage>> ProvideMessagesAsync(InvokingContext context, CancellationToken cancellationToken = default)
    {
        return base.ProvideMessagesAsync(context, cancellationToken);
    }
}