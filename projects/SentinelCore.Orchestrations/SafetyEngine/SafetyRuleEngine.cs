// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         SafetyRuleEngine.cs
// Author: Kyle L. Crowder
// Build Num:  092308



using Microsoft.Extensions.Logging;

using SentinelCore.Contracts.Abstractions;




namespace SentinelCore.Orchestrations.SafetyEngine;





/// <summary>
///     Orchestrates the evaluation of multiple <see cref="ISafetyRule" /> instances and aggregates their results.
///     The engine processes rules sequentially, short-circuiting on block if configured, and produces
///     a combined <see cref="SafetyEvaluationResult" /> that summarizes all rule evaluations.
/// </summary>
public sealed class SafetyRuleEngine
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly SafetyEngineOptions _options;
    private readonly ISystemReporter? _reporter;








    /// <summary>
    ///     Initializes a new instance of the <see cref="SafetyRuleEngine" />.
    /// </summary>
    /// <param name="rules">The collection of safety rules to evaluate.</param>
    /// <param name="loggerFactory">The logger factory for creating loggers.</param>
    /// <param name="options">Configuration options for the engine.</param>
    public SafetyRuleEngine(IReadOnlyList<ISafetyRule> rules, ILoggerFactory loggerFactory, SafetyEngineOptions? options = null) : this(rules, loggerFactory, reporter: null, options)
    {
    }








    /// <summary>
    ///     Initializes a new instance of the <see cref="SafetyRuleEngine" />.
    /// </summary>
    /// <param name="rules">The collection of safety rules to evaluate.</param>
    /// <param name="loggerFactory">The logger factory for creating loggers.</param>
    /// <param name="reporter">Optional reporter used for workflow-level warnings.</param>
    /// <param name="options">Configuration options for the engine.</param>
    public SafetyRuleEngine(IReadOnlyList<ISafetyRule> rules, ILoggerFactory loggerFactory, ISystemReporter? reporter, SafetyEngineOptions? options = null)
    {
        Rules = rules ?? throw new ArgumentNullException(nameof(rules));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _options = options ?? SafetyEngineOptions.Default;
        _reporter = reporter;
    }








    private ILogger<SafetyRuleEngine> Logger
    {
        get => _loggerFactory.CreateLogger<SafetyRuleEngine>();
    }

    /// <summary>
    ///     Gets the configured rules.
    /// </summary>
    public IReadOnlyList<ISafetyRule> Rules { get; }








    /// <summary>
    ///     Evaluates rules synchronously and returns the aggregate result.
    /// </summary>
    /// <param name="context">The evaluation context.</param>
    /// <returns>The aggregate evaluation result.</returns>
    public SafetyEvaluationResult Evaluate(SafetyEvaluationContext context)
    {
        //don't use ILogger directly
        // Logger.LogDebug("Starting synchronous safety rule evaluation with {RuleCount} rules", Rules.Count);
        _reporter?.ReportInfo("Starting Safety rule engine");
        List<SafetyRuleResult> results = new(Rules.Count);

        foreach (ISafetyRule rule in Rules)
        {
            try
            {
                SafetyRuleResult result = rule.EvaluateAsync(context).GetAwaiter().GetResult();
                results.Add(result);

                if (result.Action == SafetyAction.Block && _options.StopOnFirstBlock)
                {
                    break;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Rule '{RuleName}' threw an exception during synchronous evaluation", rule.Name);

                if (_options.TreatRuleErrorsAsBlocks)
                {
                    results.Add(SafetyRuleResult.Block(rule.Name, SafetySeverity.Critical, $"Rule evaluation failed: {ex.Message}"));
                    break;
                }

                results.Add(SafetyRuleResult.Warn(rule.Name, SafetySeverity.Medium, $"Rule evaluation failed: {ex.Message}"));
            }
        }

        return SafetyEvaluationResult.FromResults(results);
    }








    /// <summary>
    ///     Evaluates all rules against the given context and returns the aggregate result.
    /// </summary>
    /// <param name="context">The evaluation context containing the messages to inspect.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The aggregate evaluation result.</returns>
    public async Task<SafetyEvaluationResult> EvaluateAsync(SafetyEvaluationContext context, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Starting safety rule evaluation with {RuleCount} rules", Rules.Count);

        List<SafetyRuleResult> results = new(Rules.Count);

        foreach (ISafetyRule rule in Rules)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                Logger.LogDebug("Evaluation cancelled");
                break;
            }

            try
            {
                Logger.LogTrace("Evaluating rule: {RuleName}", rule.Name);
                SafetyRuleResult result = await rule.EvaluateAsync(context, cancellationToken).ConfigureAwait(false);
                results.Add(result);

                Logger.LogDebug("Rule {RuleName} result: {Action} (Severity: {Severity})", rule.Name, result.Action, result.Severity);

                // Short-circuit on block
                if (result.Action == SafetyAction.Block && _options.StopOnFirstBlock)
                {
                    //ALL BLOCKED MESSAGES SHOULD BE TAGGED LIKE SO
                    //   message.WithAgentRequestMessageSource(new AgentRequestMessageSourceType("SafetyResult"), "blocked");
                    // TODO: All messages should have a score attached.
                    //   message.WithAgentRequestMessageSource(new AgentRequestMessageSourceType("SafetyScore"), "6");

                    //SYSTEMREPORTER IS USED FOR ALL OUTPUT IT IS AN AGGREGATE LOGGER
                    _reporter?.ReportWarning($"[{rule.Name}]- Stopping evaluation early due to block from rule: {rule.Name}");

                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Logger.LogError(ex, "Rule '{RuleName}' threw an unhandled exception during evaluation", rule.Name);

                if (_options.TreatRuleErrorsAsBlocks)
                {
                    results.Add(SafetyRuleResult.Block(rule.Name, SafetySeverity.Critical, $"Rule evaluation failed with exception: {ex.Message}"));
                    break;
                }

                results.Add(SafetyRuleResult.Warn(rule.Name, SafetySeverity.Medium, $"Rule evaluation failed: {ex.Message}"));
            }
        }

        SafetyEvaluationResult aggregateResult = SafetyEvaluationResult.FromResults(results);

        Logger.LogDebug("Safety evaluation complete: IsAllowed={IsAllowed}, HighestSeverity={Severity}, Summary={Summary}", aggregateResult.IsAllowed, aggregateResult.HighestSeverity, aggregateResult.Summary);

        return aggregateResult;
    }
}
