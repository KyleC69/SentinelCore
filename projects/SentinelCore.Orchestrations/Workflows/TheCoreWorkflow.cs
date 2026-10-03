// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         TheCoreWorkflow.cs
// Author: Kyle L. Crowder
// Build Num:  092520



using SentinelCore.Abstractions;
using SentinelCore.Contracts.Abstractions;
using SentinelCore.Orchestrations.Abstractions;
using SentinelCore.Orchestrations.Agents;
using SentinelCore.Orchestrations.Application;
using SentinelCore.Orchestrations.Exceptions;
using SentinelCore.Orchestrations.Services;
using SentinelCore.Orchestrations.Workflows.Executors;
using SentinelCore.Orchestrations.Workflows.Helpers;




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
    private AIAgent? _classifierAgent;
    private readonly ExecutorFactory _executorFactory;
    private readonly object _initLock = new();
    private volatile bool _isInitialized;

    // Pre-created sub-workflow agents (initialized once via InitializeAsync)
    private AIAgent? _safetyAgent;
    private Workflow _theCoreWorkflow;
    // Pre-created agents and sessions (initialized once via InitializeAsync)


    // TheCore and sub-workflow agents (initialized once via InitializeAsync)
    private AIAgent? _theCoreAgent;



    private AIAgent? _magManagerAgent;

    private AIAgent? _windowsWorkerAgent1;
    private AIAgent? _windowsWorkerAgent2;
    private AIAgent? _windowsWorkerAgent3;
    private AIAgent? _windowsWorkerAgent4;
    private AgentSession _theCoreSession;
    private AIAgent? _objectGenExec;








    /// <summary>
    ///     Initializes a new instance of the <see cref="TheCoreWorkflow" /> class.
    /// </summary>
    /// <param name="systemReporter">
    ///     An instance of <see cref="ISystemReporter" /> used for reporting system-level events or errors.
    /// </param>
    /// <param name="agentFactory">
    ///     An instance of <see cref="ISentinelAgentFactory" /> used to create agents for the workflow.
    /// </param>
    /// <param name="serviceProvider">
    ///     An instance of <see cref="IServiceProvider" /> used to resolve service dependencies.
    /// </param>
    /// <param name="eventProcessor">
    ///     The event processor responsible for formatting and reporting workflow events.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if any of the provided parameters are <c>null</c>.
    /// </exception>
    public TheCoreWorkflow(ISystemReporter systemReporter, ISentinelAgentFactory agentFactory, IServiceProvider serviceProvider, IWorkflowEventProcessor eventProcessor) : base(systemReporter, eventProcessor)
    {
        Throw.IfNull(systemReporter);
        Throw.IfNull(agentFactory);
        Throw.IfNull(serviceProvider);
        Throw.IfNull(eventProcessor);

        _agentFactory = agentFactory;
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
    private Task<Workflow> BuildWorkflow()
    {
        Throw.IfNull(_theCoreAgent);
        Throw.IfNull(_classifierAgent);
        Throw.IfNull(_safetyAgent);
        Throw.IfNull(_theCoreSession);
        Throw.IfNull(_magManagerAgent);
        Throw.IfNull(_windowsWorkerAgent1);
        Throw.IfNull(_windowsWorkerAgent2);
        Throw.IfNull(_windowsWorkerAgent3);
        Throw.IfNull(_windowsWorkerAgent4);

        // ── Construct NON-Agent executors ────────────────────────────────────────────────────

        ExecutorCollection executors = _executorFactory.CreateAllExecutors();

        // ── Build sub-workflow ──────────────────────────────────────────────────────────────────
        SubWorkflowExecutor subWorkflowExecutor = new SubWorkflowExecutor(BuildEvidenceGatheringSubWorkflow());
        Workflow evidenceGatheringWorkflow = BuildEvidenceGatheringSubWorkflow();
        //   ExecutorBinding evidenceGatheringBinding = evidenceGatheringWorkflow.BindAsExecutor("EvidenceSubWorkflow");

        ExecutorBinding safetyAgentBinding = _safetyAgent.BindAsExecutor(emitEvents: true);

        // ── Create agent executors ─────────────────────────────────────────────────────────────
        ClassifierAgentExec classifierExec = new(_classifierAgent, _reporter);
        // TheCore agent with session
        TheCoreExec theCoreExec = new(_theCoreAgent, _theCoreSession, _reporter);
        DirectAnswerExecutor directAnswerAgentExec = new(_theCoreAgent, _theCoreSession, _reporter);
        GenerateObjectiveExec generateObjectiveExec = new(_theCoreAgent, _theCoreSession, _reporter);

        // ── Compose the switch-based routing graph ─────────────────────────────────────────
        // First stop Safety gate
        WorkflowBuilder builder = new(executors.SafetyExecutor);

        builder.AddEdge(executors.SafetyExecutor, executors.SafetyReviewExecutor, GetCondition(true)); //safety triggers review
        builder.AddEdge(executors.SafetyExecutor, executors.PatternCheckExecutor, GetCondition(false)); //Pass safety to next step

        builder.AddEdge(executors.PatternCheckExecutor, executors.WhiteListExecutor); // Check whitelist for pattern - Operator created list to ignore
        builder.AddEdge(executors.WhiteListExecutor, classifierExec);


        builder.AddSwitch(classifierExec, switchBuilder => switchBuilder
                        .AddCase(GetCondition(NextStep.Investigate), generateObjectiveExec)
                        .AddCase(GetCondition(NextStep.DirectAnswer), directAnswerAgentExec)
                        .AddCase(GetCondition(NextStep.RedAlert), executors.CriticalAlert)
                        .AddCase(GetCondition(NextStep.MoreInformationRequired), executors.MoreInformationExecutor)
                        .AddCase(GetCondition(NextStep.EscalateToHumanOperator), executors.HumanOperatorExecutor));

        builder.AddEdge(executors.MoreInformationExecutor, executors.TerminateWorkflow);

        // ------- RedAlert Branch -------------------------
        // Alert UI to critical error and mark case urgent
        builder.AddEdge(executors.CriticalAlert, executors.HumanOperatorExecutor)
                .AddEdge(executors.HumanOperatorExecutor, executors.TerminateWorkflow);

        // -- ------- MoreInformation Branch -------------------------
        //Mark case needs more info and alert operator
        builder.AddEdge(executors.MoreInformationExecutor, executors.HumanOperatorExecutor)
                .AddEdge(executors.HumanOperatorExecutor, executors.TerminateWorkflow);

        // -- ------- DirectAnswer Branch -------------------------
        builder.AddEdge(directAnswerAgentExec, executors.TerminateWorkflow);

        // -- ------- Investigate Branch -------------------------
        //Open case send to TheCore
        builder.AddEdge(generateObjectiveExec, executors.NewCaseExecutor)   // TheCore generate the initial hypothesis objective
                .AddEdge(executors.NewCaseExecutor, subWorkflowExecutor)    // NewCase creates a new case and sends objective to the sub-workflow for evidence gathering
                .AddEdge(subWorkflowExecutor, executors.TerminateWorkflow); // Sub-workflow evidence gathering Magnetic orchestration

        //     .AddEdge(executors.AggregationExecutor, executors.PersistEvidenceExecutor)
        //     .AddEdge(executors.PersistEvidenceExecutor, executors.TerminateWorkflow); // Evidence is gathered — hand the synthesized results back to core for final synopsis.
        //.AddEdge(theCoreExec, executors.TerminateWorkflow)

        // ---------------  EscalateToHumanOperator Branch -------------------------
        builder.AddEdge(executors.HumanOperatorExecutor, executors.TerminateWorkflow);

        builder.WithName("TheCoreWorkflow")
        .WithDescription("The main investigation and case management workflow");

        // ── Register output sources ─────────────────────────────────────────────────────────
        // MAF only surfaces yielded values from executors registered via WithOutputFrom;
        // without this registration no executor output ever reaches the WorkflowOutputEvent stream.
        builder.WithOutputFrom(classifierExec, directAnswerAgentExec, executors.SafetyExecutor, executors.CriticalAlert, executors.MoreInformationExecutor, executors.TerminateWorkflow, executors.HumanOperatorExecutor);

        Workflow flow = builder.Build();

        // For debug purposes
        VisualizeWorkflowAsync(flow, CancellationToken.None).Wait();

        return Task.FromResult(flow);

    }








    /// <summary>
    ///     Gets a description of the core multi-agent and non-agent workflow.
    /// </summary>
    /// <value>
    ///     A string that provides a detailed explanation of the workflow's purpose, structure, and functionality.
    ///     The description outlines how the workflow classifies incoming signals, routes them to appropriate executors,
    ///     and ensures a structured approach to handling scenarios such as investigation, direct answers, safety concerns,
    ///     and escalation to human operators.
    /// </value>
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
    /// <param name="input"></param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task representing the asynchronous operation, which upon completion provides a
    ///     <see cref="WorkflowExecutionOutput" />.
    /// </returns>
    public async Task<IAsyncEnumerable<WorkflowEvent>?> ExecuteStreamingAsync(ChatMessage input, CancellationToken cancellationToken)
    {
        Throw.IfNull(input);
        try
        {
            // Streaming execution — delegate the stream to the caller
            StreamingRun run = await InProcessExecution.Lockstep.RunStreamingAsync(_theCoreWorkflow, input, cancellationToken: cancellationToken);
            await run.TrySendMessageAsync(new TurnToken(emitEvents: true));
            // Return the async-enumerable directly (do not await it)
            return run.WatchStreamAsync(cancellationToken);
        }
        catch (Exception ex)
        {
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
    public async Task InitializeAsync(CancellationToken cancellationToken = default(CancellationToken))
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

        _theCoreAgent = await _agentFactory.CreateAgentAsync("TheCore", cancellationToken);

        _classifierAgent = await _agentFactory.CreateAgentAsync("Classifier", cancellationToken);

        _safetyAgent = await _agentFactory.CreateAgentAsync("SafetyAgent", cancellationToken);

        _theCoreSession = await _theCoreAgent.CreateSessionAsync(cancellationToken);

        // ── Build MAG sub-workflow agents ─────────────────────────────────────────────────────
        _magManagerAgent = await _agentFactory.CreateAgentAsync("Manager", cancellationToken);

        _windowsWorkerAgent1 = await _agentFactory.CreateAgentAsync("Worker1", cancellationToken);

        _windowsWorkerAgent2 = await _agentFactory.CreateAgentAsync("Worker2", cancellationToken);

        _windowsWorkerAgent3 = await _agentFactory.CreateAgentAsync("Worker3", cancellationToken);
        _windowsWorkerAgent4 = await _agentFactory.CreateAgentAsync("Worker4", cancellationToken);

        _reporter.ReportInfo("Agent profiles constructed.");
        _theCoreWorkflow = await BuildWorkflow();
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
    ///     Constructs and configures the evidence gathering sub-workflow for the Multi-Agent Group (MAG).
    /// </summary>
    /// <remarks>
    ///     This method initializes and composes a sub-workflow using the <see cref="MagenticWorkflowBuilder" />.
    ///     It ensures that all required agents are available before building the workflow.
    ///     The resulting workflow is tailored for evidence collection to support TheCore's hypothesis.
    /// </remarks>
    /// <returns>
    ///     A fully configured <see cref="Workflow" /> instance representing the evidence gathering sub-workflow.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if any of the required agents (_magManagerAgent, _windowsWorkerAgent1, _windowsWorkerAgent2, _windowsWorkerAgent3, or
    ///     _windowsWorkerAgent4) are null.
    /// </exception>
    private Workflow BuildEvidenceGatheringSubWorkflow()
    {
        Throw.IfNull(_magManagerAgent);
        Throw.IfNull(_windowsWorkerAgent1);
        Throw.IfNull(_windowsWorkerAgent2);
        Throw.IfNull(_windowsWorkerAgent3);
        Throw.IfNull(_windowsWorkerAgent4);

        Workflow subWorkflow = new MagenticWorkflowBuilder(_magManagerAgent)
                .AddParticipants(_windowsWorkerAgent1, _windowsWorkerAgent2, _windowsWorkerAgent3, _windowsWorkerAgent4)
                .WithMaxResets(3)
                .WithMaxRounds(5)
                .WithMaxStalls(2)
                .RequirePlanSignoff(false)
                .WithDescription("MAG investigation team - sub-workflow for collecting evidence to support TheCore\'s hypothesis of the signal")
                .WithName("EvidenceCollection")
                .WithOutputFrom(_windowsWorkerAgent4)
                .Build();

        return subWorkflow;
    }








    /// <summary>
    /// Creates a condition function to evaluate if a detection result matches the specified expected outcome.
    /// </summary>
    /// <param name="expectedResult">The expected outcome to compare against the detection result.</param>
    /// <returns>
    /// A <see cref="Func{T, TResult}"/> that takes an object as input and returns a boolean indicating
    /// whether the detection result matches the expected outcome.
    /// </returns>
    private static Func<object?, bool> GetCondition(bool expectedResult)
    {
        return detectionResult => detectionResult is DetectionBoolResult result && result.IsTrue == expectedResult;
    }








    /// <summary>
    ///     Creates a condition function that evaluates whether the provided detection result
    ///     matches the expected next step decision.
    /// </summary>
    /// <param name="expectedDecision">The expected next step decision.</param>
    /// <returns>A function that evaluates whether output meets the expected result.</returns>
    private static Func<object?, bool> GetCondition(NextStep expectedDecision)
    {
        return detectionResult => detectionResult is NextStep result && result == expectedDecision;
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
    ///     Validates that the provided workflow instance is not null.
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