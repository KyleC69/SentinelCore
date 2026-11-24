// Solution: SentinelCore
// Project:   SentinelCore.CaseFlowEngine
// File:         DesignTimeSentinelCoreDBContextFactory.cs
// Author: Kyle L. Crowder
// Build Num:  112101



using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

using SentinelCore.CaseFlowEngine.DBEntities;




namespace SentinelCore.CaseFlowEngine.Persistence;





/// <summary>
///     Provides a design‑time <see cref="SentinelCoreDBContext" /> for EF Core tools.
///     The runtime host supplies the connection string via
///     <see cref="SentinelCore.Contracts.Contracts.SentinelCoreSettings.SqlConnectionString" />,
///     but design‑time operations (e.g., <c>dotnet ef migrations</c>) need a concrete connection string.
///     This factory reads the connection string from the following sources in priority order:
///     1. Environment variable <c>SENTINELCORE_CONNECTIONSTRING</c>
///     2. User secrets (via <c>dotnet user-secrets</c>)
///     3. <c>appsettings.json</c> / <c>appsettings.Development.json</c> in the project directory
///     4. Falls back to a LocalDB instance for development and CI environments
/// </summary>
public sealed class DesignTimeSentinelCoreDbContextFactory : IDesignTimeDbContextFactory<SentinelCoreDBContext>
{
    private const string ConfigKey = "SentinelCore:SqlConnectionString";
    private const string EnvVar = "SENTINELCORE_CONNECTIONSTRING";








    public SentinelCoreDBContext CreateDbContext(string[] args)
    {
        string connectionString = GetConnectionString();

        DbContextOptionsBuilder<SentinelCoreDBContext> optionsBuilder = new();
        optionsBuilder.UseSqlServer(connectionString);
        return new(optionsBuilder.Options);
    }








    private static IConfigurationRoot BuildConfiguration()
    {
        // Get the project directory (where the .csproj file is located)
        string projectDir = AppContext.BaseDirectory;

        // Navigate up to find the project root (where appsettings.json would be)
        // In design-time context, we're typically in bin/Debug/net10.0/
        DirectoryInfo? directory = new(projectDir);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SentinelCore.CaseFlowEngine.csproj")))
        {
            directory = directory.Parent;
        }

        string basePath = directory?.FullName ?? projectDir;

        return new ConfigurationBuilder().SetBasePath(basePath).AddJsonFile("appsettings.json", optional: true, reloadOnChange: false).AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false).AddUserSecrets<DesignTimeSentinelCoreDbContextFactory>(optional: true).AddEnvironmentVariables().Build();
    }








    private static string GetConnectionString()
    {
        // 1. Environment variable (highest priority for CI/CD)
        string? envConnectionString = Environment.GetEnvironmentVariable(EnvVar);
        if (!string.IsNullOrWhiteSpace(envConnectionString))
        {
            return envConnectionString;
        }

        // 2. Build configuration from appsettings.json and user secrets
        IConfigurationRoot configuration = BuildConfiguration();

        // 3. Check user secrets / appsettings for connection string
        string? configConnectionString = configuration[ConfigKey];
        if (!string.IsNullOrWhiteSpace(configConnectionString))
        {
            return configConnectionString;
        }

        // 4. Fallback to LocalDB (works for development and CI)
        return "Server=(localdb)\\mssqllocaldb;Database=SentinelCore;Trusted_Connection=True;MultipleActiveResultSets=true";
    }
}