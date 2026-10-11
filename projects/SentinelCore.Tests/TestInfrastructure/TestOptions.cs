// Solution: SentinelCore
// Project:   SentinelCore.Tests
// File:         TestOptions.cs
// Author: Kyle L. Crowder
// Build Num:  100908



using Microsoft.Extensions.Options;




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






}