// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         SafetyEngineAgentBuilderExtensions.cs
// Author: Kyle L. Crowder
// Build Num:  080801



using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;




namespace SentinelCore.SafetyEngine;





/// <summary>
///     Extension methods for registering the <see cref="SafetyEngineAgent" /> as middleware
///     in the <see cref="AIAgentBuilder" /> pipeline.
/// </summary>
public static class SafetyEngineAgentBuilderExtensions
{
    /// <summary>
    ///     Adds a safety engine middleware to the agent pipeline.
    ///     This acts as a pre-filter gate that inspects prompts before they reach the AI model.
    /// </summary>
    /// <param name="builder">The agent builder.</param>
    /// <param name="rules">The safety rules to evaluate.</param>
    /// <param name="logger">The logger for the safety engine agent.</param>
    /// <param name="options">Optional configuration options.</param>
    /// <returns>The builder for continued chaining.</returns>
    /// <example>
    ///     <code>
    /// var safeAgent = new AIAgentBuilder(innerAgent)
    ///     .UseSafetyEngine(rules, logger)
    ///     .Build();
    /// </code>
    /// </example>
    public static AIAgentBuilder UseSafetyEngine(this AIAgentBuilder builder, IReadOnlyList<ISafetyRule> rules, ILogger<SafetyEngineAgent> logger, SafetyEngineOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(logger);

        SafetyEngineAgent engine = new(rules, logger, options);

        return builder.Use((messages, session, options2, innerAgent, cancellationToken) => engine.InterceptRunAsync(messages, session, options2, innerAgent, cancellationToken), (messages, session, options2, innerAgent, cancellationToken) => engine.InterceptRunStreamingAsync(messages, session, options2, innerAgent, cancellationToken));
    }








    /// <summary>
    ///     Adds a safety engine middleware to the agent pipeline
    ///     with a logger resolved from an <see cref="IServiceProvider" />.
    /// </summary>
    /// <param name="builder">The agent builder.</param>
    /// <param name="rules">The safety rules to evaluate.</param>
    /// <param name="serviceProvider">
    ///     The service provider for resolving dependencies (e.g.,
    ///     <see cref="ILogger{SafetyEngineAgent}" />).
    /// </param>
    /// <param name="options">Optional configuration options.</param>
    /// <returns>The builder for continued chaining.</returns>
    public static AIAgentBuilder UseSafetyEngine(this AIAgentBuilder builder, IReadOnlyList<ISafetyRule> rules, IServiceProvider serviceProvider, SafetyEngineOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        ILogger<SafetyEngineAgent> logger = serviceProvider.GetService<ILogger<SafetyEngineAgent>>() ?? throw new InvalidOperationException($"No logger of type {typeof(ILogger<SafetyEngineAgent>).Name} is registered in the service provider. " + "Ensure Microsoft.Extensions.Logging is configured in your DI container.");

        return builder.UseSafetyEngine(rules, logger, options);
    }
}