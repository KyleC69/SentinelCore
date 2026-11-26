// Solution: SentinelCore
// Project:   SentinelCore.Security
// File:         ISentinelProvenanceService.cs
// Author: Kyle L. Crowder
// Build Num:  112518



using Microsoft.Extensions.AI;




namespace SentinelCore.Security;





/// <summary>
///     Stamps outgoing messages with an HMAC signature over their provenance source
///     pair, and validates incoming messages before any model sees them.
/// </summary>
public interface ISentinelProvenanceService
{

    /// <summary>
    ///     Stamps the message with a separate HMAC signature stamp derived from the
    ///     provenance stamp the message already carries, in place, and returns the
    ///     same instance. Every message must carry its source pair before it is
    ///     signed; the pair is verified against the fixed registry — callers cannot
    ///     sign an arbitrary pair.
    /// </summary>
    /// <param name="message">The message to stamp; it must already carry its provenance source pair.</param>
    /// <returns>The same message instance, now stamped.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="message" /> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the message carries no provenance stamp.</exception>
    /// <exception cref="ArgumentException">Thrown when the provenance source pair is not registered.</exception>
    ChatMessage Stamp(ChatMessage message);








    /// <summary>
    ///     Performs the full entry-point validation: stamp presence, fixed source registry
    ///     lookup, timestamp window, signature verification, and nonce consumption
    ///     (replay detection). Intended for the outermost entry point of the system.
    /// </summary>
    /// <param name="message">The message entering the system.</param>
    /// <returns>The validation outcome; every failure is fail-closed.</returns>
    ProvenanceValidationResult Validate(ChatMessage message);








    /// <summary>
    ///     Performs a non-consuming integrity re-check for downstream validation points:
    ///     stamp presence, source registry lookup, timestamp window, and signature
    ///     verification — without consuming the nonce, so a message already validated at
    ///     the entry point does not trip replay detection.
    /// </summary>
    /// <param name="message">The message to re-check.</param>
    /// <returns>The validation outcome; every failure is fail-closed.</returns>
    ProvenanceValidationResult VerifyIntegrity(ChatMessage message);
}
