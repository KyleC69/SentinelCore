// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         TheCoreWorkflow.cs
// Author: Kyle L. Crowder
// Build Num:  081312



using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using SentinelCore.Abstractions;
using SentinelCore.Agents;
using SentinelCore.Agents.Models;
using SentinelCore.Application;
using SentinelCore.Events;
using SentinelCore.SafetyEngine;
using SentinelCore.Workflows.Executors;




namespace SentinelCore.Workflows;





/// <summary>
///     Represents a multi-agent and non-agent workflow designed to classify incoming signals
///     and route them to the appropriate executor based on the classification result.
/// </summary>
/// <remarks>
///     This workflow is responsible for building the workflow graph and delegating execution
///     to the appropriate components. It leverages the MAF runtime for routing and execution.
///     <para>
///         Key features of this workflow include:
///     </para>
///     <list type="number">
///         <item>Classifies signals and produces routing decisions.</item>
///         <item>Routes signals to the appropriate executor using a switch mechanism.</item>
///         <item>
///             Handles various scenarios such as:
///             <list type="bullet">
///                 <item>Investigation workflows using <see cref="InvestigationExecutor" />.</item>
///                 <item>Safety-related workflows using <see cref="SafetyExecutor" />.</item>
///                 <item>
///                     Direct answers, pattern matching, noise handling, and more using
///                     <see cref="DirectAnswerExecutor" />.
///                 </item>
///             </list>
///         </item>
///     </list>
///     The shared message object, <see cref="SignalHypothesis" />, flows between steps
///     to ensure seamless communication and processing.
/// </remarks>
public sealed class TheCoreWorkflow : WorkflowBase, IOrchestration
{
    private readonly ILoggerFactory _Factory;
    private readonly ISentinelAgentFactory _agentFactory;
    private readonly IAgentProfileBuilder _agentSpecBuilder;
    private readonly ISentinelCoreEvents _events;
    private readonly IServiceProvider _provider;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true, PropertyNameCaseInsensitive = true };








    /// <summary>
    ///     Initializes a new instance of the <see cref="TheCoreWorkflow" /> class.
    /// </summary>
    /// <param name="agentSpecBuilder">
    ///     An instance of <see cref="IAgentProfileBuilder" /> used to build agent profiles.
    /// </param>
    /// <param name="execfactory"></param>
    /// <param name="systemReporter">
    ///     An instance of <see cref="ISystemReporter" /> used for reporting system-level events or errors.
    /// </param>
    /// <param name="events">
    ///     An instance of <see cref="ISentinelCoreEvents" /> used to handle core events.
    /// </param>
    /// <param name="agentFactory">
    ///     An instance of <see cref="ISentinelAgentFactory" /> used to create agents for the workflow.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if any of the provided parameters are <c>null</c>.
    /// </exception>
    public TheCoreWorkflow(IAgentProfileBuilder agentSpecBuilder, ISystemReporter systemReporter, ISentinelCoreEvents events, ISentinelAgentFactory agentFactory, ILoggerFactory factory, IServiceProvider provider) : base(systemReporter)
    {
        Throw.IfNull(agentSpecBuilder);
        Throw.IfNull(systemReporter);
        Throw.IfNull(events);
        Throw.IfNull(agentFactory);
        Throw.IfNull(factory);
        Throw.IfNull(provider);
        _agentSpecBuilder = agentSpecBuilder;
        _events = events;
        _agentFactory = agentFactory;
        _Factory = factory;
        _provider = provider;




    }








    /// <summary>
    ///     Constructs and returns the workflow for the current orchestration.
    /// </summary>
    /// <returns>
    ///     An instance of <see cref="Workflow" /> representing the constructed workflow.
    /// </returns>
    /// <remarks>
    ///     This method defines the core logic for building the workflow specific to this orchestration.
    /// </remarks>
    public async Task<Workflow> BuildWorkflow()
    {
        // ── Compose the Outer parent workflow ──────────────────────────────────────
        // TheCore classifies → Switch on CoreRoutingDecision.NextStep
        //   Investigate  → Investigation (Magentic) → Aggregator → Analysis (Group) → Safety → TheCore Final
        //   CanAnswerDirectly / PatternMatch → DirectAnswer
        //   RedAlert / EscalateToHumanOperator → Safety
        //   IsNoise / MoreInformationRequired → DirectAnswer

        // ── Build agents ───────────────────────────────────────────────────────────


        AgentProfile classiferprofile = _agentSpecBuilder.BuildAgentSpec("Classifier", AgentRole.Utility);
        classiferprofile.Instructions = """
                                        You are acting as an expert Systems and Software Engineer in an AI controlled investigation platform.
                                        You will be given information that may be from automated telemetry, anomaly detectors or in the form of natural speech from an end-user.
                                        This is known as a signal in this application and can indicate Operating System problems or hardware errors, Event logs, performance counters etc.
                                        You main task is to understand the user intent and to formulate a hypothesis on the source of signal.

                                        You must respond with a JSON object matching the SignalHypothesis schema:

                                        {
                                            "category": "The affected subsystem",
                                            "hypothesis": "Your hypothesis here",
                                            "initialConfidenceScore": 0.0-1.0,
                                            "nextStep": "One of: RedAlert, Investigate, MoreInformationRequired, EscalateToHumanOperator, or DirectAnswer",
                                            "reasoning": "Explain the driving factors in your decisions"
                                        }

                                        Output ONLY valid JSON. Do not include any text before or after the JSON object.
                                        Do not wrap in fenced code blocks(```)

                                        Rules for nextStep:

                                        - If the signal indicates catastrophic hardware or software failure is imminent, choose RedAlert.
                                        - If the signal is ambiguous, choose MoreInformationRequired.
                                        - If the signal is a question about the system environment or status, choose Investigate.
                                        - If the signal contains procedural instructions (e.g., check logs, scan drivers, query WMI), you must choose DirectAnswer
                                        - All other cases: choose Investigate and provide a reasonable hypothesis about what the signal may indicate. The category can be the subsystem affected.
                                        - Do NOT wrap response in fence blocks
                                        """;

        classiferprofile.ResponseFormat = ChatResponseFormat.ForJsonSchema(AIJsonUtilities.CreateJsonSchema(typeof(SignalHypothesis)));



        AgentProfile coreprofile = _agentSpecBuilder.BuildAgentSpec("TheCore", AgentRole.Core);
        coreprofile.ResponseFormat = ChatResponseFormat.ForJsonSchema(AIJsonUtilities.CreateJsonSchema(typeof(CoreDirective)));
        coreprofile.Instructions = """
                                    You are Sentinel Core.
                                   Your only job is to produce a structured directive for the MAG Manager.
                                   You must not produce diagnostic steps, procedures, subsystem names, or evidence requests.
                                   You must not describe how to investigate.
                                   You must not suggest tools or methods.
                                   You must not infer system state without evidence.
                                   You must not fabricate facts.

                                   You must output exactly one object with the following fields:

                                   Hypothesis — your best explanation of the signal

                                   Intent — the purpose of the MAG team’s work

                                   Type — the classification of the task

                                   Scope — the breadth of the investigation

                                   Urgency — the priority level

                                   Notes — optional contextual hints

                                   If the user request is procedural (e.g., “show errors in last 24 hours”), set Type = Procedural and do not generate a hypothesis.
                                   If the request is investigative, generate a hypothesis and set Type = Investigative.
                                   If the request is contextual (e.g., “what is the system load?”), set Type = Contextual.

                                   You must not output anything except the structured directive object.
                                   No prose.
                                   No explanations.
                                   No reasoning paragraphs.
                                   No narrative.
                                   Only the object.

                                   """;

        _reporter.ReportInfo("Agent profiles constructed....");

        // Agent factory is still messy but is flexible enough to allow proper assignment of model tuning params
        // Customizable models, preset personas, Response format and system prompt config during creation.
        // The core client is wrapped with loggers, event publishing agent, safetyware and middleware.

        AIAgent safetyAgent = await _agentFactory.BuildFromProfileAsync(_agentSpecBuilder.BuildAgentSpec("SafetyAgent", AgentRole.Utility)).ConfigureAwait(false);
        AIAgent SafeAI = await BuildAgentAsync().ConfigureAwait(false);
        AIAgent theCore = await _agentFactory.BuildFromProfileAsync(coreprofile).ConfigureAwait(false);
        AIAgent classifier = await _agentFactory.BuildFromProfileAsync(classiferprofile).ConfigureAwait(false);

        // TEMPORARILY CREATED HERE UNTIL BETTER PLACEMENT IS ESTABLISHED - goal is to tie session app life-cycle
        AgentSession session = await theCore.CreateSessionAsync().ConfigureAwait(false);

        // ── Construct NON-Agent executors ────────────────────────────────────────────────────
        //
        //Pull the executors out of DI using factory -------------------------------------------------

        AIAgentHostOptions options = new() { EmitAgentUpdateEvents = true, EmitAgentResponseEvents = true, ReassignOtherAgentsAsUsers = true, ForwardIncomingMessages = false };
        SafetyExecutor safetyExecutor = ActivatorUtilities.CreateInstance<SafetyExecutor>(_provider);

        EscalatedExecutor escalatedExecutor = ActivatorUtilities.CreateInstance<EscalatedExecutor>(_provider);

        WhiteListExecutor whiteList = ActivatorUtilities.CreateInstance<WhiteListExecutor>(_provider);

        PatternCheckExecutor patternCheck = ActivatorUtilities.CreateInstance<PatternCheckExecutor>(_provider);

        HumanOperatorExecutor humanOperator = ActivatorUtilities.CreateInstance<HumanOperatorExecutor>(_provider);

        InvestigationExecutor investigationExecutor = ActivatorUtilities.CreateInstance<InvestigationExecutor>(_provider);

        VerifyEvidenceExecutor validateEvidence = ActivatorUtilities.CreateInstance<VerifyEvidenceExecutor>(_provider);

        DirectAnswerExecutor directAnswerExecutor = ActivatorUtilities.CreateInstance<DirectAnswerExecutor>(_provider);


        NewCaseExecutor newCase = ActivatorUtilities.CreateInstance<NewCaseExecutor>(_provider);

        AggregationExecutor aggregator = ActivatorUtilities.CreateInstance<AggregationExecutor>(_provider);

        MoreInformationExecutor moreinfo = ActivatorUtilities.CreateInstance<MoreInformationExecutor>(_provider);

        CriticalAlert critical = ActivatorUtilities.CreateInstance<CriticalAlert>(_provider);

        LoggingExecutor logger = ActivatorUtilities.CreateInstance<LoggingExecutor>(_provider);

        CaseGenExec caseGen = ActivatorUtilities.CreateInstance<CaseGenExec>(_provider);

        //Wrapped executors --------------

        Workflow evidence = await BuildSubWorkflowAsync();
        ExecutorBinding evidenceBinding = evidence.BindAsExecutor("EvidenceCollection");

        //Agent safety valve
        ExecutorBinding seer = SafeAI.BindAsExecutor();

        // NOTE: Have not been able to use agents BindAsExecutor() method to fire correctly something in the message handling is failing.
        // Agents wrapped in derived Executors <see cref="Executor" /> are able to operate correctly
        // ── create agent executors, the classifier as the entry-point executor ───────────────────────────────

        ClassifierAgentExec classifiedExec = new(classifier);
        TheCoreExec coreExec = new(theCore, session, _reporter);

        // ── Compose the switch-based routing graph ─────────────────────────────────

        WorkflowBuilder builder = new(patternCheck); // performs initial basic checks on signal(message)

        builder.AddEdge(patternCheck, whiteList); //Checks signal against operator created whitelist. benign issues user has chosen to ignore.
        builder.AddEdge(whiteList, caseGen, GetCommand(CommandValue.CASEGEN)); //Special pipeline for quickly generating cases from current event logs etc.
        builder.AddEdge(whiteList, classifiedExec, GetCommand(CommandValue.OTHER)); // Agent classification router - route when NOT CASEGEN

        builder.AddSwitch(classifiedExec, switchBuilder => switchBuilder.AddCase(GetCondition(NextStep.Investigate), newCase) // open new case and move next
                .AddCase(GetCondition(NextStep.RedAlert), critical) // Hardware failure critical error, impending catastrophe
                .AddCase(GetCondition(NextStep.MoreInformationRequired), moreinfo) // request more information from the user needed to interpret the signal
                .AddCase(GetCondition(NextStep.EscalateToHumanOperator), humanOperator) // route to human operator for further analysis and decision making
                .AddCase(GetCondition(NextStep.DirectAnswer), directAnswerExecutor)
                .WithDefault(newCase));

        builder.AddEdge(critical, humanOperator)
                .AddEdge(moreinfo, humanOperator)
                .AddEdge(newCase, coreExec) // move to the core for initial analysis
                .AddEdge(coreExec, evidenceBinding) // send analysis to the investigation manager
                .AddEdge(evidenceBinding, aggregator) // Gather evidence supporting hypothesis
                .AddEdge(aggregator, validateEvidence) // collect the investigation evidence
                .AddEdge(validateEvidence, coreExec) //Validate evidence
                .WithName("TheCoreFlow")
                .WithDescription("The main investigation and case management workflow");

        //Build the flow
        Workflow flow = builder.Build();

        //Create Graphviz string of entire flow
        VisualizeWorkflow(flow);

        return flow;
    }








    public string Description { get; } = "TheCore is a multi-agent & non-agent workflow that classifies an incoming signal and routes it to the appropriate executor based on the classification result. It demonstrates a structured approach to handling various scenarios, including investigation, direct answers, safety concerns, and escalation to human operators. The workflow is designed to ensure that each step is executed by the appropriate agent or executor, providing a clear and efficient process for managing complex tasks.";








    /// <summary>
    ///     Executes the core workflow asynchronously.
    /// </summary>
    /// <param name="message">The input message for the workflow, represented as a <see cref="ChatMessage" />.</param>
    /// <param name="token">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task representing the asynchronous operation, which upon completion provides a
    ///     <see cref="WorkflowExecutionResult" />.
    /// </returns>
    /// <remarks>
    ///     This method builds the workflow, executes it in a streaming manner, and processes events emitted during execution.
    /// </remarks>
    public async Task ExecuteAsync(ChatMessage message, CancellationToken token)
    {
        Throw.IfNull(message);

        this.ResetEventAccumulators();

        Workflow workflow = await BuildWorkflow().ConfigureAwait(false);

        Run result = await InProcessExecution.RunAsync(workflow, message, cancellationToken: token).ConfigureAwait(false);
        foreach (WorkflowEvent evt in result.NewEvents)
            if (evt is WorkflowOutputEvent outputEvt)
            {
                this.ProcessEvent(outputEvt);

            }
    }








    public string Name { get; } = nameof(TheCoreWorkflow);








    private async Task<AIAgent> BuildAgentAsync()
    {
        AgentProfile agentProfile = new()
        {
                AgentId = "SafetyAgent",
                AgentName = "SafetyAgent",
                Model = new ModelProfile
                {
                        Endpoint = "http://localhost:11111",
                        Provider = ModelProfile.ModelProvider.Ollama,
                        MaxOutputTokens = 16000,
                        ModelId = "gemma4",
                        Temperature = 0.3f
                },
                Instructions = "You are a helpful agent."
        };
        SafetyEngineOptions opt = new();


        List<ISafetyRule> rules = SentinelAgentFactory.CreateSafetyRules();
        AIAgent agent = await _agentFactory.BuildFromProfileAsync(agentProfile);
        agent.AsBuilder().UseSafetyEngine(rules, _Factory.CreateLogger<SafetyEngineAgent>(), opt).Build();

        return agent;

    }








    /// <summary>
    ///     Builds the Magentic investigation sub-workflow.
    ///     <para>
    ///         This sub-workflow consists of a managing agent (<see cref="AgentRole.Manager" />)
    ///         and three utility agents. The manager coordinates the utility agents and aggregates
    ///         their results. When the sub-workflow completes, results flow back to the
    ///         parent workflow for further processing.
    ///     </para>
    /// </summary>
    /// <returns>The composed investigation <see cref="Workflow" />.</returns>
    private async Task<Workflow> BuildSubWorkflowAsync()
    {
        // ── Build agents for the Magentic sub-workflow ──────────────────────────────
        // AIAgent manager = _agentFactory.BuildFromProfile(
        AgentProfile managerpro = _agentSpecBuilder.BuildAgentSpec("Manager", AgentRole.Manager);
        managerpro.Instructions = """
                                  You are the MAG Manager.
                                  Your job is to convert a CoreDirective into one or more InvestigationSteps.
                                  You must not generate hypotheses.
                                  You must not generate reasoning.
                                  You must not interpret evidence.
                                  You must not modify the hypothesis.
                                  You must not fabricate facts.

                                  You must:

                                  Read the CoreDirective fields (Intent, Type, Scope, Urgency, Hypothesis.Category).

                                  Select the correct MAG Worker based on its declared capabilities.

                                  Create an InvestigationStep for each action you assign.

                                  Populate: Timestamp, Agent, Action, Input.

                                  Send the task to the selected worker.

                                  Receive the worker’s Output, Evidence, and ConfidenceDelta.

                                  Insert these into the InvestigationStep.

                                  Append the step to the InvestigationLedger.

                                  You must not:

                                  Use TheCore’s reasoning for routing.

                                  Use worker reasoning to modify the directive.

                                  Add your own reasoning.

                                  Suggest diagnostic steps.

                                  Suggest subsystems.

                                  Suggest tools.

                                  You must route tasks based ONLY on:

                                  DirectiveType

                                  DirectiveScope

                                  DirectiveIntent

                                  Hypothesis.Category

                                  Worker capabilities

                                  You must output only InvestigationSteps.
                                  No narrative.
                                  No prose.
                                  No explanations.
                                  Only structured steps.

                                  """;
        managerpro.ResponseFormat = ChatResponseFormat.ForJsonSchema(AIJsonUtilities.CreateJsonSchema(typeof(InvestigationStep)));
        AIAgent manager = await _agentFactory.BuildFromProfileAsync(managerpro);

        //Generate a baseline profile with guided defaults
        AgentProfile prof = _agentSpecBuilder.BuildAgentSpec("Worker1", AgentRole.Utility);

        //Customize the profile before creating the agent
        prof.Instructions = """
                            You are a MAG Worker.
                            Your job is to execute a single task assigned by the MAG Manager.
                            You must not generate hypotheses.
                            You must not modify the directive.
                            You must not interpret the directive.
                            You must not fabricate facts.

                            You must:

                            Perform the action assigned by the manager using your toolbelt.

                            Produce Output (raw results from tools).

                            Produce Evidence (structured property/value/condition items).

                            Produce ConfidenceDelta (based on evidence relevance).

                            You must not:

                            Suggest additional steps.

                            Suggest subsystems.

                            Suggest tools.

                            Produce narrative reasoning beyond what is needed to explain evidence.

                            Modify the hypothesis.

                            Modify the directive.

                            Your output must contain:

                            Output (object)

                            Evidence (list of EvidenceItem or dictionary)

                            ConfidenceDelta (double)

                            You must output only the response object.
                            No prose.
                            No narrative.
                            No explanations.
                            """;

        prof.Tools = ToolRegistry.GetAllTools();
        AIAgent utility1 = await _agentFactory.BuildFromProfileAsync(prof);


        //Copy profile to new copy and modify
        AgentProfile prof2 = prof;
        prof2.AgentId = "worker2";
        prof2.AgentName = "Worker2";
        AIAgent utility2 = await _agentFactory.BuildFromProfileAsync(prof2);


        AgentProfile prof3 = prof2;
        prof3.AgentId = "worker3";
        prof3.AgentName = "Worker3";
        AIAgent utility3 = await _agentFactory.BuildFromProfileAsync(prof3);


        //  AIAgent aggregator = _agentFactory.BuildFromProfile(_agentSpecBuilder.BuildAgentSpec("Aggregator", AgentRole.Utility));


        // ── Bind the manager as the entry-point executor ────────────────────────────
        AIAgentHostOptions managerOptions = new() { EmitAgentUpdateEvents = true, EmitAgentResponseEvents = true, ReassignOtherAgentsAsUsers = true, ForwardIncomingMessages = true };

        ExecutorBinding managerBinding = manager.BindAsExecutor(managerOptions);

        // ── Compose the sub-workflow graph ─────────────────────────────────────────
        Workflow subWorkflow = new MagenticWorkflowBuilder(manager).AddParticipants(utility1, utility2, utility3).WithMaxResets(3).WithMaxRounds(3).WithMaxStalls(2).RequirePlanSignoff(false).WithDescription("Magnetic sub-workflow for collecting evidence to support theCore's hypothesis of the signal").WithName("EvidenceCollection").Build();
        return subWorkflow;
    }








    /// <summary>
    ///     Creates a condition function that evaluates whether the provided detection result
    ///     matches the expected decision.
    /// </summary>
    /// <param name="expectedDecision">
    ///     The expected <see cref="CommandValue" /> to compare against the detection result.
    /// </param>
    /// <returns>
    ///     A function that takes an object and returns <c>true</c> if the object is a
    ///     <see cref="SuppressionDecision" /> with a <see cref="SuppressionDecision.Command" />
    ///     matching the <paramref name="expectedDecision" />; otherwise, <c>false</c>.
    /// </returns>
    private static Func<object?, bool> GetCommand(CommandValue expectedDecision)
    {
        return detectionResult => detectionResult is SuppressionDecision result && result.Command == expectedDecision;
    }








    // ─────────────────────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────────────────────








    /// <summary>
    ///     Creates a condition for routing messages based on the result of the
    ///     next step decision from TheCore.
    ///     This condition is used to evaluate whether a given detection result
    ///     matches the expected next step decision.
    ///     It ensures that the routing logic aligns with the expected workflow
    ///     behavior by comparing the `NextStep` property of the detection result
    ///     to the provided expected decision.
    /// </summary>
    /// <param name="expectedDecision">
    ///     The expected next step decision, which represents the desired outcome
    ///     for the routing logic. This value is compared against the `NextStep`
    ///     property of the detection result.
    /// </param>
    /// <returns>
    ///     A function that evaluates whether a message meets the expected result.
    ///     The returned function checks if the detection result is of type
    ///     `CoreRoutingDecision` and whether its `NextStep` matches the
    ///     `expectedDecision`.
    /// </returns>
    private static Func<object?, bool> GetCondition(NextStep expectedDecision)
    {
        return detectionResult => detectionResult is CoreRoutingDecision result && result.NextStep == expectedDecision;
    }








    private void VisualizeWorkflow(Workflow workflow)
    {
        string flow = workflow.ToDotString();
        Console.WriteLine(flow);
        File.WriteAllText("workflow.dot", flow);
        File.WriteAllText("workflow.mermaid", flow);
    }
}