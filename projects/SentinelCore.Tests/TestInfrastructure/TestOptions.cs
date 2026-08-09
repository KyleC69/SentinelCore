// Solution: SentinelCore
// Project:   SentinelCore.Tests
// File:         TestOptions.cs
// Author: Kyle L. Crowder
// Build Num:  080801



using SentinelCore.Contracts;




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
        //   settings ??= new SentinelCoreSettings { CoreModel = new ModelSettings("http://localhost:11434", "test-core"), DomainModel = new ModelSettings("http://localhost:11434", "test-domain"), ManagerModel = new ModelSettings("http://localhost:11434", "test-manager") };
        return Options.Create(settings);
    }
}