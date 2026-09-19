// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         TheCoreWorkflow.cs
// Author: Kyle L. Crowder
// Build Num:  091419



using System.Text.Json;

using Microsoft.Extensions.Logging;

using SentinelCore.Abstractions;
using SentinelCore.Contracts.Abstractions;
using SentinelCore.Contracts.Events;
using SentinelCore.Orchestrations.Abstractions;
using SentinelCore.Orchestrations.Agents;
using SentinelCore.Orchestrations.Agents.Models;
using SentinelCore.Orchestrations.Application;
using SentinelCore.Orchestrations.Exceptions;
using SentinelCore.Orchestrations.Workflows.Executors;




namespace SentinelCore.Orchestrations.Workflows;





/// <summary>
///     Orchestrates the core multi-agent workflow that classifies incoming signals and routes them to appropriate
///     executors, coordinating agents, safety checks, evidence gathering, and escalation.
/// </summary>
/// <remarks>
///     Sealed orchestration that composes agent and non-agent executors, builds a MAG-based
///     evidence-gathering sub-workflow, and visualizes the workflow graph. Use BuildWorkflow asynchronously to construct
///     the Workflow and ExecuteAsync to run it with a ChatMessage; execution produces WorkflowEvent entries and may return
///     a WorkflowExecutionResult. Constructor enforces required dependencies via constructor injection and throws on null
///     arguments.
/// </remarks>
public sealed class TheCoreWorkflow : WorkflowBase, IOrchestration
{
    private readonly ISentinelAgentFactory _agentFactory;
    private AIAgent? _applicationWorkerAgent;
    private AIAgent? _classifierAgent;
    private readonly ISentinelCoreEvents _events;
    private readonly ExecutorFactory _executorFactory;

    private bool _isInitialized;

    private readonly ILoggerFactory _loggerFactory;

    // Pre-created sub-workflow agents (initialized once via InitializeAsync)
    private AIAgent? _magManagerAgent;
    private AIAgent? _networkWorkerAgent;
    private AIAgent? _safetyAgent;

    // Pre-created agents and sessions (initialized once via InitializeAsync)
    private AIAgent? _sentinelCoreAgent;
    private AgentSession? _sentinelCoreSession;
    private readonly IServiceProvider _serviceProvider;
    private AIAgent? _windowsOsWorkerAgent;
    private AIAgent _directAnswerAgent;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true, PropertyNameCaseInsensitive = true };
    public sealed record WorkflowFinished(string Reason, List<ChatMessage> OutputMessages, WorkflowOutputEvent? OutputEvent);







    /// <summary>
    ///     Initializes a new instance of the <see cref="TheCoreWorkflow" /> class.
    /// </summary>
    /// <param name="agentSpecBuilder">
    ///     An instance of <see cref="IAgentProfileBuilder" /> used to build agent profiles.
    /// </param>
    /// <param name="systemReporter">
    ///     An instance of <see cref="ISystemReporter" /> used for reporting system-level events or errors.
    /// </param>
    /// <param name="events">
    ///     An instance of <see cref="ISentinelCoreEvents" /> used to handle core events.
    /// </param>
    /// <param name="agentFactory">
    ///     An instance of <see cref="ISentinelAgentFactory" /> used to create agents for the workflow.
    /// </param>
    /// <param name="loggerFactory">
    ///     An instance of <see cref="ILoggerFactory" /> used to create loggers.
    /// </param>
    /// <param name="serviceProvider">
    ///     An instance of <see cref="IServiceProvider" /> used to resolve service dependencies.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if any of the provided parameters are <c>null</c>.
    /// </exception>
    public TheCoreWorkflow(IAgentProfileBuilder agentSpecBuilder, ISystemReporter systemReporter, ISentinelCoreEvents events, ISentinelAgentFactory agentFactory, ILoggerFactory loggerFactory, IServiceProvider serviceProvider) : base(systemReporter)
    {
        Throw.IfNull(agentSpecBuilder);
        Throw.IfNull(systemReporter);
        Throw.IfNull(events);
        Throw.IfNull(agentFactory);
        Throw.IfNull(loggerFactory);
        Throw.IfNull(serviceProvider);

        _agentFactory = agentFactory;
        _events = events;
        _loggerFactory = loggerFactory;
        _serviceProvider = serviceProvider;
        _executorFactory = new ExecutorFactory(serviceProvider, loggerFactory);
    }








    /// <summary>
    ///     Asynchronously builds and returns the workflow for the current orchestration.
    /// </summary>
    /// <returns>
    ///     A <see cref="Workflow" /> instance representing the constructed workflow.
    /// </returns>
    /// <remarks>
    ///     This method is designed to define and construct the workflow logic specific to this orchestration.
    /// </remarks>
    /// <exception cref="Exception">
    ///     An exception may be thrown if the workflow construction fails.
    /// </exception>
    public Task<Workflow> BuildWorkflow()
    {
        Throw.IfNull(_sentinelCoreAgent);
        Throw.IfNull(_classifierAgent);
        Throw.IfNull(_safetyAgent);
        Throw.IfNull(_sentinelCoreSession);
        Throw.IfNull(_magManagerAgent);
        Throw.IfNull(_windowsOsWorkerAgent);
        Throw.IfNull(_networkWorkerAgent);
        Throw.IfNull(_applicationWorkerAgent);

        // ── Construct NON-Agent executors ────────────────────────────────────────────────────

        ExecutorCollection executors = _executorFactory.CreateAllExecutors();

        // ── Build sub-workflow ──────────────────────────────────────────────────────────────────

        Workflow evidenceGatheringWorkflow = BuildEvidenceGatheringSubWorkflow();
        ExecutorBinding evidenceGatheringBinding = evidenceGatheringWorkflow.BindAsExecutor("EvidenceCollection");

        ExecutorBinding safetyAgentBinding = _safetyAgent.BindAsExecutor();

        // ── Create agent executors ─────────────────────────────────────────────────────────────

        ClassifierAgentExec classifierExec = new(_classifierAgent, _reporter);
        TheCoreExec sentinelCoreExec = new(_sentinelCoreAgent, _sentinelCoreSession, _reporter);
        DirectAnswerExecutor directAnswerAgentExec = new(_sentinelCoreAgent, _reporter);






        // ── Compose the switch-based routing graph ─────────────────────────────────────────

        WorkflowBuilder builder = new(executors.PatternCheckExecutor);

        // --- Pre-Agent ---------
        builder.AddEdge(executors.PatternCheckExecutor, executors.SafetyExecutor);
        builder.AddEdge(executors.SafetyExecutor, classifierExec);
        builder.AddSwitch(classifierExec, switchBuilder => switchBuilder.AddCase(GetCondition(NextStep.Investigate), executors.NewCaseExecutor)
                        .AddCase(GetCondition(NextStep.RedAlert), executors.CriticalAlert)
                        .AddCase(GetCondition(NextStep.MoreInformationRequired), executors.MoreInformationExecutor)
                        .AddCase(GetCondition(NextStep.EscalateToHumanOperator), executors.EscalatedExecutor)
                        .AddCase(GetCondition(NextStep.DirectAnswer), directAnswerAgentExec)
                        .WithDefault(executors.HumanOperatorExecutor));

        // ------- RedAlert Branch -------------------------
        builder.AddEdge(executors.CriticalAlert, executors.HumanOperatorExecutor)

                // -- ------- MoreInformation Branch -------------------------
                .AddEdge(executors.MoreInformationExecutor, executors.HumanOperatorExecutor)

                // --------- EscalateToHumanOperator Branch -------------------------
                .AddEdge(executors.EscalatedExecutor, executors.HumanOperatorExecutor)
                // -- ------- DirectAnswer Branch -------------------------

                // -- ------- Investigate Branch -------------------------
                .AddEdge(executors.NewCaseExecutor, sentinelCoreExec)
                .AddEdge(sentinelCoreExec, evidenceGatheringBinding)  // Sub-workflow for evidence gathering
                .AddEdge(evidenceGatheringBinding, executors.AggregationExecutor)
                .AddEdge(executors.AggregationExecutor, sentinelCoreExec)   // Loop back to TheCoreExec for re-evaluation after evidence gathering for final report generation
                .WithName("TheCoreFlow")
                .WithDescription("The main investigation and case management workflow");

        Workflow flow = builder.Build();
        return Task.FromResult(flow);


    }








    /// <summary>
    ///
    /// </summary>
    public string Description { get; } = """
                                         TheCore is a multi-agent and non-agent workflow that starts with a set of safeties, classifies an incoming signal and routes it
                                         to the appropriate executor based on the classification result. It demonstrates a structured approach
                                         to handling various scenarios, including investigation, direct answers, safety concerns, and escalation
                                         to human operators. The workflow is designed to ensure that each step is executed by the appropriate
                                         agent or executor, providing a clear and efficient process for managing complex tasks.
                                         """;








    /// <summary>
    ///     Executes the core workflow asynchronously.
    /// </summary>
    /// <param name="message">The input message for the workflow, represented as a <see cref="ChatMessage" />.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task representing the asynchronous operation, which upon completion provides a
    ///     <see cref="WorkflowExecutionResult" />.
    /// </returns>
    public async Task<WorkflowExecutionResult?> ExecuteAsync(ChatMessage message, CancellationToken cancellationToken)
    {
        Throw.IfNull(message);
        this.ResetEventAccumulators();
        try
        {
            Workflow workflow = await BuildWorkflowAsync().ConfigureAwait(false);
            ValidateWorkflow(workflow);
            return await ExecuteStreamingAsync(workflow, message, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            HandleExecutionException(ex);
            throw new SentinelCoreExecutionException("Failed to execute the workflow.", ex);
        }
    }








    /// <summary>
    ///     Initializes agents and other resources required for workflow execution.
    ///     Should be called once before any calls to <see cref="ExecuteAsync" />.
    ///     //TODO: Consider making this method idempotent or handling re-initialization gracefully. Run on startup of the
    ///     application or orchestration service.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="InvalidOperationException">Thrown if initialization has already completed.</exception>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitialized)
        {
            throw new InvalidOperationException($"'{nameof(TheCoreWorkflow)}' has already been initialized.");
        }

        // ── Build main workflow agents ─────────────────────────────────────────────────────────

        _sentinelCoreAgent = await _agentFactory.CreateAgentAsync("TheCore", null, cancellationToken, ChatResponseFormat.ForJsonSchema(AIJsonUtilities.CreateJsonSchema(typeof(CoreDirective))));

        _classifierAgent = await _agentFactory.CreateAgentAsync("Classifier", null, cancellationToken, ChatResponseFormat.ForJsonSchema(AIJsonUtilities.CreateJsonSchema(typeof(SignalHypothesis))));
        _directAnswerAgent = await _agentFactory.CreateAgentAsync("Classifier", null, cancellationToken, ChatResponseFormat.Text);

        _safetyAgent = await _agentFactory.CreateAgentAsync("SafetyAgent", null, cancellationToken);

        _sentinelCoreSession = await _sentinelCoreAgent.CreateSessionAsync(cancellationToken);

        // ── Build MAG sub-workflow agents ─────────────────────────────────────────────────────

        _magManagerAgent = await _agentFactory.CreateAgentAsync("Manager", null, cancellationToken, ChatResponseFormat.ForJsonSchema(AIJsonUtilities.CreateJsonSchema(typeof(InvestigationStep))));

        _windowsOsWorkerAgent = await _agentFactory.CreateAgentAsync("Worker1", null, cancellationToken);

        _networkWorkerAgent = await _agentFactory.CreateAgentAsync("Worker2", null, cancellationToken);

        _applicationWorkerAgent = await _agentFactory.CreateAgentAsync("Worker3", null, cancellationToken);

        _reporter.ReportInfo("Agent profiles constructed.");
        _isInitialized = true;
    }








    /// <summary>
    ///     Gets the name of the workflow.
    /// </summary>
    /// <value>
    ///     A string representing the name of the workflow. For this implementation, it is set to the name of the class,
    ///     <see cref="TheCoreWorkflow" />.
    /// </value>
    /// <remarks>
    ///     The <c>Name</c> property provides a consistent identifier for the workflow, which can be used for logging,
    ///     debugging, or orchestration purposes.
    /// </remarks>
    public string Name { get; } = nameof(TheCoreWorkflow);








    /// <summary>
    ///     Builds the evidence gathering sub-workflow for the MAG (Multi-Agent Group).
    /// </summary>
    /// <returns>The composed investigation <see cref="Workflow" />.</returns>
    private Workflow BuildEvidenceGatheringSubWorkflow()
    {
        Throw.IfNull(_magManagerAgent);
        Throw.IfNull(_windowsOsWorkerAgent);
        Throw.IfNull(_networkWorkerAgent);
        Throw.IfNull(_applicationWorkerAgent);

        Workflow subWorkflow = new MagenticWorkflowBuilder(_magManagerAgent).AddParticipants(_windowsOsWorkerAgent, _networkWorkerAgent, _applicationWorkerAgent).WithMaxResets(3).WithMaxRounds(3).WithMaxStalls(2).RequirePlanSignoff(false).WithDescription("MAG sub-workflow for collecting evidence to support TheCore\'s hypothesis of the signal").WithName("EvidenceCollection").Build();

        return subWorkflow;
    }








    private async Task<Workflow> BuildWorkflowAsync()
    {
        Workflow? workflow = await BuildWorkflow().ConfigureAwait(false);
        return workflow ?? throw new InvalidOperationException("Workflow could not be built.");
    }





    /// <summary>
    /// Runs the specified workflow in streaming mode, processes events as they arrive, and collects chat output messages.
    /// </summary>
    /// <remarks>Streams workflow events, processes and publishes intermediate events, and stops early if cancellation
    /// is requested.</remarks>
    /// <param name="workflow">The workflow to execute.</param>
    /// <param name="message">The initial chat message to send to the workflow.</param>
    /// <param name="cancellationToken">Cancellation token to observe while streaming.</param>
    /// <returns>A WorkflowExecutionResult containing the collected chat messages and a final WorkflowOutputEvent, or null if the
    /// workflow produced no messages.</returns>
    private async Task<WorkflowExecutionResult?> ExecuteStreamingAsync(Workflow workflow, ChatMessage message, CancellationToken cancellationToken)
    {
        List<ChatMessage>? outputMessages = null;

        await using StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, message, cancellationToken: cancellationToken).ConfigureAwait(false);
        await run.TrySendMessageAsync(new TurnToken(true)).ConfigureAwait(false);

        await foreach (WorkflowEvent evt in run.WatchStreamAsync(cancellationToken).ConfigureAwait(false))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            this.ProcessEvent(evt);
            this.PublishIntermediateEventAsync(evt);

            if (evt is WorkflowOutputEvent outputEvt && outputEvt.Is<List<ChatMessage>>())
            {
                outputMessages = outputEvt.As<List<ChatMessage>>();
            }
        }

        return outputMessages is { Count: > 0 }
            ? new WorkflowExecutionResult(outputMessages, new WorkflowOutputEvent(outputMessages, "TheCoreExecution"))
            : null;
    }








    /// <summary>
    ///     Creates a condition function that evaluates whether the provided detection result
    ///     matches the expected next step decision.
    /// </summary>
    /// <param name="expectedDecision">The expected next step decision.</param>
    /// <returns>A function that evaluates whether a message meets the expected result.</returns>
    private static Func<object?, bool> GetCondition(NextStep expectedDecision)
    {
        return detectionResult => detectionResult is SignalHypothesis result && result.NextStep == expectedDecision;
    }








    private void HandleExecutionException(Exception ex)
    {
        _reporter.ReportError("An exception occurred during workflow execution.", ex);
    }








    /// <summary>
    ///     Publishes an intermediate event to the UI hub so streaming agent output reaches the client in real time.
    /// </summary>
    /// <param name="evt">The <see cref="WorkflowEvent" /> to forward.</param>
    /// <remarks>
    ///     Streaming token updates (<see cref="AgentResponseUpdateEvent" />) and completed agent responses
    ///     (<see cref="AgentResponseEvent" />) are surfaced with an <see cref="ActivityType" /> derived from the
    ///     producing executor. Lifecycle and output events are logged by <see cref="WorkflowBase.ProcessEvent" />.
    /// </remarks>
    private void PublishIntermediateEventAsync(WorkflowEvent evt)
    {
        switch (evt)
        {
            case AgentResponseUpdateEvent updateEvent:
                _events.RaiseSentinelOutputEvent(new SentinelOutputEventArgs(updateEvent.ExecutorId, updateEvent.Update.Text, ResolveActivityType(updateEvent.ExecutorId)));
                break;
            case AgentResponseEvent responseEvent:
                _events.RaiseSentinelOutputEvent(new SentinelOutputEventArgs(responseEvent.ExecutorId, responseEvent.Response.Text, ResolveActivityType(responseEvent.ExecutorId)));
                break;
            case ExecutorInvokedEvent invokedEvent:
                _events.RaiseSentinelOutputEvent(new SentinelOutputEventArgs(invokedEvent.ExecutorId, $"Executor '{invokedEvent.ExecutorId}' invoked.", ActivityType.Orchestration));
                break;
            case ExecutorCompletedEvent completedEvent:
                _events.RaiseSentinelOutputEvent(new SentinelOutputEventArgs(completedEvent.ExecutorId, $"Executor '{completedEvent.ExecutorId}' completed.", ActivityType.Orchestration));
                break;
        }
    }

    /// <summary>
    ///     Resolves an <see cref="ActivityType" /> for a workflow event based on the executor that produced it.
    /// </summary>
    /// <param name="executorId">The identifier of the executor that produced the event.</param>
    /// <returns>
    ///     The mapped <see cref="ActivityType" />, falling back to <see cref="ActivityType.System" /> when the executor
    ///     cannot be classified.
    /// </returns>
    private static ActivityType ResolveActivityType(string? executorId)
    {
        if (string.IsNullOrWhiteSpace(executorId))
        {
            return ActivityType.System;
        }

        string id = executorId.ToLowerInvariant();
        if (id.Contains("manager", StringComparison.OrdinalIgnoreCase) || id.Contains("evidence", StringComparison.OrdinalIgnoreCase))
        {
            return ActivityType.Manager;
        }

        if (id.Contains("safety", StringComparison.OrdinalIgnoreCase))
        {
            return ActivityType.Tooling;
        }

        if (id.Contains("classifier", StringComparison.OrdinalIgnoreCase) || id.Contains("worker", StringComparison.OrdinalIgnoreCase))
        {
            return ActivityType.Participant;
        }

        return ActivityType.Core;
    }








    /// <summary>
    ///     Validates that the provided Workflow instance is not null.
    /// </summary>
    /// <param name="workflow">The Workflow to validate.</param>
    /// <exception cref="InvalidOperationException">Thrown when the workflow is null and cannot be built.</exception>
    private static void ValidateWorkflow(Workflow workflow)
    {
        if (workflow == null)
        {
            throw new InvalidOperationException("Workflow could not be built.");
        }
    }








    /// <summary>
    ///     Creates Graphviz representations of the workflow and saves them to files.
    /// </summary>
    /// <param name="workflow">The workflow to visualize.</param>
    private void VisualizeWorkflow(Workflow workflow)
    {
        string flow = workflow.ToDotString();
        Console.WriteLine(flow);

        // Use async file operations instead of blocking I/O
        Task.Run(async () =>
        {
            await File.WriteAllTextAsync("workflow.dot", flow);
            await File.WriteAllTextAsync("workflow.mermaid", flow);
        }, CancellationToken.None);
    }
}





