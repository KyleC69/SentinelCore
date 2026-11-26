// Solution: SentinelCore
// Project:   SentinelCore.Security
// File:         SentinelSecurityServiceExtensions.cs
// Author: Kyle L. Crowder
// Build Num:  112518



using Microsoft.Extensions.DependencyInjection;

using SentinelCore.Security.Cryptography;
using SentinelCore.Security.Quarantine;




namespace SentinelCore.Security.Infrastructure.DependencyInjection;





/// <summary>
///     Registers the message-provenance security module. All services are registered as
///     singletons so the replay cache and the quarantine store are shared across every
///     consumer in the process.
/// </summary>
/// <remarks>
///     The quarantine store resolves
///     <see cref="Microsoft.Extensions.Logging.ILogger{TCategoryName}" /> for
///     structured quarantine logging.
/// </remarks>
public static class SentinelSecurityServiceExtensions
{

    /// <summary>
    ///     Adds the message-provenance security services to the collection.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configure">Optional configuration of <see cref="SentinelSecurityOptions" />.</param>
    /// <returns>The updated service collection.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services" /> is <c>null</c>.</exception>
    public static IServiceCollection AddSentinelSecurity(this IServiceCollection services, Action<SentinelSecurityOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        SentinelSecurityOptions options = new();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddSingleton<ISentinelKeyProvider, EnvironmentSentinelKeyProvider>();
        services.AddSingleton<ISentinelProvenanceService, SentinelProvenanceService>();
        services.AddSingleton<IQuarantineStore, SecureQuarantineStore>();

        return services;
    }
}