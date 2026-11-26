// Solution: SentinelCore
// Project:   SentinelCore.Security
// File:         SentinelSecurityOptions.cs
// Author: Kyle L. Crowder
// Build Num:  112518



namespace SentinelCore.Security;





/// <summary>
///     Configuration for the SentinelCore message-provenance security module.
/// </summary>
/// <remarks>
///     The module is fail-closed by design: when the signing key is unavailable, when a
///     signature does not verify, or when validation itself throws, the message is
///     quarantined unchanged and never reaches a model.
/// </remarks>
public sealed class SentinelSecurityOptions
{


    /// <summary>
    ///     Gets or sets the maximum number of consumed nonces retained for replay
    ///     detection before a prune pass removes entries older than twice the
    ///     timestamp tolerance.
    /// </summary>
    /// <remarks>
    ///     The provenance service enforces this value at construction: it must be at
    ///     least <c>1</c> or the replay cache cannot retain any consumed nonce.
    /// </remarks>
    public int MaxReplayCacheSize { get; set; } = 4096;



    /// <summary>
    ///     Gets or sets the secure directory where quarantined messages are persisted as
    ///     JSON files. The directory ACL grants access to the process identity only;
    ///     processes running under different accounts should configure distinct
    ///     directories.
    /// </summary>
    /// <remarks>
    ///     The quarantine store enforces this value at construction: it must not be
    ///     empty or whitespace, because quarantine persistence is mandatory, never
    ///     optional.
    /// </remarks>
    public string QuarantineDirectory { get; set; } = DefaultQuarantineDirectory();



    /// <summary>
    ///     Gets or sets the maximum age (and future clock skew) accepted for a stamped
    ///     message before it is treated as expired. Consumed nonces are retained for at
    ///     least this window so replays inside the window are detected.
    /// </summary>
    /// <remarks>
    ///     The provenance service enforces this value at construction: it must be
    ///     greater than <see cref="TimeSpan.Zero" />; a non-positive tolerance expires
    ///     every stamped message immediately.
    /// </remarks>
    public TimeSpan TimestampTolerance { get; set; } = TimeSpan.FromMinutes(5);








    /// <summary>
    ///     The default quarantine directory under the common application data folder.
    /// </summary>
    private static string DefaultQuarantineDirectory() =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "SentinelCore", "Quarantine");
}
