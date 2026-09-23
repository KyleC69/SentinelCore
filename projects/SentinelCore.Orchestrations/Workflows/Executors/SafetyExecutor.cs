// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         SafetyExecutor.cs
// Author: Kyle L. Crowder
// Build Num:  092308



using Microsoft.Extensions.Logging;

using SentinelCore.Contracts.Abstractions;
using SentinelCore.Orchestrations.Agents;
using SentinelCore.Orchestrations.SafetyEngine;
using SentinelCore.Orchestrations.SafetyEngine.Rules;




namespace SentinelCore.Orchestrations.Workflows.Executors;





/// <summary>
///     An executor that evaluates incoming messages against configured safety rules
///     and either blocks or allows them to proceed based on the evaluation result.
///     This executor uses the <see cref="SafetyRuleEngine" /> to orchestrate rule evaluation
///     and leverages the agent factory to create safety-specific agents when needed.
/// </summary>
[YieldsOutput(typeof(ChatMessage))]
public sealed partial class SafetyExecutor : Executor
{
    private readonly ISentinelAgentFactory _agentFactory;
    private readonly ISystemReporter _reporter;
    private readonly SafetyRuleEngine _ruleEngine;








    /// <summary>
    ///     Initializes a new instance of the <see cref="SafetyExecutor" />.
    /// </summary>
    /// <param name="reporter">The system reporter for logging safety events.</param>
    /// <param name="agentFactory">The agent factory for creating safety agents.</param>
    /// <param name="loggerFactory">The logger factory for creating safety engine loggers.</param>
    public SafetyExecutor(ISystemReporter reporter, ISentinelAgentFactory agentFactory, ILoggerFactory loggerFactory) : base("SafetyExecutor")
    {
        _reporter = reporter ?? throw new ArgumentNullException(nameof(reporter));
        _agentFactory = agentFactory ?? throw new ArgumentNullException(nameof(agentFactory));
        Name = Id;

        // Initialize the rule engine with default safety rules
        IReadOnlyList<ISafetyRule> rules = CreateDefaultRules();
        _ruleEngine = new SafetyRuleEngine(rules, loggerFactory, reporter: _reporter);
    }








    /// <summary>
    ///     Gets the human-readable name of this executor, used in log messages and diagnostics.
    /// </summary>
    public string Name { get; init; }








    /// <summary>
    ///     Creates the default set of safety rules for evaluation.
    /// </summary>
    /// <returns>A read-only list of safety rules.</returns>
    [Obsolete("")]
    private static IReadOnlyList<ISafetyRule> CreateDefaultRules()
    {
        return new List<ISafetyRule>
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
    }








    /// <summary>
    ///     Creates a fallback result when the executor encounters an error or receives null input.
    /// </summary>
    private ChatMessage CreateFallbackResult() => new(ChatRole.Assistant, $"[{Name}]Safety evaluation could not be completed. The message has been allowed through as a precaution.");








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
    public async ValueTask<ChatMessage> HandleChatMessageAsync(ChatMessage message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        // --- Status: Executor start ---
        _reporter.ReportInfo($"[{Name}] Starting execution. Input type: {typeof(ChatMessage).Name}");

        // --- Null validation ---
        if (message is null)
        {
            _reporter.ReportError($"[{Name}] Input message was null. Returning fallback {nameof(ChatMessage)}.");
            await context.YieldOutputAsync(new ChatMessage(ChatRole.Assistant, $"[{Name}] Input message was null. Returning fallback {nameof(ChatMessage)}."), cancellationToken).ConfigureAwait(false);
            return CreateFallbackResult();
        }

        try
        {
            // --- Status: Begin processing ---
            _reporter.ReportInfo($"[{Name}] Processing message...");

            ChatMessage result = await ProcessMessageAsync(message, context, cancellationToken).ConfigureAwait(false);

            if (result is null)
            {
                _reporter.ReportError($"[{Name}] ProcessMessageAsync returned null. Using fallback {nameof(ChatMessage)}.");
                result = CreateFallbackResult();
            }

            // --- Status: Final output ---
            _reporter.ReportInfo($"[{Name}] Completed successfully. Output type: {nameof(ChatMessage)}");

            return result;
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

            await context.YieldOutputAsync(new ChatMessage(ChatRole.Assistant, $"⚠️ An internal error occurred in {Name}: {ex.Message}"), cancellationToken).ConfigureAwait(false);

            _reporter.ReportInfo($"[{Name}] Returning fallback {nameof(ChatMessage)} due to error.");

            return CreateFallbackResult();
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
        SafetyEvaluationResult result = await _ruleEngine.EvaluateAsync(evalContext, cancellationToken).ConfigureAwait(false);
        ChatMessage taggedMessage = ApplySafetyTags(message, result);

        if (!result.IsAllowed)
        {
            _reporter.ReportWarning($"Safety block: {result.Summary}");

            // Return a blocked response message
            ChatMessage blockedMessage = new(ChatRole.Assistant, $"Request blocked by safety policy: {result.Summary}");
            return SafetyTagger.Attach(blockedMessage, result.TotalScore, "blocked");
        }

        if (result.TotalScore > 0)
        {
            _reporter.ReportWarning($"Safety warning: {result.Summary}");
        }

        _reporter.ReportInfo($"[{Name}] Message passed safety evaluation with {result.RuleResults.Count} rule result(s).");

        return taggedMessage;
    }








    private static ChatMessage ApplySafetyTags(ChatMessage message, SafetyEvaluationResult result)
    {
        if (result.TotalScore <= 0 && result.IsAllowed)
        {
            return message;
        }

        string safetyResult = result.IsAllowed ? "warn" : "blocked";
        return SafetyTagger.Attach(message, result.TotalScore, safetyResult);
    }
}
