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
public sealed class SafetyExecutor : Executor<ChatMessage, ChatMessage>
{
    // TODO: Remove pragma when safety agent creation is implemented
#pragma warning disable S1144 // Unused private field - reserved for future safety agent creation
    private readonly ISentinelAgentFactory _agentFactory;
#pragma warning restore S1144
    private readonly ILogger<SafetyExecutor> _logger;
    private readonly ISystemReporter _reporter;
    private readonly SafetyRuleEngine _ruleEngine;








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

        // Initialize the rule engine with default safety rules
        IReadOnlyList<ISafetyRule> rules = CreateDefaultRules();
        _ruleEngine = new SafetyRuleEngine(rules, loggerFactory);
    }








 
    public override async ValueTask<ChatMessage> HandleAsync(ChatMessage message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        _reporter.ReportInfo("Starting Safety filter");
        _logger.LogDebug("SafetyExecutor processing message");

        try
        {
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
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Safety evaluation was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during safety evaluation");
            _reporter.ReportError(ex, "Safety evaluation error");
        }

        _reporter.ReportInfo("Leaving safety exec");
        return message;
    }








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