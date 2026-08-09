// Solution: SentinelCore
// Project:   SentinelCore.CaseFlowEngine
// File:         DesignTimeSentinelCoreDBContextFactory.cs
// Author: Kyle L. Crowder
// Build Num:  080801



using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;




namespace SentinelCore.CaseFlowEngine.Persistence;





/// <summary>
///     Provides a design‑time <see cref="SentinelCoreDBContext" /> for EF Core tools.
///     The runtime host supplies the connection string via <see cref="SentinelCoreSettings.SqlConnectionString" />,
///     but design‑time operations (e.g., <c>dotnet ef migrations</c>) need a concrete connection string.
///     This factory attempts to read the connection string from the environment variable
///     <c>SENTINELCORE_CONNECTIONSTRING</c>. If the variable is not set, it falls back to a LocalDB
///     instance which works for development and CI environments.
/// </summary>
public sealed class DesignTimeSentinelCoreDBContextFactory : IDesignTimeDbContextFactory<SentinelCoreDBContext>
{
    private const string EnvVar = "SENTINEL_CORE";








    public SentinelCoreDBContext CreateDbContext(string[] args)
    {
        string connectionString = Environment.GetEnvironmentVariable(EnvVar) ?? "Server=(localdb)\\mssqllocaldb;Database=SentinelCore;Trusted_Connection=True;MultipleActiveResultSets=true";

        DbContextOptionsBuilder<SentinelCoreDBContext> optionsBuilder = new();
        optionsBuilder.UseSqlServer(connectionString);
        return new SentinelCoreDBContext(optionsBuilder.Options);
    }
}