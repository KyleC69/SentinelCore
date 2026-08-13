// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         SentinelChatClientBuilderExtensions.cs
// Author: Kyle L. Crowder
// Build Num:  081312



using Microsoft.Extensions.Logging;

using SentinelCore.Agents.Middleware;
using SentinelCore.Events;




namespace SentinelCore.Agents;





/// <summary>
///     Extension methods for adding SentinelCore middleware to ChatClientBuilder.
/// </summary>
public static class SentinelChatClientBuilderExtensions
{

    /// <summary>
    ///     Adds SentinelCore event publishing middleware.
    /// </summary>
    public static ChatClientBuilder UseSentinelEvents(this ChatClientBuilder builder, ISentinelCoreEvents? events, string agentName, ILogger? logger)
    {
        if (events == null || logger == null || string.IsNullOrEmpty(agentName))
        {
            return builder; // Skip if not configured
        }

        return builder.Use((inner, services) => new EventPublishingChatClient(inner, events, agentName, logger));
    }
}