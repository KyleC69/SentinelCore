// Solution: SentinelCore
// Project:   SentinelCore.Tests
// File:         TestOptions.cs
// Author: Kyle L. Crowder
// Build Num:  091300



using Microsoft.Extensions.Options;

using SentinelCore.Contracts.Contracts;




namespace SentinelCore.Tests.TestInfrastructure;





/// <summary>
///     Provides <see cref="IOptions{TOptions}" /> wrappers for unit tests.
/// </summary>
public static class TestOptions
{

    /// <summary>
    ///     The logical agent names the tests seed per-agent models for.
    /// </summary>
    public static readonly string[] CatalogAgents =
    [
            "CoreChat",
            "Classifier",
            "TheCore",
            "SafetyAgent",
            "Manager",
            "Worker1",
            "Worker2",
            "Worker3",
            "CaseGenerator"
    ];








    /// <summary>
    ///     Creates an <see cref="IOptions{SentinelCoreSettings}" /> wrapping the given settings.
    /// </summary>
    public static IOptions<SentinelCoreSettings> Create(SentinelCoreSettings? settings = null)
    {
        settings ??= new SentinelCoreSettings { DefaultModel = new ModelProfile("http://default", "default-model", 0.1f) };

        // Seed per-agent entries for the catalog agents so builds resolve without
        // relying on role tiers — mirroring a fully configured Model Configuration page.
        foreach (string agent in CatalogAgents)
        {
            settings.AgentModels[agent] = new ModelProfile("http://test", $"test-model-{agent.ToLowerInvariant()}", 0.1f);
        }

        return Options.Create(settings);
    }
}