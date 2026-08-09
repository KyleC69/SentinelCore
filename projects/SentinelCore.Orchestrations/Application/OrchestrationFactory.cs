// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         OrchestrationFactory.cs
// Author: Kyle L. Crowder
// Build Num:  080801



using Microsoft.Extensions.DependencyInjection;

using SentinelCore.Abstractions;
using SentinelCore.Workflows;

using System.Diagnostics.CodeAnalysis;




namespace SentinelCore.Application;





/// <summary>
///     Factory for creating orchestration instances.
/// </summary>
public interface IOrchestrationFactory
{
    /// <summary>
    ///     Creates a workflow instance for the specified orchestration type.
    /// </summary>
    /// <param name="orchestrationType">The requested orchestration type.</param>
    /// <returns>The configured workflow.</returns>
    IOrchestration CreateOrchestrationInstance(OrchestrationType orchestrationType);
}





/// <summary>
///     Factory for creating orchestration instances based on the configured <see cref="OrchestrationType" />.
/// </summary>
/// <remarks>
///     This factory uses dependency injection to resolve the appropriate orchestration implementation.
/// </remarks>
public sealed class OrchestrationFactory : IOrchestrationFactory
{
    private readonly IServiceProvider _serviceProvider;








    /// <summary>
    ///     Initializes a new instance of the <see cref="OrchestrationFactory" /> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve orchestrations.</param>
    public OrchestrationFactory([NotNull] IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        _serviceProvider = serviceProvider;
    }








    /// <summary>
    ///     Creates a workflow instance for the specified orchestration type.
    ///     Multi orchestration hook
    /// </summary>
    /// <param name="orchestrationType"></param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    public IOrchestration CreateOrchestrationInstance(OrchestrationType orchestrationType)
    {
        return orchestrationType switch
        {
            // TheCore Orchestration is the default orchestration that is used to manage the core functionality of the system.
            // A very specialized multi-agent investigation type orchestration to interrogate Windows systems.
            OrchestrationType.TheCore => _serviceProvider.GetRequiredService<TheCoreWorkflow>(),
            OrchestrationType.CustomGroup => _serviceProvider.GetRequiredService<CustomGroupWorkflow>()
        };
    }
}