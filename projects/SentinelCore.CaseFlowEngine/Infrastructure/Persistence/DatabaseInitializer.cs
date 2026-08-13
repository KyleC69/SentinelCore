// Solution: SentinelCore
// Project:   SentinelCore.CaseFlowEngine
// File:         DatabaseInitializer.cs
// Author: Kyle L. Crowder
// Build Num:  081312



using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using SentinelCore.CaseFlowEngine.Persistence;




namespace SentinelCore.Infrastructure.Persistence;





/// <summary>
///     Hosted service responsible for initializing the SentinelCore database.
/// </summary>
/// <remarks>
///     This class ensures that all necessary database tables are created before the application starts processing
///     requests.
///     This approach ensures that the database schema is up-to-date and ready for use.
/// </remarks>
[Obsolete("This is unnecessary - use *.sqlproj for persistence")]
public sealed class DatabaseInitializer : IHostedService
{
    private readonly ILogger<DatabaseInitializer> _logger;
    private readonly IServiceScopeFactory _scopeFactory;








    /// <summary>
    ///     Initializes a new instance of the <see cref="DatabaseInitializer" /> class.
    /// </summary>
    public DatabaseInitializer(IServiceScopeFactory scopeFactory, ILogger<DatabaseInitializer> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }








    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Ensuring SentinelCore database tables exist…");

        using IServiceScope scope = _scopeFactory.CreateScope();
        SentinelCoreDBContext context = scope.ServiceProvider.GetRequiredService<SentinelCoreDBContext>();
        await context.Database.MigrateAsync(cancellationToken);
        _logger.LogInformation("SentinelCore database tables are ready.");
    }








    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}