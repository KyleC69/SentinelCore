// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         PatternMemoryContributor.cs
// Author: Kyle L. Crowder
// Build Num:  092308



using Microsoft.Extensions.Logging;

using SentinelCore.Orchestrations.Agents.AgentPresets;
using SentinelCore.Orchestrations.Agents.Middleware;




namespace SentinelCore.Orchestrations.Agents;





/// <summary>
///     Adds the <see cref="PatternMemoryInjector" /> context provider when the preset's
///     <see cref="AgentPresetDefinition.UsePatternMemory" /> flag is set.
///     Per PL-3, pattern memory is only applied to the Core agent.
/// </summary>
public sealed class PatternMemoryContributor : IAgentConstructionContributor
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly IPatternMatcher _patternMatcher;








    /// <summary>
    ///     Initializes a new instance of the <see cref="PatternMemoryContributor" /> class.
    /// </summary>
    /// <param name="patternMatcher">The pattern matcher for searching pattern memory.</param>
    /// <param name="loggerFactory">The logger factory for creating loggers.</param>
    /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
    public PatternMemoryContributor(IPatternMatcher patternMatcher, ILoggerFactory loggerFactory)
    {
        _patternMatcher = patternMatcher ?? throw new ArgumentNullException(nameof(patternMatcher));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }








    /// <inheritdoc />
    public Task ContributeAsync(AgentConstructionContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Preset.UsePatternMemory)
        {
            context.ContextProviders.Add(new PatternMemoryInjector(_patternMatcher, _loggerFactory.CreateLogger<PatternMemoryInjector>()));
        }

        return Task.CompletedTask;
    }








    /// <inheritdoc />
    public int Order
    {
        get => 30;
    }
}