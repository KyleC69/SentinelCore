// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         EventPublishingChatClient.cs
// Author: Kyle L. Crowder
// Build Num:  082808



using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

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
    ///     Forwards the request to the inner client, then publishes the response's tool
    ///     results and agent text output through the unified
    ///     <see cref="ISentinelCoreEvents.SentinelOutputEvent" /> channel and the <see cref="ILogger" />.
    /// </summary>
    public override async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        _logger.LogTrace("Agent {AgentName} sending request with {MessageCount} messages", _agentName, messages.Count());

        ChatResponse response = await base.GetResponseAsync(messages, options, cancellationToken).ConfigureAwait(false);

        foreach (FunctionResultContent toolResult in response.Messages.SelectMany(m => m.Contents).OfType<FunctionResultContent>())
        {
            PublishToolResult(toolResult);
        }

        foreach (TextContent textContent in response.Messages.SelectMany(m => m.Contents).OfType<TextContent>())
        {
            PublishTextOutput(textContent.Text);
        }

        return response;
    }







    /// <summary>
    ///     Streams the inner client's updates unchanged while accumulating agent text.
    ///     When the stream completes, the accumulated text and any tool results are
    ///     published to <see cref="ISentinelCoreEvents.SentinelOutputEvent" /> and the <see cref="ILogger" />.
    /// </summary>
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        StringBuilder textAccumulator = new();

        await foreach (ChatResponseUpdate update in base.GetStreamingResponseAsync(messages, options, cancellationToken).ConfigureAwait(false))
        {
            foreach (FunctionResultContent toolResult in update.Contents.OfType<FunctionResultContent>())
            {
                PublishToolResult(toolResult);
            }

            foreach (TextContent textContent in update.Contents.OfType<TextContent>())
            {
                if (!string.IsNullOrWhiteSpace(textContent.Text))
                {
                    textAccumulator.Append(textContent.Text);
                }
            }

            yield return update;
        }

        if (textAccumulator.Length > 0)
        {
            PublishTextOutput(textAccumulator.ToString());
        }
    }







    /// <summary>
    ///     Logs and publishes a tool/function-call result produced by the agent.
    /// </summary>
    /// <param name="toolResult">The tool result content captured from the response.</param>
    private void PublishToolResult(FunctionResultContent toolResult)
    {
        if (toolResult.Exception is not null)
        {
            _logger.LogWarning(toolResult.Exception, "Agent {AgentName} tool call {CallId} failed", _agentName, toolResult.CallId ?? "unknown");
            _events.RaiseSentinelOutputEvent(new SentinelOutputEventArgs(_agentName, $"Tool call {toolResult.CallId} failed: {toolResult.Exception.Message}", ActivityType.Tooling));
            return;
        }

        string resultText = toolResult.Result?.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(resultText))
        {
            return;
        }

        _logger.LogTrace("Agent {AgentName} tool call {CallId} returned {Length} chars", _agentName, toolResult.CallId ?? "unknown", resultText.Length);

        _events.RaiseSentinelOutputEvent(new SentinelOutputEventArgs(_agentName, resultText, ActivityType.Tooling));
    }







    /// <summary>
    ///     Publishes text output through the unified <see cref="ISentinelCoreEvents.SentinelOutputEvent" /> channel
    ///     and emits a trace entry to the <see cref="ILogger" />.
    /// </summary>
    /// <param name="text">The text to publish.</param>
    /// <param name="activityType">The category of activity being reported.</param>
    /// <param name="parms">Optional format parameters.</param>
    internal void PublishTextOutput(string? text, ActivityType activityType = ActivityType.Core, object? parms = null)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        // Only format when parameters were supplied — model output routinely contains
        // braces that would otherwise throw FormatException inside string.Format.
        string payload = parms is null ? text : string.Format(text, parms);

        _logger.LogTrace("Publishing agent output for {AgentName}: {Length} chars", _agentName, payload.Length);

        _events.RaiseSentinelOutputEvent(new SentinelOutputEventArgs(_agentName, payload, activityType));
    }
}