// Solution: SentinelCore
// Project:   SentinelCore.Security
// File:         SourceType.cs
// Author: Kyle L. Crowder
// Build Num:  112518



namespace SentinelCore.Security.Sources;





/// <summary>
///     The fixed list of source types a message source can belong to. The list is
///     read-only by design: values are locked into the build and can only be changed by
///     shipping a new build.
/// </summary>
public enum SourceType
{

    /// <summary>A human operator using a first-party Sentinel Core host.</summary>
    LocalOperator,

    /// <summary>A first-party deployed component, such as the SentinelCoreService.</summary>
    DeployedService,

    /// <summary>An approved external input source.</summary>
    ExternalSource,
    Internal
}





/// <summary>
///     The read-only list of all valid <see cref="SourceType" /> values. The list is
///     fixed at build time; validation of anything outside the list fails closed.
/// </summary>
public static class SourceTypes
{

    /// <summary>Gets every valid source type, in registry order.</summary>
    public static IReadOnlyList<SourceType> All { get; } = [SourceType.LocalOperator, SourceType.DeployedService, SourceType.ExternalSource, SourceType.Internal];








    /// <summary>
    ///     Determines whether the value is one of the fixed source types.
    /// </summary>
    /// <param name="sourceType">The value to check.</param>
    /// <returns><c>true</c> when the value is part of the fixed list.</returns>
    public static bool IsDefined(SourceType sourceType) =>
            All.Contains(sourceType);
}
