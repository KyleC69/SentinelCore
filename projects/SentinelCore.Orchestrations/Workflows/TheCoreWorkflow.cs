// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         TheCoreWorkflow.cs
// Author: Kyle L. Crowder
// Build Num:  091300



using Microsoft.Extensions.Logging;

using SentinelCore.Abstractions;
using SentinelCore.Contracts.Abstractions;
using SentinelCore.Contracts.Events;
using SentinelCore.Orchestrations.Abstractions;
using SentinelCore.Orchestrations.Agents;
using SentinelCore.Orchestrations.Agents.Models;
using SentinelCore.Orchestrations.Application;
using SentinelCore.Orchestrations.Workflows.Executors;

using System.Text.Json;




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

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true, PropertyNameCaseInsensitive = true };








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

        ClassifierAgentExec classifierExec = new(_classifierAgent);
        TheCoreExec sentinelCoreExec = new(_sentinelCoreAgent, _sentinelCoreSession, _reporter);

        // ── Compose the switch-based routing graph ─────────────────────────────────────────

        WorkflowBuilder builder = new(executors.PatternCheckExecutor);

        builder.AddEdge(executors.PatternCheckExecutor, executors.SafetyExecutor);
        builder.AddEdge(executors.SafetyExecutor, classifierExec);

        builder.AddSwitch(classifierExec, switchBuilder => switchBuilder.AddCase(GetCondition(NextStep.Investigate), executors.NewCaseExecutor).AddCase(GetCondition(NextStep.RedAlert), executors.CriticalAlert).AddCase(GetCondition(NextStep.MoreInformationRequired), executors.MoreInformationExecutor).AddCase(GetCondition(NextStep.EscalateToHumanOperator), executors.HumanOperatorExecutor).AddCase(GetCondition(NextStep.DirectAnswer), executors.DirectAnswerExecutor).WithDefault(executors.NewCaseExecutor));

        builder.AddEdge(executors.CriticalAlert, executors.HumanOperatorExecutor).AddEdge(executors.MoreInformationExecutor, executors.HumanOperatorExecutor).AddEdge(executors.NewCaseExecutor, sentinelCoreExec).AddEdge(sentinelCoreExec, evidenceGatheringBinding).AddEdge(evidenceGatheringBinding, executors.AggregationExecutor).AddEdge(executors.AggregationExecutor, sentinelCoreExec).WithName("TheCoreFlow").WithDescription("The main investigation and case management workflow");

        Workflow flow = builder.Build();

        VisualizeWorkflow(flow);

        return Task.FromResult(flow);
    }








    /// <inheritdoc />
    public string Description { get; } = """
                                         TheCore is a multi-agent and non-agent workflow that classifies an incoming signal and routes it
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
    /// <remarks>
    ///     This method builds the workflow, executes it in a streaming manner, and processes events emitted during execution.
    /// </remarks>
    public async Task<WorkflowExecutionResult?> ExecuteAsync(ChatMessage message, CancellationToken cancellationToken)
    {
        Throw.IfNull(message);

        this.ResetEventAccumulators();

        Workflow workflow = await BuildWorkflow().ConfigureAwait(false);

        Run result = await InProcessExecution.RunAsync(workflow, message, cancellationToken: cancellationToken).ConfigureAwait(false);

        List<ChatMessage>? outputMessages = null;
        foreach (WorkflowEvent evt in result.NewEvents)
        {
            this.ProcessEvent(evt);

            if (evt is WorkflowOutputEvent outputEvt && outputEvt.Is<List<ChatMessage>>())
            {
                outputMessages = outputEvt.As<List<ChatMessage>>();
            }
        }

        return outputMessages is not null ? new WorkflowExecutionResult(outputMessages, eventLog: []) : null;
    }








    /// <summary>
    ///     Initializes agents and other resources required for workflow execution.
    ///     Should be called once before any calls to <see cref="ExecuteAsync" />.
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

        _sentinelCoreAgent = await _agentFactory.CreateAgentAsync("TheCore", AgentInstructionConstants.SentinelCoreInstructions, cancellationToken, ChatResponseFormat.ForJsonSchema(AIJsonUtilities.CreateJsonSchema(typeof(CoreDirective))));

        _classifierAgent = await _agentFactory.CreateAgentAsync("Classifier", AgentInstructionConstants.ClassifierInstructions, cancellationToken, ChatResponseFormat.ForJsonSchema(AIJsonUtilities.CreateJsonSchema(typeof(SignalHypothesis))));

        _safetyAgent = await _agentFactory.CreateAgentAsync("SafetyAgent", AgentInstructionConstants.SafetyAgentInstructions, cancellationToken);

        _sentinelCoreSession = await _sentinelCoreAgent.CreateSessionAsync(cancellationToken);

        // ── Build MAG sub-workflow agents ─────────────────────────────────────────────────────

        _magManagerAgent = await _agentFactory.CreateAgentAsync("Manager", AgentInstructionConstants.MagManagerInstructions, cancellationToken, ChatResponseFormat.ForJsonSchema(AIJsonUtilities.CreateJsonSchema(typeof(InvestigationStep))));

        _windowsOsWorkerAgent = await _agentFactory.CreateAgentAsync("Worker1", AgentInstructionConstants.WorkerBaseInstructions, cancellationToken);

        _networkWorkerAgent = await _agentFactory.CreateAgentAsync("Worker2", AgentInstructionConstants.WorkerBaseInstructions, cancellationToken);

        _applicationWorkerAgent = await _agentFactory.CreateAgentAsync("Worker3", AgentInstructionConstants.WorkerBaseInstructions, cancellationToken);

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