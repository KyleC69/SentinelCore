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
        settings ??= new SentinelCoreSettings { DefaultModel = ModelProfile.Glm5(), DefaultUtilityModel = ModelProfile.Gpt20() };
        return Options.Create(settings);
    }
}