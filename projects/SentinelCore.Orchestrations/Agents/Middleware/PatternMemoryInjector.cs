// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         PatternMemoryInjector.cs
// Author: Kyle L. Crowder
// Build Num:  081312



using System.Diagnostics.CodeAnalysis;




namespace SentinelCore.Agents.Middleware;





/// <summary>
///     Searches pattern memory for previous cases matching user task (signal) to investigate and will inject the case for
///     "The Core" to reason over.
/// </summary>
public class PatternMemoryInjector : MessageAIContextProvider
{

    protected override ValueTask<AIContext> InvokingCoreAsync([NotNull] AIContextProvider.InvokingContext context, CancellationToken cancellationToken = default)
    {
        return base.InvokingCoreAsync(context, cancellationToken);
    }








    protected override ValueTask<IEnumerable<ChatMessage>> InvokingCoreAsync([NotNull] InvokingContext context, CancellationToken cancellationToken = default)
    {
        return base.InvokingCoreAsync(context, cancellationToken);
    }








    protected override ValueTask<AIContext> ProvideAIContextAsync([NotNull] AIContextProvider.InvokingContext context, CancellationToken cancellationToken = default)
    {
        return base.ProvideAIContextAsync(context, cancellationToken);
    }








    protected override ValueTask<IEnumerable<ChatMessage>> ProvideMessagesAsync([NotNull] InvokingContext context, CancellationToken cancellationToken = default)
    {
        return base.ProvideMessagesAsync(context, cancellationToken);
    }
}