// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         EventPublishingChatClient.cs
// Author: Kyle L. Crowder
// Build Num:  082808



using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.Logging;

using SentinelCore.Events;




namespace SentinelCore.Agents.Middleware;





/// <summary>
///     A delegating <see cref="IChatClient" /> that publishes both tool/function-call
///     results and agent text output to <see cref="ISentinelCoreEvents" />.
///     This is the primary event capture mechanism for SentinelCore agents.
/// </summary>
/// <remarks>
///     <para>
///         This middleware captures two streams of communication:
///     </para>
///     <list type="bullet">
///         <item>Tool results — from <see cref="FunctionResultContent" /> in responses.</item>
///         <item>Agent text output — from <see cref="TextContent" /> in responses.</item>
///     </list>
///     <para>
///         In a function-calling loop, intermediate responses contain function call
///         requests and results. The final response contains the agent's text output.
///         Only responses with <see cref="TextContent" /> trigger text-event publishing,
///         avoiding noise from intermediate function-call rounds.
///     </para>
/// </remarks>
public sealed class EventPublishingChatClient : DelegatingChatClient
{
    private readonly string _agentName;
    private readonly ISentinelCoreEvents _events;
    private readonly ILogger _logger;








    /// <summary>
    ///     Initializes a new instance of the <see cref="EventPublishingChatClient" /> class.
    /// </summary>
    /// <param name="inner">The inner chat client to delegate to.</param>
    /// <param name="events">The event hub to publish to.</param>
    /// <param name="agentName">The name of the agent using this client.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    public EventPublishingChatClient([NotNull] IChatClient inner, [NotNull] ISentinelCoreEvents events, [NotNull] string agentName, [NotNull] ILogger logger) : base(inner)
    {
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(agentName);
        ArgumentNullException.ThrowIfNull(logger);

        _events = events;
        _agentName = agentName;
        _logger = logger;
    }








    /// <summary>
    ///     General purpose event publishing method.
    ///     Publishes text output through the unified <see cref="ISentinelCoreEvents.SentinelOutputEvent" /> channel.
    /// </summary>
    /// <param name="text">The text to publish.</param>
    /// <param name="activityType">The category of activity being reported.</param>
    /// <param name="parms">Optional format parameters.</param>
    internal void PublishTextOutput(string text, ActivityType activityType = ActivityType.Core, object? parms = null)
    {
        string formatted = string.Format(text, parms);

        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        _logger.LogTrace("Publishing agent output for {AgentName}: {Length} chars", _agentName, text.Length);

        _events.RaiseSentinelOutputEvent(new SentinelOutputEventArgs(_agentName, text, activityType));
    }
}