// Solution: SentinelCore
// Project:   SentinelCore.Tests
// File:         TestOptions.cs
// Author: Kyle L. Crowder
// Build Num:  082808



using Microsoft.Extensions.Options;




namespace SentinelCore.Tests.TestInfrastructure;





/// <summary>
///     Provides <see cref="IOptions{TOptions}" /> wrappers for unit tests.
/// </summary>
public static class TestOptions
{
    /// <summary>
    ///     Creates an <see cref="IOptions{SentinelCoreSettings}" /> wrapping the given settings.
    /// </summary>
    public static IOptions<SentinelCoreSettings> Create(SentinelCoreSettings? settings = null)
    {
        settings ??= new SentinelCoreSettings
        {
            DefaultModel = ModelProfile.Glm5(),
            ManagerModel = ModelProfile.Gpt120(),
            DefaultUtilityModel = ModelProfile.Gpt20()
        };

        // Seed per-agent entries for the catalog agents so builds resolve without
        // relying on role tiers — mirroring a fully configured Model Configuration page.
        foreach (string agent in CatalogAgents)
        {
            settings.AgentModels[agent] = agent is "Manager"
                ? ModelProfile.Gpt120()
                : ModelProfile.Glm5();
        }

        return Options.Create(settings);
    }





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
}
