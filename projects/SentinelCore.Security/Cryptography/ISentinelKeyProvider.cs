// Solution: SentinelCore
// Project:   SentinelCore.Security
// File:         ISentinelKeyProvider.cs
// Author: Kyle L. Crowder
// Build Num:  112518



namespace SentinelCore.Security.Cryptography;





/// <summary>
///     Process-boundary abstraction that supplies the shared HMAC signing key.
///     Implementations must fail closed: when the key cannot be obtained, callers treat
///     the message as unvalidatable and quarantine it.
/// </summary>
public interface ISentinelKeyProvider
{

    /// <summary>
    ///     Attempts to obtain the shared signing key.
    /// </summary>
    /// <param name="key">The key material when found; an empty string otherwise.</param>
    /// <returns><c>true</c> when a usable key was found.</returns>
    bool TryGetKey(out string key);
}