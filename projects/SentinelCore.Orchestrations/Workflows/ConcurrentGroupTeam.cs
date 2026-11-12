using System.Runtime.CompilerServices;

using SentinelCore.CaseFlowEngine.Infrastructure;
using SentinelCore.Orchestrations.Abstractions;
using SentinelCore.Orchestrations.Agents.AgentPresets;
using SentinelCore.Orchestrations.Exceptions;

namespace SentinelCore.Orchestrations.Workflows;

/// <summary>
///     Orchestration that runs a team of analyst agents through a group chat workflow whose speakers are selected in
///     round-robin order by a <see cref="RoundRobinGroupChatManager" />.
/// </summary>
internal sealed class ConcurrentGroupTeam : IOrchestration
{
    private static readonly (string Name, string Description)[] AnalystProfiles =
    [
        ("AnalystAlpha", "Analyzes assigned evidence and reports findings to the team."),
        ("AnalystBravo", "Cross-checks findings and challenges unsupported conclusions."),
        ("AnalystCharlie", "Correlates findings across sources and summarizes the team's position.")
    ];

    private readonly ISentinelAgentFactory _agentFactory;
    private readonly List<AIAgent> _analysts = new(AnalystProfiles.Length);

    /// <summary>
    ///     Initializes a new instance of the <see cref="ConcurrentGroupTeam" /> class.
    /// </summary>
    /// <param name="agentFactory">The factory used to create the agents that participate in the group chat.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="agentFactory" /> is <see langword="null" />.</exception>
    internal ConcurrentGroupTeam(ISentinelAgentFactory agentFactory)
    {
        _agentFactory = agentFactory ?? throw new ArgumentNullException(nameof(agentFactory));
    }

    /// <summary>
    ///     Gets a human-readable explanation of what this orchestration does and when it is appropriate to use it.
    /// </summary>
    public string Description => "This orchestration is designed to handle concurrent investigations by a team of analysts. It allows for multiple analysts to work on different aspects of an investigation simultaneously, improving efficiency and response time.";

    /// <summary>
    ///     Gets the display name used to identify this orchestration to callers and in diagnostics.
    /// </summary>
    public string Name => "Concurrent Group Investigation Team";

    /// <summary>
    ///     Starts a streaming group chat run for the supplied input message and returns the workflow event stream.
    /// </summary>
    /// <param name="inputMessage">The user message that seeds the group chat.</param>
    /// <param name="token">A <see cref="CancellationToken" /> used to cancel the streaming run.</param>
    /// <returns>The asynchronous stream of <see cref="WorkflowEvent" /> instances produced by the workflow.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when <see cref="InitializeAsync" /> has not created the participating agents, or when the workflow
    ///     rejects the input message or turn token.
    /// </exception>
    /// <exception cref="SentinelCoreExecutionException">Thrown when the group chat run cannot be started.</exception>
    public async Task<IAsyncEnumerable<WorkflowEvent>?> ExecuteStreamingAsync(ChatMessage inputMessage, CancellationToken token)
    {
        Throw.IfNull(inputMessage);

        if (_analysts.Count == 0)
        {
            throw new InvalidOperationException($"{nameof(ConcurrentGroupTeam)} must be initialized by calling {nameof(InitializeAsync)} before execution.");
        }

        try
        {
            Workflow workflow = AgentWorkflowBuilder
                    .CreateGroupChatBuilderWith(agents => new RoundRobinGroupChatManager(agents))
                    .AddParticipants(_analysts)
                    .Build();

            StreamingRun run = await InProcessExecution.OpenStreamingAsync(workflow, cancellationToken: token).ConfigureAwait(false);

            if (!await run.TrySendMessageAsync(new List<ChatMessage> { inputMessage }).ConfigureAwait(false))
            {
                await run.DisposeAsync().ConfigureAwait(false);
                throw new InvalidOperationException("The group chat workflow rejected the input message.");
            }

            if (!await run.TrySendMessageAsync(new TurnToken(emitEvents: true)).ConfigureAwait(false))
            {
                await run.DisposeAsync().ConfigureAwait(false);
                throw new InvalidOperationException("The group chat workflow rejected the turn token.");
            }

            return StreamWorkflowEventsAsync(run, token);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new SentinelCoreExecutionException("Failed to start the concurrent group team workflow.", ex);
        }
    }

    /// <summary>
    ///     Creates the analyst agents that participate in the group chat. Repeated calls after the participants exist are
    ///     no-ops, so callers may invoke it before every execution.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous initialization operation.</returns>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_analysts.Count > 0)
        {
            return;
        }

        foreach ((string name, string description) in AnalystProfiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _analysts.Add(await _agentFactory.CreateAgentAsync(AgentPreset.WorkerAgent, name, description).ConfigureAwait(false));
        }
    }

    /// <summary>
    ///     Projects the streaming run into the workflow event stream and disposes the run when enumeration completes.
    /// </summary>
    /// <param name="run">The started streaming run.</param>
    /// <param name="token">A <see cref="CancellationToken" /> used to cancel enumeration.</param>
    /// <returns>The asynchronous stream of <see cref="WorkflowEvent" /> instances produced by the run.</returns>
    private static async IAsyncEnumerable<WorkflowEvent> StreamWorkflowEventsAsync(StreamingRun run, [EnumeratorCancellation] CancellationToken token)
    {
        await using (run)
        {
            await foreach (WorkflowEvent workflowEvent in run.WatchStreamAsync(blockOnPendingRequest: false, cancellationToken: token).ConfigureAwait(false))
            {
                yield return workflowEvent;
            }
        }
    }
}
