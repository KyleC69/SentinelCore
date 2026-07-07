// Solution: SentinelCoreLib
// Project:   SentinelCoreLib
// File:         SafetyMiddleware.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;




namespace SentinelCoreLib.Agents.Middleware;





/// <summary>
///     Safety Middleware to examine user input and filter malicious input
/// </summary>
public sealed class SafetyMiddleware : MessageAIContextProvider
{
    // To be designed later, placeholder registration
    //








    protected override ValueTask InvokedCoreAsync(InvokedContext context, CancellationToken cancellationToken = default)
    {

        return base.InvokedCoreAsync(context, cancellationToken);
    }








    protected override ValueTask<IEnumerable<ChatMessage>> InvokingCoreAsync(InvokingContext context, CancellationToken cancellationToken = default)
    {
        return base.InvokingCoreAsync(context, cancellationToken);
    }
}