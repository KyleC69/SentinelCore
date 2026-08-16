// Solution: SentinelCore
// Project:   SentinelCore.Contracts
// File:         ISentinelCoreBuilder.cs
// Author: Kyle L. Crowder
// Build Num:  081602



using Microsoft.Extensions.DependencyInjection;




namespace SentinelCore.DependencyInjection;





/// <summary>
///     Configures optional SentinelCore modules during host startup.
///     Persistence is always-on and no longer requires explicit opt-in.
/// </summary>
public interface ISentinelCoreBuilder
{
    /// <summary>
    ///     Gets or sets a value indicating whether investigation control has been enabled.
    /// </summary>
    bool InvestigationControlEnabled { get; set; }

    /// <summary>
    ///     The service collection being configured.
    /// </summary>
    IServiceCollection Services { get; }
}