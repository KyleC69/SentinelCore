// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         TheCoreWorkflow.cs
// Author: Kyle L. Crowder
// Build Num:  092308



using SentinelCore.Abstractions;
using SentinelCore.Contracts.Abstractions;
using SentinelCore.Contracts.Events;
using SentinelCore.Orchestrations.Abstractions;
using SentinelCore.Orchestrations.Agents;
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
    private readonly object _initLock = new();
    private volatile bool _isInitialized;

    // Pre-created sub-workflow agents (initialized once via InitializeAsync)
    private AIAgent? _magManagerAgent;
    private AIAgent? _networkWorkerAgent;
    private AIAgent? _safetyAgent;

    // Pre-created agents and sessions (initialized once via InitializeAsync)
    private AIAgent? _sentinelCoreAgent;
    private AgentSession? _sentinelCoreSession;
    private AIAgent? _windowsOsWorkerAgent;

    // Unified workflow execution engine — all orchestration classes delegate
    // streaming execution here rather than running private StreamingRun loops.
    private readonly ISentinelWorkflowExecution _workflowExecution;








    /// <summary>
    ///     Initializes a new instance of the <see cref="TheCoreWorkflow" /> class.
    /// </summary>
    /// <param name="systemReporter">
    ///     An instance of <see cref="ISystemReporter" /> used for reporting system-level events or errors.
    /// </param>
    /// <param name="events">
    ///     An instance of <see cref="ISentinelCoreEvents" /> used to handle core events.
    /// </param>
    /// <param name="agentFactory">
    ///     An instance of <see cref="ISentinelAgentFactory" /> used to create agents for the workflow.
    /// </param>
    /// <param name="serviceProvider">
    ///     An instance of <see cref="IServiceProvider" /> used to resolve service dependencies.
    /// </param>
    /// <param name="workflowExecution">
    ///     An instance of <see cref="ISentinelWorkflowExecution" /> used to execute workflows.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if any of the provided parameters are <c>null</c>.
    /// </exception>
    public TheCoreWorkflow(ISystemReporter systemReporter, ISentinelCoreEvents events, ISentinelAgentFactory agentFactory, IServiceProvider serviceProvider, ISentinelWorkflowExecution workflowExecution) : base(systemReporter)
    {
        Throw.IfNull(systemReporter);
        Throw.IfNull(events);
        Throw.IfNull(agentFactory);
        Throw.IfNull(serviceProvider);
        Throw.IfNull(workflowExecution);

        _agentFactory = agentFactory;
        _events = events;
        _workflowExecution = workflowExecution;
        _executorFactory = new ExecutorFactory(serviceProvider);
    }








    /// <summary>
    ///     Builds and returns the workflow for the current orchestration.
    /// </summary>
    /// <returns>
    ///     A <see cref="Workflow" /> instance representing the constructed workflow.
    /// </returns>
    /// <remarks>
    ///     This method is designed to define and construct the workflow logic specific to this orchestration.
    ///     The method is synchronous internally but returns <see cref="Task{Workflow}" /> to satisfy the
    ///     <see cref="IOrchestration" /> interface contract.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the workflow cannot be built because agents have not been initialized.
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

        // SentinelCore agent with session
        DirectAnswerExecutor directAnswerAgentExec = new(_sentinelCoreAgent, _sentinelCoreSession, _reporter);






        // ── Compose the switch-based routing graph ─────────────────────────────────────────

        WorkflowBuilder builder = new(executors.PatternCheckExecutor);

        // --- Pre-Agent ---------
        builder.AddEdge(executors.PatternCheckExecutor, executors.SafetyExecutor);
        builder.AddEdge(executors.SafetyExecutor, classifierExec);
        builder.AddSwitch(classifierExec, switchBuilder => switchBuilder.AddCase(GetCondition(NextStep.Investigate), executors.NewCaseExecutor).AddCase(GetCondition(NextStep.DirectAnswer), directAnswerAgentExec).AddCase(GetCondition(NextStep.RedAlert), executors.CriticalAlert).AddCase(GetCondition(NextStep.MoreInformationRequired), executors.MoreInformationExecutor).AddCase(GetCondition(NextStep.EscalateToHumanOperator), executors.EscalatedExecutor).WithDefault(executors.HumanOperatorExecutor));

        // ------- RedAlert Branch -------------------------
        // Alert UI to critical error and mark case urgent
        builder.AddEdge(executors.CriticalAlert, executors.HumanOperatorExecutor)

                // -- ------- MoreInformation Branch -------------------------
                //Mark case needs more info and alert operator
                .AddEdge(executors.MoreInformationExecutor, executors.HumanOperatorExecutor)

                // --------- EscalateToHumanOperator Branch -------------------------
                // Need to justify branch
                .AddEdge(executors.EscalatedExecutor, executors.HumanOperatorExecutor)

                // -- ------- DirectAnswer Branch -------------------------
                .AddEdge(directAnswerAgentExec, executors.TerminateWorkflow)

                // -- ------- Investigate Branch -------------------------
                //Open case send to TheCore
                .AddEdge(executors.NewCaseExecutor, sentinelCoreExec)
                .AddEdge(sentinelCoreExec, evidenceGatheringBinding) // Sub-workflow for evidence gathering
                .AddEdge(evidenceGatheringBinding, executors.AggregationExecutor)
                .AddEdge(executors.AggregationExecutor, executors.PersistEvidenceExecutor) // Evidence gathered — hand the synthesized results to a human for review
                .AddEdge(executors.PersistEvidenceExecutor, sentinelCoreExec)
                .AddEdge(sentinelCoreExec, executors.TerminateWorkflow)
                .WithName("TheCoreFlow")
                .WithDescription("The main investigation and case management workflow");

        // ── Register output sources ─────────────────────────────────────────────────────────
        // MAF only surfaces yielded values from executors registered via WithOutputFrom;
        // without this registration no executor output ever reaches the WorkflowOutputEvent stream.
        builder.WithOutputFrom(classifierExec, directAnswerAgentExec, sentinelCoreExec, executors.NewCaseExecutor, executors.CriticalAlert, executors.MoreInformationExecutor, executors.EscalatedExecutor, executors.TerminateWorkflow, executors.HumanOperatorExecutor, executors.AggregationExecutor, executors.PersistEvidenceExecutor);

        Workflow flow = builder.Build();

        VisualizeWorkflowAsync(flow, CancellationToken.None).Wait();
        return Task.FromResult(flow);

    }








    /// <summary>
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
            Workflow workflow = await BuildWorkflow().ConfigureAwait(false);
            ValidateWorkflow(workflow);

            // Delegate streaming execution to the unified engine. The raw-event
            // callback preserves this orchestration's per-event handling (streaming
            // accumulation via WorkflowBase and real-time UI publishing) while the
            // engine owns the run loop, structured logging, and Magentic events.
            WorkflowExecutionResult result = await _workflowExecution.ExecuteAsync(workflow, message, Name, evt =>
                    {
                        this.ProcessEvent(evt);
                        PublishIntermediateEvent(evt);
                    }, cancellationToken)
                    .ConfigureAwait(false);

            return result.HasOutput ? result : null;
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
    ///     Idempotent: repeated calls after a successful initialization return immediately
    ///     without rebuilding agents, so callers (e.g. <see cref="OrchestrationControl" />)
    ///     may safely invoke this before every execution.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitialized)
        {
            // Already initialized — nothing to do. This makes the method idempotent
            // so per-send callers do not have to track initialization state themselves.
            return;
        }

        lock (_initLock)
        {
            if (_isInitialized)
            {
                // Double-checked locking: another thread completed initialization
                // while we waited for the lock.
                return;
            }
        }

        // ── Build main workflow agents ─────────────────────────────────────────────────────────

        _sentinelCoreAgent = await _agentFactory.CreateAgentAsync("TheCore", cancellationToken);

        _classifierAgent = await _agentFactory.CreateAgentAsync("Classifier", cancellationToken);

        _safetyAgent = await _agentFactory.CreateAgentAsync("SafetyAgent", cancellationToken);

        _sentinelCoreSession = await _sentinelCoreAgent.CreateSessionAsync(cancellationToken);

        // ── Build MAG sub-workflow agents ─────────────────────────────────────────────────────

        _magManagerAgent = await _agentFactory.CreateAgentAsync("Manager");

        _windowsOsWorkerAgent = await _agentFactory.CreateAgentAsync("Worker1", cancellationToken);

        _networkWorkerAgent = await _agentFactory.CreateAgentAsync("Worker2", cancellationToken);

        _applicationWorkerAgent = await _agentFactory.CreateAgentAsync("Worker3", cancellationToken);

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








    /// <summary>
    ///     Reports a workflow execution exception through the system reporter.
    /// </summary>
    /// <param name="ex">The exception that occurred during execution.</param>
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
    private void PublishIntermediateEvent(WorkflowEvent evt)
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
        if (id.Contains("manager", StringComparison.Ordinal) || id.Contains("evidence", StringComparison.Ordinal))
        {
            return ActivityType.Manager;
        }

        if (id.Contains("safety", StringComparison.Ordinal))
        {
            return ActivityType.Tooling;
        }

        if (id.Contains("classifier", StringComparison.Ordinal) || id.Contains("worker", StringComparison.Ordinal))
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
        if (workflow is null)
        {
            throw new InvalidOperationException("Workflow could not be built.");
        }
    }








    /// <summary>
    ///     Creates Graphviz representations of the workflow and saves them to files.
    /// </summary>
    /// <param name="workflow">The workflow to visualize.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    private async Task VisualizeWorkflowAsync(Workflow workflow, CancellationToken cancellationToken)
    {
        string flow = workflow.ToDotString();
        await File.WriteAllTextAsync("workflow.dot", flow, cancellationToken).ConfigureAwait(false);
    }
}