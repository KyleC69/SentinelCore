// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         TheCoreWorkflow.cs
// Author: Kyle L. Crowder
// Build Num:  111504



using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.Logging;

using SentinelCore.CaseFlowEngine.Infrastructure;
using SentinelCore.Contracts.Abstractions;
using SentinelCore.Orchestrations.Abstractions;
using SentinelCore.Orchestrations.Agents;
using SentinelCore.Orchestrations.Agents.AgentPresets;
using SentinelCore.Orchestrations.Application;
using SentinelCore.Orchestrations.Exceptions;
using SentinelCore.Orchestrations.Infrastructure.DependencyInjection;
using SentinelCore.Orchestrations.Infrastructure.Diagnostics;
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
    private AIAgent _adversarialAnalystAgent;
    private readonly ISentinelAgentFactory _agentFactory;
    private bool _agentInitialized;
    private AIAgent? _classifierAgent;
    private AIAgent _evidenceCollectorAgent;
    private readonly ExecutorFactory _executorFactory;
    private AIAgent _finalReportWriterAgent;
    private readonly object _initLock = new();
    private SemaphoreSlim _initializationGate = new(1, 1);
    private volatile bool _isInitialized;



    private AIAgent _managerAgent;
    private AIAgent _researcherAgent;

    // Pre-created sub-workflow agents (initialized once via InitializeAsync)
    private AIAgent? _safetyAgent;
    // Pre-created agents and sessions (initialized once via InitializeAsync)


    // TheCore and sub-workflow agents (initialized once via InitializeAsync)
    private AIAgent? _theCoreAgent;
    private AgentSession? _theCoreSession;
    private Workflow? _theCoreWorkflow;











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
    /// <param name="loggerFactory">
    ///     An instance of <see cref="ILoggerFactory" /> used to create logger instances.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if any of the provided parameters are <c>null</c>.
    /// </exception>
    public TheCoreWorkflow(ISystemReporter systemReporter, ISentinelAgentFactory agentFactory, IServiceProvider serviceProvider, ILoggerFactory loggerFactory) : base(systemReporter, loggerFactory)
    {
        Throw.IfNull(systemReporter);
        Throw.IfNull(agentFactory);
        Throw.IfNull(serviceProvider);
        Throw.IfNull(loggerFactory);

        _agentFactory = agentFactory;
        _executorFactory = new(serviceProvider);
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
    public async Task<IAsyncEnumerable<WorkflowEvent>?> ExecuteStreamingAsync(ChatMessage input, CancellationToken token)
    {
        Throw.IfNull(input);

        using CancellationTokenSource executionCts = CancellationTokenSource.CreateLinkedTokenSource(token);
        CancellationToken executionToken = executionCts.Token;



        try
        {
            StreamingRun run = await InProcessExecution.RunStreamingAsync(_theCoreWorkflow, input, cancellationToken: executionToken).AsTask().WaitAsync(RunStartupTimeout, executionToken).ConfigureAwait(false);
            bool turnAccepted = await run.TrySendMessageAsync(new TurnToken(true)).AsTask().WaitAsync(RunStartupTimeout, executionToken).ConfigureAwait(false);
            if (!turnAccepted)
            {
                this.ReportExecutionFailure("The magnetic group workflow rejected the turn token, so no work was scheduled.");
                return null;
            }

            // Return the async-enumerable directly (do not await it)
            return run.WatchStreamAsync(false, executionToken);



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
    [Experimental("MAAI001")]
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

        await EnsureAgentInitializedAsync(cancellationToken);

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
    ///     Thrown if any of the required agents (_magManagerAgent, _windowsWorkerAgent1, _windowsWorkerAgent2,
    ///     _windowsWorkerAgent3, or
    ///     _windowsWorkerAgent4) are null.
    /// </exception>
    [Experimental("MAAI001")]
    private Workflow BuildEvidenceGatheringSubWorkflow()
    {
        MagenticPromptOverrides over = new() { FinalAnswerPrompt = Orchestrations.MagenticPromptOverrides.FinalAnswerPrompt, TaskLedgerPlanPrompt = Orchestrations.MagenticPromptOverrides.TaskLedgerPlanPrompt, TaskLedgerFactsPrompt = Orchestrations.MagenticPromptOverrides.TaskLedgerFactsPrompt };
        //-------------------  Magentic Group workflow  -------------------
        Workflow workflow = new MagenticWorkflowBuilder(_managerAgent).AddParticipants(_evidenceCollectorAgent, _adversarialAnalystAgent, _researcherAgent, _finalReportWriterAgent) // Added reporting agent back in
                .WithName("ForensicsInvestigationTeamWorkflow")
                .WithDescription("A powerful manager coordinates the investigation team and monitors the activities and progress of the evidence collector, adversarial analyst, and researcher.")
                .RequirePlanSignoff(false)
                .WithPromptOverrides(over)
                .WithOutputFrom(_finalReportWriterAgent)
                .WithMaxRounds(15)
                .WithMaxResets(3)
                .WithMaxStalls(2)
                .Build();



        return workflow;
    }








    /// <summary>
    ///     Constructs and initializes the core workflow for the orchestration process.
    /// </summary>
    /// <remarks>
    ///     This method is marked as experimental and may be subject to changes or removal in future versions.
    /// </remarks>
    /// <returns>
    ///     A <see cref="Workflow" /> instance representing the constructed workflow.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown if the workflow cannot be constructed due to invalid state or configuration.
    /// </exception>
    /// <seealso cref="WorkflowBase" />
    /// <seealso cref="IOrchestration" />
    [Experimental("MAAI001")]
    private async Task<Workflow> BuildWorkflow()
    {
        Throw.IfNull(_theCoreAgent);
        Throw.IfNull(_classifierAgent);
        Throw.IfNull(_safetyAgent);
        Throw.IfNull(_theCoreSession);

        // ── Construct NON-Agent executors ────────────────────────────────────────────────────

        ExecutorCollection executors = _executorFactory.CreateAllExecutors();

        // ── Build sub-workflow ──────────────────────────────────────────────────────────────────
        Workflow evidenceGatheringWorkflow = BuildEvidenceGatheringSubWorkflow();
        SubWorkflowExecutor subWorkflowExecutor = new(evidenceGatheringWorkflow, _reporter);

        ExecutorBinding safetyAgentBinding = _safetyAgent.BindAsExecutor(true);

        // ── Create agent executors ─────────────────────────────────────────────────────────────
        ClassifierAgentExec classifierExec = new(_classifierAgent, _reporter);
        // TheCore agent with session
        TheCoreExec theCoreExec = new(_theCoreAgent, _theCoreSession, _reporter);
        DirectAnswerExecutor directAnswerAgentExec = new(_theCoreAgent, _theCoreSession, _reporter);



        // ── Compose the switch-based routing graph ─────────────────────────────────────────
        // First stop Safety gate
        WorkflowBuilder builder = new(executors.SafetyExecutor);

        builder.AddEdge(executors.SafetyExecutor, executors.SafetyReviewExecutor, GetCondition(true)); //safety triggers review
        builder.AddEdge(executors.SafetyExecutor, executors.PatternCheckExecutor, GetCondition(false)); //Pass safety to next step

        builder.AddEdge(executors.PatternCheckExecutor, executors.WhiteListExecutor); // Check whitelist for pattern - Operator created list to ignore

        builder.AddEdge(executors.WhiteListExecutor, executors.TheCoreSwitchExec);

        // -- ------- DirectAnswer Branch -------------------------
        builder.AddEdge(executors.TheCoreSwitchExec, executors.DirectAnswerExecutor, GetCondition(true)); // TheCore DirectAnswer
        builder.AddEdge(executors.DirectAnswerExecutor, executors.TerminateWorkflow);  //Direct Answer Termination


        // -- ------- Investigate Branch -------------------------
        //Open case send to TheCore
        builder.AddEdge(executors.TheCoreSwitchExec, executors.SignalClassifierExecutor, GetCondition(false)); // Investigation Branch
        builder.AddEdge(executors.SignalClassifierExecutor, executors.NewCaseExecutor) // TheCore generate the initial hypothesis objective
                .AddEdge(executors.NewCaseExecutor, subWorkflowExecutor) // NewCase creates a new case and sends objective to the sub-workflow for evidence gathering
                .AddEdge(subWorkflowExecutor, executors.TerminateWorkflow); // Sub-workflow evidence gathering Magnetic orchestration



        // ------- RedAlert Branch -------------------------
        // Alert UI to critical error and mark case urgent
        builder.AddEdge(executors.CriticalAlert, executors.HumanOperatorExecutor).AddEdge(executors.HumanOperatorExecutor, executors.TerminateWorkflow);

        // -- ------- MoreInformation Branch -------------------------
        //Mark case needs more info and alert operator
        builder.AddEdge(executors.MoreInformationExecutor, executors.HumanOperatorExecutor).AddEdge(executors.HumanOperatorExecutor, executors.TerminateWorkflow);





        //     .AddEdge(executors.AggregationExecutor, executors.PersistEvidenceExecutor)
        //     .AddEdge(executors.PersistEvidenceExecutor, executors.TerminateWorkflow); // Evidence is gathered — hand the synthesized results back to core for final synopsis.
        //.AddEdge(theCoreExec, executors.TerminateWorkflow)

        // ---------------  EscalateToHumanOperator Branch -------------------------
        builder.AddEdge(executors.HumanOperatorExecutor, executors.TerminateWorkflow);

        builder.WithName("TheCoreWorkflow").WithDescription("The main investigation and case management workflow");

        // Workflow spans (build, session, run, executor, edge group, message) are off by default in the
        // workflow framework. Point them at the SentinelCore source so the host's tracer provider
        // exports them alongside the agent spans emitted by SentinelAgentFactory.
        builder.WithOpenTelemetry(activitySource: SentinelCoreDiagnostics.SharedActivitySource);

        // ── Register output sources ─────────────────────────────────────────────────────────
        // MAF only surfaces yielded values from executors registered via WithOutputFrom;
        // without this registration no executor output ever reaches the WorkflowOutputEvent stream.
        builder.WithOutputFrom(directAnswerAgentExec, executors.HumanOperatorExecutor);

        Workflow flow = builder.Build();

        // For debug purposes
        await VisualizeWorkflowAsync(flow, CancellationToken.None).ConfigureAwait(false);

        return flow;

    }








    [Experimental("MAAI001")]
    private async Task CreateAgentsAsync(CancellationToken token)
    {


        // ── Build main workflow agents ─────────────────────────────────────────────────────────

        _theCoreAgent = await _agentFactory.CreateAgentAsync(AgentPreset.CoreAgent, token);

        _classifierAgent = await _agentFactory.CreateAgentAsync(AgentPreset.ClassifierAgent, token);

        _safetyAgent = await _agentFactory.CreateAgentAsync(AgentPreset.SafetyAgent, token);

        _theCoreSession = await _theCoreAgent.CreateSessionAsync(token);

        // ── Build MAG sub-workflow agents ─────────────────────────────────────────────────────


        _managerAgent = await _agentFactory.CreateAgentAsync(AgentPreset.ManagerAgent, "ManagerCoordinator", "Coordinates the investigation team and monitors progress");
        _evidenceCollectorAgent = await _agentFactory.CreateAgentAsync(AgentPreset.WorkerAgent, "EvidenceCollector", "Collects evidence for the investigation", AgentInstructionConstants.TEAM_MEMBER_INSTRUCTIONS);
        _adversarialAnalystAgent = await _agentFactory.CreateAgentAsync(AgentPreset.WorkerAgent, "AdversarialAnalyst", "The challenger of the collected evidence", AgentInstructionConstants.TEAM_MEMBER_INSTRUCTIONS);
        _researcherAgent = await _agentFactory.CreateAgentAsync(AgentPreset.WorkerAgent, "Researcher", "Conducts research related to the investigation and fact checker.", AgentInstructionConstants.TEAM_MEMBER_INSTRUCTIONS);
        _finalReportWriterAgent = await _agentFactory.CreateAgentAsync(AgentPreset.AggregatorAgent, "CaseReportAuthor", "Aggregates the results from all workers and generates the final case report.", AgentInstructionConstants.AGGREGATOR_INSTRUCTIONS);

        //Participants must be verified and created before the sub-workflow is built. The sub-workflow is built here, but it is not executed directly. Instead, it is added to the parent workflow as a sub-workflow executor. This allows the parent workflow to manage the execution of the sub-workflow and handle its outputs appropriately.
        Throw.IfNull(_managerAgent);
        Throw.IfNull(_evidenceCollectorAgent);
        Throw.IfNull(_adversarialAnalystAgent);
        Throw.IfNull(_researcherAgent);
        Throw.IfNull(_finalReportWriterAgent);



        _reporter.ReportInfo("Agent profiles constructed.");
        _theCoreWorkflow = await BuildWorkflow();
        Throw.IfNull(_theCoreAgent);
        Throw.IfNull(_classifierAgent);
        _isInitialized = true;
    }








    /// <summary>
    ///     Lazily initializes every agent the workflow needs on first use, avoiding the sync-over-async deadlocks that
    ///     occur when building agents in the constructor.
    /// </summary>
    /// <remarks>
    ///     Initialization is serialized by <see cref="_initializationGate" /> so concurrent executions cannot create
    ///     duplicate agents, and the whole creation sequence is bounded by
    ///     <see cref="WorkflowBase.AgentInitializationTimeout" /> so an
    ///     unresponsive provider or tool server cannot stall execution. Failures are reported and rethrown so the caller
    ///     can surface a meaningful error instead of failing later with a <see cref="NullReferenceException" />.
    /// </remarks>
    /// <param name="token">A <see cref="CancellationToken" /> to observe while initializing.</param>
    /// <returns>A task that represents the asynchronous initialization operation.</returns>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="token" /> is cancelled.</exception>
    [Experimental("MAAI001")]
    private async Task EnsureAgentInitializedAsync(CancellationToken token)
    {
        if (_agentInitialized)
        {
            return;
        }

        await _initializationGate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            if (_agentInitialized)
            {
                return;
            }

            await CreateAgentsAsync(token).WaitAsync(AgentInitializationTimeout, token).ConfigureAwait(false);

            _agentInitialized = true;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // Caller cancellation is not an initialization failure, so it propagates without being reported.
            throw;
        }
        catch (Exception e)
        {
            this.ReportExecutionFailure("Failed to initialize the custom group workflow agents.", e);
            throw;
        }
        finally
        {
            _initializationGate.Release();
        }
    }








    /// <summary>
    ///     Creates a condition function to evaluate if a detection result matches the specified expected outcome.
    /// </summary>
    /// <param name="expectedResult">The expected outcome to compare against the detection result.</param>
    /// <returns>
    ///     A <see cref="Func{T, TResult}" /> that takes an object as input and returns a boolean indicating
    ///     whether the detection result matches the expected outcome.
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
        try
        {
            await File.WriteAllTextAsync("workflow.dot", flow, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Debug.WriteLine($"Failed to write workflow.dot: {ex.Message}");
        }
    }
}
