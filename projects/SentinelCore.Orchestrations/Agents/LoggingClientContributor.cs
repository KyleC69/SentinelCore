// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         LoggingClientContributor.cs
// Author: Kyle L. Crowder
// Build Num:  092308



using Microsoft.Extensions.Logging;

using SentinelCore.Orchestrations.Agents.AgentPresets;




namespace SentinelCore.Orchestrations.Agents;





/// <summary>
///     Wraps the chat client with a logging decorator when the preset's
///     <see cref="AgentPresetDefinition.UseLogging" /> flag is set.
///     <para>
///         Event publishing is handled at the workflow layer via
///         <see cref="ISentinelCoreEvents.RaiseSentinelOutputEvent" />,
///         not by a chat-client decorator.
///     </para>
/// </summary>
public sealed class LoggingClientContributor : IAgentConstructionContributor
{
    private readonly ILoggerFactory _loggerFactory;








    /// <summary>
    ///     Initializes a new instance of the <see cref="LoggingClientContributor" /> class.
    /// </summary>
    /// <param name="loggerFactory">The logger factory for creating loggers.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="loggerFactory" /> is null.</exception>
    public LoggingClientContributor(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }








    /// <inheritdoc />
    public Task ContributeAsync(AgentConstructionContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Preset.UseLogging)
        {
            context.WrappedClient = new LoggingChatClient(context.WrappedClient, _loggerFactory.CreateLogger("InnerClientLogger"));
        }

        return Task.CompletedTask;
    }








    /// <inheritdoc />
    public int Order
    {
        get => 20;
    }
}