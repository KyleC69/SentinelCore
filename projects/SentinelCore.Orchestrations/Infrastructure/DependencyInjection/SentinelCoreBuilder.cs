// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         SentinelCoreBuilder.cs
// Author: Kyle L. Crowder
// Build Num:  080801



using Microsoft.Extensions.DependencyInjection;

using SentinelCore.DependencyInjection;




namespace SentinelCore.Infrastructure.DependencyInjection;





/// <summary>
///     Default implementation of <see cref="ISentinelCoreBuilder" />.
///     Registers core services and validates module dependency chains.
///     Persistence is always-on and no longer requires explicit opt-in.
/// </summary>
public class SentinelCoreBuilder : ISentinelCoreBuilder
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SentinelCoreBuilder" /> class.
    /// </summary>
    /// <param name="services">The service collection being configured.</param>
    public SentinelCoreBuilder(IServiceCollection services)
    {
        Services = services;
    }








    internal bool GroupOrchestrationEnabled { get; private set; }

    public bool MagneticOrchestrationEnabled { get; private set; }

    public bool InvestigationControlEnabled { get; set; }

    public IServiceCollection Services { get; }
}