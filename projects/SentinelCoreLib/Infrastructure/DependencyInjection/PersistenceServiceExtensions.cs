// Solution: SentinelCoreLib
// Project:   SentinelCoreLib
// File:         PersistenceServiceExtensions.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using SentinelCoreLib.Application.Abstractions.Persistence;
using SentinelCoreLib.Infrastructure.Persistence;




namespace SentinelCoreLib.Infrastructure.DependencyInjection;





/// <summary>
///     Provides extension methods for registering SentinelCore persistence services.
/// </summary>
public static class PersistenceServiceExtensions
{
    /// <summary>
    ///     Adds Entity Framework Core SQL Server persistence if a connection string is provided.
    ///     When the connection string is missing, registers a no-op repository so the host can still start.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The SQL Server connection string. May be null or empty to disable persistence.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddSentinelCorePersistence(this IServiceCollection services, string? connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            ILogger logger = services.BuildServiceProvider().GetRequiredService<ILoggerFactory>().CreateLogger("PersistenceServiceExtensions");
            logger.LogWarning("No connection string provided for SentinelCore persistence. Using no-op repository.");
            return services;
        }

        services.AddDbContext<SentinelCoreDbContext>(options => options.UseSqlServer(connectionString, sql => sql.MigrationsAssembly(typeof(SentinelCoreDbContext).Assembly.GetName().Name)));

        services.AddSingleton<ICaseRepository, CaseRepository>();
        services.AddSingleton<IEvidenceStore, EvidenceStore>();
        services.AddSingleton<IPatternMemoryStore, PatternMemoryStore>();

        return services;
    }
}