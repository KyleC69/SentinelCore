// Solution: SentinelCore
// Project:   SentinelCore.Security
// File:         SentinelSources.cs
// Author: Kyle L. Crowder
// Build Num:  112518



using System.Collections.Frozen;



namespace SentinelCore.Security.Sources;





/// <summary>
///     The internal fixed registry of approved message sources. Every entry is
///     hardcoded at build time; the values are locked and can only be changed by
///     shipping a new build — an intentional security measure. There is deliberately no
///     runtime registration, revocation, or configuration: provenance validation
///     verifies a message's source pair against these values and nothing else.
/// </summary>
public static class SentinelSources
{

    /// <summary>
    ///     The SentinelCoreService Windows service deployed with the app. It sends signed
    ///     messages to The Core; its transport pathway is not yet implemented.
    /// </summary>
    public static readonly MessageSource CoreService = MessageSource.Create(new Guid("3b297f4d-5969-40d2-82c7-e1ed6008c9b8"), SourceType.DeployedService, "Sentinel Core Service (deployed)");

    /// <summary>The SentinelCore.UI WPF host (human operator chat and investigations).</summary>
    public static readonly MessageSource CoreUi = MessageSource.Create(new Guid("c35a8107-669c-4728-bee5-3750891e7241"), SourceType.LocalOperator, "Sentinel Core UI (operator host)");

    /// <summary>The approved external input source.</summary>
    public static readonly MessageSource ExternalIngest = MessageSource.Create(new Guid("79407efd-966a-424f-a330-b9b4ce71a1f6"), SourceType.ExternalSource, "Approved external input source");

    /// <summary>Internal Sentinel infrastructure messaging.</summary>
    public static readonly MessageSource Internal = MessageSource.Create(new Guid("63771025-4088-44D3-806C-CD4A8385EA65"), SourceType.Internal, "Internal source");
    /// <summary>Gets every source in the fixed registry.</summary>
    public static IReadOnlyList<MessageSource> RegisteredSources { get; } = [CoreUi, CoreService, ExternalIngest, Internal];

    private static readonly FrozenDictionary<Guid, MessageSource> Registry = RegisteredSources.ToFrozenDictionary(source => source.SourceId);







    /// <summary>
    ///     Resolves the source registered for a source identifier.
    /// </summary>
    /// <param name="sourceId">The source identifier to resolve.</param>
    /// <returns>The registered source, or <see langword="null" /> when the identifier is empty or unknown.</returns>
    public static MessageSource? Resolve(Guid sourceId) =>
            sourceId != Guid.Empty && Registry.TryGetValue(sourceId, out MessageSource? source) ? source : null;








    /// <summary>
    ///     Determines whether the given source identifier is registered.
    /// </summary>
    /// <param name="sourceId">The source identifier to check.</param>
    /// <returns><c>true</c> when the identifier is registered.</returns>
    public static bool IsRegistered(Guid sourceId) =>
            Resolve(sourceId) is not null;








    /// <summary>
    ///     Attempts to resolve the source registered for a source identifier.
    /// </summary>
    /// <param name="sourceId">The source identifier to resolve.</param>
    /// <param name="source">The resolved source, or <c>null</c> when unknown.</param>
    /// <returns><c>true</c> when the identifier is registered.</returns>
    public static bool TryGet(Guid sourceId, out MessageSource? source)
    {
        source = Resolve(sourceId);
        return source is not null;
    }
}
