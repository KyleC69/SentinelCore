// Solution: SentinelCore
// Project:   SentinelCore.Tests
// File:         NoOpLoggerFactory.cs
// Author: Kyle L. Crowder
// Build Num:  081602



using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;




namespace SentinelCore.Tests.TestInfrastructure;





/// <summary>
///     Provides a no-op <see cref="ILoggerFactory" /> for tests that don't need to
///     verify logging output. Uses <see cref="NullLoggerFactory" />.
/// </summary>
public static class NoOpLoggerFactory
{
    public static ILoggerFactory Instance { get; } = NullLoggerFactory.Instance;








    public static ILogger<T> CreateLogger<T>()
    {
        return NullLogger<T>.Instance;
    }
}