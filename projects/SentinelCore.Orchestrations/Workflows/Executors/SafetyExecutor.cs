// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         SafetyExecutor.cs
// Author: Kyle L. Crowder
// Build Num:  100310



using Microsoft.Extensions.Logging;

using SentinelCore.Contracts.Abstractions;
using SentinelCore.Orchestrations.Agents;
using SentinelCore.Orchestrations.SafetyEngine;
using SentinelCore.Orchestrations.SafetyEngine.Rules;
using SentinelCore.Orchestrations.Workflows.Helpers;




namespace SentinelCore.Orchestrations.Workflows.Executors;





/// <summary>
///     An executor that evaluates incoming messages against configured safety rules
///     and either blocks or allows them to proceed based on the evaluation result.
///     This executor uses the <see cref="SafetyRuleEngine" /> to orchestrate rule evaluation
///     and leverages the agent factory to create safety-specific agents when needed.
/// </summary>
[YieldsOutput(typeof(DetectionBoolResult))]
public sealed partial class SafetyExecutor : Executor
{
    private readonly ISentinelAgentFactory _agentFactory;
    private readonly ISystemReporter _reporter;
    private readonly SafetyRuleEngine _ruleEngine;



    private static readonly IReadOnlyList<ISafetyRule> DefaultRules = new List<ISafetyRule>
    {
            new BlocklistRule("DefaultBlocklist", SafetyTriggerTerms.GetAllIndicators()),

            // Code Injection Detection
            new CodeInjectionRule(),

            // Data Exfiltration Detection
            new DataExfiltrationRule(),

            // Encoding Evasion Detection
            new EncodingEvasionRule(),

            // Harmful Content Detection
            new HarmfulContentRule(),

            // Max Length Rule
            new MaxLengthRule(),

            // PII Detection
            new PIIDetectionRule(),

            // Prompt Injection Detection
            new PromptInjectionRule(),

            // Repetition Attack Detection
            new RepetitionAttackRule(),

            // Role Escalation Detection
            new RoleEscalationRule(),

            // System Prompt Extraction Detection
            new SystemPromptExtractionRule(),

            // Token Limit Rule
            new TokenLimitRule(),

            // URL Block Rule
            new UrlBlockRule()
    };








    /// <summary>
    ///     Initializes a new instance of the <see cref="SafetyExecutor" />.
    /// </summary>
    /// <param name="reporter">The system reporter for logging safety events.</param>
    /// <param name="agentFactory">The agent factory for creating safety agents.</param>
    /// <param name="loggerFactory">The logger factory for creating safety engine loggers.</param>
    public SafetyExecutor(ISystemReporter reporter, ISentinelAgentFactory agentFactory, ILoggerFactory loggerFactory, IEnumerable<ISafetyRule>? rules = null) : base("SafetyExecutor")
    {
        _reporter = reporter ?? throw new ArgumentNullException(nameof(reporter));
        _agentFactory = agentFactory ?? throw new ArgumentNullException(nameof(agentFactory));
        Name = Id;

        // Initialize the rule engine with provided or default safety rules
        var ruleList = rules?.ToList() ?? DefaultRules.ToList();
        _ruleEngine = new SafetyRuleEngine(ruleList, loggerFactory, reporter: _reporter);
    }








    /// <summary>
    ///     Gets the human-readable name of this executor, used in log messages and diagnostics.
    /// </summary>
    public string Name { get; init; }








    /// <summary>
    ///     Applies safety evaluation results to a chat message by attaching safety-related metadata.
    /// </summary>
    /// <param name="message">
    ///     The chat message to which safety tags will be applied.
    /// </param>
    /// <param name="result">
    ///     The safety evaluation result containing the safety score, evaluation status, and other metadata.
    /// </param>
    /// <returns>
    ///     A new <see cref="ChatMessage" /> instance with safety tags applied to its additional properties.
    /// </returns>
    /// <remarks>
    ///     This method determines the safety result IsAllowed: (e.g., "True" or "False") based on the evaluation result
    ///     and attaches it to the message along with the total safety score.
    /// </remarks>
    private static ChatMessage ApplySafetyTags(ChatMessage message, SafetyEvaluationResult result)
    {
        message.AdditionalProperties ??= new AdditionalPropertiesDictionary();

        // Apply standard safety metadata
        message.AdditionalProperties["SafetyScore"] = result.TotalScore;
        message.AdditionalProperties["IsAllowed"] = result.IsAllowed.ToString();

        // Apply tags expected by existing tests and for backward compatibility
        message.AdditionalProperties["safetyScore"] = result.TotalScore;
        message.AdditionalProperties["safetyResult"] = result.IsAllowed ? "allowed" : "blocked";

        return message;
    }








    /// <summary>
    ///     Handles the safety evaluation of an incoming message.
    ///     Provides uniform cross-cutting concerns: logging, null validation,
    ///     cooperative cancellation propagation, and structured error handling.
    /// </summary>
    /// <param name="message">The message to evaluate against safety rules.</param>
    /// <param name="context">The workflow context providing shared state and yielding.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The original message if allowed, or a blocked response message.</returns>
    [MessageHandler]
    public async ValueTask<DetectionBoolResult> HandleChatMessageAsync(ChatMessage? message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        // --- DEBUG: Entry point ---
        _reporter.ReportInfo($"[{Name}] DEBUG: HandleChatMessageAsync entered. Message text: {(message?.Text?.Length > 50 ? message.Text[..50] + "..." : message?.Text)}");

        // --- Null validation ---
        if (message is null)
        {
            _reporter.ReportError($"[{Name}] Input message was null.");
            ChatMessage fallback = new(ChatRole.Assistant, $"[{Name}] Input message was null.");
            await context.YieldOutputAsync(fallback, cancellationToken).ConfigureAwait(false);
            return new DetectionBoolResult(fallback, true);
        }

        try
        {

            await context.QueueStateUpdateAsync(WorkFlowStateKeys.PROMPT, message.Text, "SharedState", cancellationToken);
            // --- Status: Begin processing ---
            _reporter.ReportInfo($"[{Name}] Processing message...");

            ChatMessage? result = await ProcessMessageAsync(message, context, cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrEmpty(result?.Text))
            {
                _reporter.ReportError($"[{Name}] ProcessMessageAsync returned null. Defaulting to blocked for safety.");
                return new DetectionBoolResult(message, true);
            }

            // DetectionBoolResult.IsTrue = true means the message IS blocked/detected
            bool isBlocked = result.GetMetaTagByKey("IsAllowed") == "False";
            DetectionBoolResult output = new(result, isBlocked);
            _reporter.ReportInfo($"[{Name}] Message processed. IsBlocked: {output.IsTrue}");

            return output;
        }
        catch (OperationCanceledException)
        {
            // Cooperative cancellation must propagate — never swallow it.
            _reporter.ReportInfo($"[{Name}] Execution canceled.");
            throw;
        }
        catch (Exception ex)
        {
            // --- Robust error handling ---
            _reporter.ReportError($"[{Name}] Exception: {ex.Message}", ex);

            ChatMessage msg = new(ChatRole.Assistant, $"⚠️ An internal error occurred in {Name}: {ex.Message}");
            await context.YieldOutputAsync(msg, cancellationToken).ConfigureAwait(false);

            _reporter.ReportInfo($"[{Name}] Returning fallback {nameof(ChatMessage)} due to error.");

            return new DetectionBoolResult(msg, true);
        }
    }








    /// <summary>
    ///     Core processing logic: evaluates the message against safety rules.
    /// </summary>
    private async ValueTask<ChatMessage> ProcessMessageAsync(ChatMessage message, IWorkflowContext context, CancellationToken cancellationToken)
    {
        _reporter.ReportInfo($"[{Name}] Starting safety evaluation.");



        // Create evaluation context from the message
        IReadOnlyList<ChatMessage> messages = new List<ChatMessage> { message };
        SafetyEvaluationContext evalContext = new(messages);

        // Evaluate the message against safety rules
        //if the message is blocked by safety rules, add metadata and return a blocked response
        //The message should be returned to user while the metadata is the source of truth for the decision. 
        SafetyEvaluationResult result = await _ruleEngine.EvaluateAsync(evalContext, cancellationToken).ConfigureAwait(false);

        _reporter.ReportInfo($"[{Name}] DEBUG: SafetyRuleEngine result - IsAllowed: {result.IsAllowed}, Score: {result.TotalScore}, Summary: {result.Summary}");

        // ChatMessage taggedMessage = ApplySafetyTags(message, result); // <------ This line applies safety result to the additional properties of the message
        await context.QueueStateUpdateAsync(WorkFlowStateKeys.PROMPT, message, "SharedState", cancellationToken);



        _reporter.ReportInfo($"[{Name}] Message passed safety evaluation with {result.RuleResults.Count} rule result(s).");

        return message;
    }
}