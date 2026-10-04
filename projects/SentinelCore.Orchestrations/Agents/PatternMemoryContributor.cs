// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         PatternMemoryContributor.cs
// Author: Kyle L. Crowder
// Build Num:  100310



using Microsoft.Extensions.Logging;

using SentinelCore.Orchestrations.Agents.AgentPresets;
using SentinelCore.Orchestrations.Agents.Middleware;




namespace SentinelCore.Orchestrations.Agents;





/// <summary>
///     Adds the <see cref="PatternMemoryInjector" /> context provider when the preset's
///     <see cref="AgentPresetDefinition.UsePatternMemory" /> flag is set.
///     Per PL-3, pattern memory is only applied to the Core agent.
/// </summary>
public sealed class PatternMemoryContributor
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








    /// <summary>
    /// Adds a pattern-memory context provider to the agent construction context when enabled by the preset.
    /// </summary>
    /// <remarks>No asynchronous work is performed. When context.Preset.UsePatternMemory is true, a
    /// PatternMemoryInjector is added to ContextProviders.</remarks>
    /// <param name="context">The agent construction context whose ContextProviders collection may be updated.</param>
    /// <param name="cancellationToken">A cancellation token to observe for operation cancellation.</param>
    /// <returns>A completed Task representing the operation.</returns>
    public Task ContributeAsync(AgentConstructionContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Preset.UsePatternMemory)
        {
            context.ContextProviders.Add(new PatternMemoryInjector(_patternMatcher, _loggerFactory.CreateLogger<PatternMemoryInjector>()));
        }

        return Task.CompletedTask;
    }









    public int Order
    {
        get => 30;
    }








    /// <summary>
    ///     Contributes to the agent construction context. The contributor inspects
    ///     the preset and adds its piece (client wrappers, context providers, tools).
    /// </summary>
    /// <param name="context">The accumulator for construction decisions.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task ContributeAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}