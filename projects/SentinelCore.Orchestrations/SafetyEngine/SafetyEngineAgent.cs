// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         SafetyEngineAgent.cs
// Author: Kyle L. Crowder
// Build Num:  080801



using Microsoft.Extensions.Logging;




namespace SentinelCore.SafetyEngine;





/// <remarks>
///     <para>
///         The <see cref="SafetyEngineAgent" /> is designed to enforce safety measures by
///         inspecting prompts and determining whether they should be blocked, warned, or allowed
///         based on the evaluation of configured <see cref="ISafetyRule" /> instances.
///     </para>
///     <para>
///         This middleware integrates into the <see cref="AIAgentBuilder" /> pipeline through
///         <see cref="SafetyEngineAgentBuilderExtensions.UseSafetyEngine" />, ensuring that all
///         prompts are evaluated for compliance with safety rules before reaching the AI model.
///         This guarantees that potentially harmful or inappropriate prompts are intercepted
///         early in the processing pipeline.
///     </para>
///     <para>
///         If any rule evaluation results in <see cref="SafetyAction.Block" />, the prompt is
///         intercepted, and a blocked response is returned instead of forwarding it to the inner
///         agent. This mechanism ensures that the AI model does not process or respond to
///         prompts that violate safety guidelines.
///     </para>
///     <para>
///         Example usage:
///         <code>
/// var safeAgent = new AIAgentBuilder(innerAgent)
///     .UseSafetyEngine(rules, logger)
///     .Build();
/// </code>
///         In this example, the safety engine is configured with a set of rules and a logger,
///         and it is added to the AI agent pipeline. The resulting agent will evaluate all
///         incoming prompts against the specified safety rules.
///     </para>
/// </remarks>
public sealed class SafetyEngineAgent
{

    private readonly ILogger<SafetyEngineAgent> _logger;
    private readonly SafetyEngineOptions _options;
    private readonly IReadOnlyList<ISafetyRule> _rules;

    /// <summary>
    ///     The key used to store <see cref="SafetyEvaluationResult" /> in
    ///     <see cref="AgentRunOptions.AdditionalProperties" /> so callers can inspect the result.
    /// </summary>
    public const string EvaluationResultKey = "SafetyEngine.EvaluationResult";








    /// <summary>
    ///     Creates a new <see cref="SafetyEngineAgent" /> with the specified rules and options.
    /// </summary>
    /// <param name="rules">The safety rules to evaluate.</param>
    /// <param name="logger">A logger for this agent.</param>
    /// <param name="options">Configuration options.</param>
    public SafetyEngineAgent(IReadOnlyList<ISafetyRule> rules, ILogger<SafetyEngineAgent> logger, SafetyEngineOptions? options = null)
    {
        _rules = rules ?? throw new ArgumentNullException(nameof(rules));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? SafetyEngineOptions.Default;
    }








    /// <summary>
    ///     Creates a blocked <see cref="AgentResponse" /> for the given evaluation result.
    /// </summary>
    /// <param name="evaluationResult">The evaluation result that caused the block.</param>
    /// <returns>A response indicating the prompt was blocked.</returns>
    public AgentResponse CreateBlockedResponse(SafetyEvaluationResult evaluationResult)
    {
        ChatMessage blockMessage = new(ChatRole.Assistant, _options.BlockedResponseMessage ?? $"Request blocked by safety policy: {evaluationResult.Summary}");

        return new AgentResponse(blockMessage);
    }








    /// <summary>
    ///     Creates a blocked <see cref="AgentResponseUpdate" /> for the given evaluation result.
    /// </summary>
    /// <param name="evaluationResult">The evaluation result that caused the block.</param>
    /// <returns>A response update indicating the prompt was blocked.</returns>
    public AgentResponseUpdate CreateBlockedResponseUpdate(SafetyEvaluationResult evaluationResult)
    {
        string message = _options.BlockedResponseMessage ?? $"Request blocked by safety policy: {evaluationResult.Summary}";
        return new AgentResponseUpdate(ChatRole.Assistant, message);
    }








    /// <summary>
    ///     Evaluates all rules against the given context and returns the aggregate result.
    /// </summary>
    /// <param name="context">The evaluation context containing the messages to inspect.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The aggregate evaluation result.</returns>
    public async Task<SafetyEvaluationResult> EvaluateRulesAsync(SafetyEvaluationContext context, CancellationToken cancellationToken)
    {
        List<SafetyRuleResult> results = new(_rules.Count);

        foreach (ISafetyRule rule in _rules)
            try
            {
                _logger.LogDebug("Evaluating safety rule: {RuleName}", rule.Name);
                SafetyRuleResult result = await rule.EvaluateAsync(context, cancellationToken);
                results.Add(result);

                // Short-circuit: if a rule blocks and we're configured to stop on first block,
                // skip remaining rules.
                if (result.Action == SafetyAction.Block && _options.StopOnFirstBlock)
                {
                    _logger.LogDebug("Stopping evaluation early due to block from rule: {RuleName}", rule.Name);
                    break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Safety rule '{RuleName}' threw an exception during evaluation.", rule.Name);

                if (_options.TreatRuleErrorsAsBlocks)
                {
                    results.Add(SafetyRuleResult.Block(rule.Name, SafetySeverity.Critical, $"Rule evaluation failed: {ex.Message}"));
                    break;
                }

                results.Add(SafetyRuleResult.Warn(rule.Name, SafetySeverity.Medium, $"Rule evaluation failed: {ex.Message}"));
            }

        return SafetyEvaluationResult.FromResults(results);
    }








    /// <summary>
    ///     Inspects messages through the safety engine and either blocks or forwards to the inner agent.
    ///     This is the core middleware logic used by the <see cref="AIAgentBuilder.Use" /> pipeline.
    /// </summary>
    /// <param name="messages">The incoming messages.</param>
    /// <param name="session">The agent session.</param>
    /// <param name="options">The run options.</param>
    /// <param name="innerAgent">The inner agent to forward to if allowed.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The agent response, either a blocked response or the inner agent's response.</returns>
    public async Task<AgentResponse> InterceptRunAsync(IEnumerable<ChatMessage> messages,
            AgentSession? session, //throwing null all of a sudden. cause unknown
            AgentRunOptions? options,
            AIAgent innerAgent,
            CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(options);

        IList<ChatMessage> messageList = messages as IList<ChatMessage> ?? messages.ToList();
        SafetyEvaluationContext context = new((IReadOnlyList<ChatMessage>)messageList);

        SafetyEvaluationResult evaluationResult = await EvaluateRulesAsync(context, cancellationToken);

        // Attach the evaluation result to options AdditionalProperties so callers can inspect it.
        options.AdditionalProperties ??= new AdditionalPropertiesDictionary();
        options.AdditionalProperties[EvaluationResultKey] = evaluationResult;

        if (!evaluationResult.IsAllowed)
        {
            _logger.LogWarning("Prompt blocked by safety engine. Rule: {RuleName}. Reason: {Reason}. Severity: {Severity}", evaluationResult.BlockingResult?.RuleName, evaluationResult.BlockingResult?.Reason, evaluationResult.HighestSeverity);

            return CreateBlockedResponse(evaluationResult);
        }

        if (evaluationResult.HighestSeverity >= SafetySeverity.Medium)
        {
            _logger.LogInformation("Prompt passed with warnings. Highest severity: {Severity}. Summary: {Summary}", evaluationResult.HighestSeverity, evaluationResult.Summary);
        }

        // Prompt is allowed — forward to the inner agent.
        return await innerAgent.RunAsync(messageList, session, options, cancellationToken);
    }








    /// <summary>
    ///     Inspects messages through the safety engine and either blocks or forwards to the inner agent (streaming).
    ///     This is the core middleware logic used by the <see cref="AIAgentBuilder.Use" /> pipeline.
    /// </summary>
    /// <param name="messages">The incoming messages.</param>
    /// <param name="session">The agent session.</param>
    /// <param name="options">The run options.</param>
    /// <param name="innerAgent">The inner agent to forward to if allowed.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>An async enumerable of response updates.</returns>
    public async IAsyncEnumerable<AgentResponseUpdate> InterceptRunStreamingAsync(IEnumerable<ChatMessage> messages, AgentSession? session, AgentRunOptions? options, AIAgent innerAgent, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(options);

        IList<ChatMessage> messageList = messages as IList<ChatMessage> ?? messages.ToList();
        SafetyEvaluationContext context = new((IReadOnlyList<ChatMessage>)messageList);

        SafetyEvaluationResult evaluationResult = await EvaluateRulesAsync(context, cancellationToken);

        // Attach the evaluation result to options AdditionalProperties so callers can inspect it.
        options.AdditionalProperties ??= new AdditionalPropertiesDictionary();
        options.AdditionalProperties[EvaluationResultKey] = evaluationResult;

        if (!evaluationResult.IsAllowed)
        {
            _logger.LogWarning("Prompt blocked by safety engine (streaming). Rule: {RuleName}. Reason: {Reason}. Severity: {Severity}", evaluationResult.BlockingResult?.RuleName, evaluationResult.BlockingResult?.Reason, evaluationResult.HighestSeverity);

            yield return CreateBlockedResponseUpdate(evaluationResult);
            yield break;
        }

        if (evaluationResult.HighestSeverity >= SafetySeverity.Medium)
        {
            _logger.LogInformation("Prompt passed with warnings (streaming). Highest severity: {Severity}. Summary: {Summary}", evaluationResult.HighestSeverity, evaluationResult.Summary);
        }

        // Prompt is allowed — forward to the inner agent.
        await foreach (AgentResponseUpdate update in innerAgent.RunStreamingAsync(messageList, session, options, cancellationToken)) yield return update;
    }
}