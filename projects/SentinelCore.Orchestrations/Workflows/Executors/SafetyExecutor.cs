// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         SafetyExecutor.cs
// Author: Kyle L. Crowder
// Build Num:  091418

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
    // TODO: Remove pragma when safety agent creation is implemented
#pragma warning disable S1144 // Unused private field - reserved for future safety agent creation
    private readonly ISentinelAgentFactory _agentFactory;
#pragma warning restore S1144
    private readonly ILogger<SafetyExecutor> _logger;
    private readonly ISystemReporter _reporter;
    private readonly SafetyRuleEngine _ruleEngine;

    /// <summary>
    ///     Gets the human-readable name of this executor, used in log messages and diagnostics.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SafetyExecutor" />.
    /// </summary>
    /// <param name="reporter">The system reporter for logging safety events.</param>
    /// <param name="agentFactory">The agent factory for creating safety agents.</param>
    /// <param name="loggerFactory">The logger factory for creating safety engine loggers.</param>
    /// <param name="logger">The logger for this executor.</param>
    public SafetyExecutor(ISystemReporter reporter, ISentinelAgentFactory agentFactory, ILoggerFactory loggerFactory, ILogger<SafetyExecutor> logger) : base("SafetyExecutor")
    {
        _reporter = reporter ?? throw new ArgumentNullException(nameof(reporter));
        _agentFactory = agentFactory ?? throw new ArgumentNullException(nameof(agentFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        Name = Id;

        // Initialize the rule engine with default safety rules
        IReadOnlyList<ISafetyRule> rules = CreateDefaultRules();
        _ruleEngine = new SafetyRuleEngine(rules, loggerFactory);
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
        _reporter.ReportInfo("Starting Safety filter");
        _logger.LogDebug("SafetyExecutor processing message");

        // Create evaluation context from the message
        IReadOnlyList<ChatMessage> messages = new List<ChatMessage> { message };
        SafetyEvaluationContext evalContext = new(messages);

        // Evaluate the message against safety rules
        SafetyEvaluationResult result = await _ruleEngine.EvaluateAsync(evalContext, cancellationToken).ConfigureAwait(false);

        if (!result.IsAllowed)
        {
            _logger.LogWarning("Message blocked by safety policy. Severity: {Severity}, Summary: {Summary}", result.HighestSeverity, result.Summary);
            _reporter.ReportWarning($"Safety block: {result.Summary}");

            // Return a blocked response message
            ChatMessage blockedMessage = new(ChatRole.Assistant, $"Request blocked by safety policy: {result.Summary}");
            return blockedMessage;
        }

        _logger.LogDebug("Message passed safety evaluation. Rule results: {ResultCount}", result.RuleResults.Count);
        _reporter.ReportInfo("Message passed safety filter");

        return message;
    }

    /// <summary>
    ///     Creates a fallback result when the executor encounters an error or receives null input.
    /// </summary>
    private ChatMessage CreateFallbackResult() => new(ChatRole.Assistant, "Safety evaluation could not be completed. The message has been allowed through as a precaution.");

    /// <summary>
    ///     Creates the default set of safety rules for evaluation.
    /// </summary>
    /// <returns>A read-only list of safety rules.</returns>
    private static IReadOnlyList<ISafetyRule> CreateDefaultRules()
    {
        return new List<ISafetyRule>
        {
                // Blocklist Rule - blocks specific terms
                new BlocklistRule("DefaultBlocklist", new[] { "malicious", "harmful", "exploit" }, SafetySeverity.High, "Blocks prompts containing blocklisted terms"),

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
}
