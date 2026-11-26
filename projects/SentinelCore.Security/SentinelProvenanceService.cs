// Solution: SentinelCore
// Project:   SentinelCore.Security
// File:         SentinelProvenanceService.cs
// Author: Kyle L. Crowder
// Build Num:  112518



using System.Collections.Concurrent;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

using Microsoft.Extensions.AI;

using SentinelCore.Security.Cryptography;
using SentinelCore.Security.Sources;




namespace SentinelCore.Security;





/// <summary>
///     HMAC-SHA256 based message security service. The provenance stamp defines only
///     the source of the message (a SourceId/SourceType pair, verified against the
///     fixed <see cref="SentinelSources" /> registry); the signature is a separate
///     stamp on the message. The canonical payload signed for every message is: source
///     id, source type, RFC 3339 timestamp, nonce, chat role, and message text, joined
///     with newline separators. Signatures are compared using
///     <see cref="CryptographicOperations.FixedTimeEquals" />.
/// </summary>
/// <remarks>
///     Validation is fail-closed at every step: missing or malformed stamps, source
///     pairs not present in the fixed registry, unavailable keys, expired timestamps,
///     invalid signatures, and replayed nonces all produce a failed result that callers
///     must quarantine.
/// </remarks>
public sealed class SentinelProvenanceService : ISentinelProvenanceService
{

    // Consumed nonces mapped to the stamp timestamp of first validation. Entries are
    // pruned once the cache exceeds MaxReplayCacheSize and fall outside twice the
    // timestamp tolerance window.
    private readonly ConcurrentDictionary<string, DateTimeOffset> _consumedNonces = new(StringComparer.Ordinal);

    private readonly ISentinelKeyProvider _keyProvider;
    private readonly SentinelSecurityOptions _options;
    private readonly TimeProvider _timeProvider;

    private const int NonceByteCount = 16;








    /// <summary>
    ///     Initializes the service and validates configuration invariants so
    ///     misconfiguration fails at startup rather than degrading validation at runtime.
    /// </summary>
    /// <param name="keyProvider">The process boundary that supplies the shared signing key.</param>
    /// <param name="options">Optional configuration; defaults are used when <c>null</c>.</param>
    /// <param name="timeProvider">Optional clock; the system clock is used when <c>null</c>. Injected for testability.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="keyProvider" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the options violate the module invariants (non-positive timestamp tolerance, replay cache size below one).</exception>
    public SentinelProvenanceService(ISentinelKeyProvider keyProvider, SentinelSecurityOptions? options = null, TimeProvider? timeProvider = null)
    {
        _keyProvider = keyProvider ?? throw new ArgumentNullException(nameof(keyProvider));
        _timeProvider = timeProvider ?? TimeProvider.System;
        _options = options ?? new SentinelSecurityOptions();
        ValidateOptions(_options);
    }







    /// <summary>
    ///     Validates configuration invariants the module cannot honor silently: a
    ///     non-positive tolerance expires every stamp immediately, and a replay cache
    ///     below one entry cannot retain any consumed nonce.
    /// </summary>
    /// <param name="options">The configuration to validate.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an invariant is violated.</exception>
    private static void ValidateOptions(SentinelSecurityOptions options)
    {
        if (options.TimestampTolerance <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(options), options.TimestampTolerance, "TimestampTolerance must be greater than zero; non-positive values expire every stamped message immediately.");
        }

        if (options.MaxReplayCacheSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(options), options.MaxReplayCacheSize, "MaxReplayCacheSize must be at least 1; smaller values cannot retain consumed nonces for replay detection.");
        }
    }







    /// <summary>
    ///     Resolves the message's provenance source pair against the fixed registry,
    ///     treating an unknown identifier and a registered identifier with a mismatched
    ///     type as the same failure: neither pair is an approved source.
    /// </summary>
    /// <param name="provenance">The provenance stamp to resolve.</param>
    /// <returns>The registered source, or <see langword="null" /> when the pair is not registered.</returns>
    private static MessageSource? ResolveRegisteredSource(MessageProvenance provenance)
    {
        MessageSource? source = SentinelSources.Resolve(provenance.SourceId);
        return source is not null && source.SourceType == provenance.SourceType ? source : null;
    }








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
    /// <exception cref="ArgumentException">Thrown when the provenance source pair is not registered (unknown identifier or mismatched type).</exception>
    public ChatMessage Stamp(ChatMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        MessageProvenance? provenance = message.GetProvenance()
                ?? throw new InvalidOperationException("Every message must carry a provenance stamp (source pair) before it can be signed.");

        MessageSource source = ResolveRegisteredSource(provenance)
                ?? throw new ArgumentException($"The provenance stamp pairs source '{provenance.SourceId}' with type '{provenance.SourceType}', which is not registered in the fixed Sentinel source registry.", nameof(message));

        (_, MessageSignature signature) = CreateStamps(message, provenance.SourceId, provenance.SourceType, _timeProvider.GetUtcNow());
        return message.WithSignature(signature);
    }








    /// <summary>
    ///     Performs the full entry-point validation including nonce consumption
    ///     (replay detection).
    /// </summary>
    /// <param name="message">The message entering the system.</param>
    /// <returns>The validation outcome; every failure is fail-closed.</returns>
    public ProvenanceValidationResult Validate(ChatMessage message)
    {
        ProvenanceValidationResult integrity = CheckIntegrity(message);
        if (!integrity.IsValid)
        {
            return integrity;
        }

        // Fail-closed: a signature stamp removed between the integrity check and
        // this read is a malformed message, not an internal error.
        if (message.GetSignature() is not MessageSignature signature)
        {
            return ProvenanceValidationResult.Failed(ProvenanceFailure.MalformedProvenance, "The signature stamp was removed between integrity checks.");
        }

        if (!_consumedNonces.TryAdd(signature.Nonce, signature.TimestampUtc))
        {
            return ProvenanceValidationResult.Failed(ProvenanceFailure.ReplayDetected, "The message nonce was already consumed by an earlier validation.");
        }

        PruneConsumedNonces();
        return ProvenanceValidationResult.Success();
    }








    /// <summary>
    ///     Performs a non-consuming integrity re-check for downstream validation points.
    /// </summary>
    /// <param name="message">The message to re-check.</param>
    /// <returns>The validation outcome; every failure is fail-closed.</returns>
    public ProvenanceValidationResult VerifyIntegrity(ChatMessage message) =>
            CheckIntegrity(message);








    /// <summary>
    ///     Builds the canonical payload: the provenance source pair, RFC 3339 timestamp,
    ///     nonce, chat role, and message text joined with newline separators. Exposed
    ///     internally so tests can verify the signed payload contract.
    /// </summary>
    /// <param name="sourceId">The registered source identifier.</param>
    /// <param name="sourceType">The registered source type.</param>
    /// <param name="timestampUtc">The stamp timestamp.</param>
    /// <param name="nonce">The per-message nonce.</param>
    /// <param name="role">The chat role of the message.</param>
    /// <param name="text">The message text.</param>
    /// <returns>The canonical payload string.</returns>
    internal static string BuildCanonicalPayload(Guid sourceId, SourceType sourceType, DateTimeOffset timestampUtc, string nonce, string role, string text) =>
            string.Join("\n", sourceId.ToString("D"), sourceType.ToString(), timestampUtc.ToString("O", CultureInfo.InvariantCulture), nonce, role, text);








    /// <summary>
    ///     Integrity checks shared by <see cref="Validate" /> and <see cref="VerifyIntegrity" />.
    /// </summary>
    private ProvenanceValidationResult CheckIntegrity(ChatMessage message)
    {
        if (message is null)
        {
            return ProvenanceValidationResult.Failed(ProvenanceFailure.MissingProvenance, "The message was null.");
        }

        MessageProvenance? provenance = message.GetProvenance();
        if (provenance is null)
        {
            return ProvenanceValidationResult.Failed(ProvenanceFailure.MissingProvenance, "No provenance stamp is present on the message.");
        }

        MessageSignature? signature = message.GetSignature();
        if (signature is null)
        {
            return ProvenanceValidationResult.Failed(ProvenanceFailure.MalformedProvenance, "No signature stamp is present on the message.");
        }

        if (provenance.SourceId == Guid.Empty || !SourceTypes.IsDefined(provenance.SourceType) || string.IsNullOrWhiteSpace(signature.Nonce) || string.IsNullOrWhiteSpace(signature.Signature))
        {
            return ProvenanceValidationResult.Failed(ProvenanceFailure.MalformedProvenance, "The security stamps are missing required fields.");
        }

        if (!_keyProvider.TryGetKey(out string key) || string.IsNullOrWhiteSpace(key))
        {
            return ProvenanceValidationResult.Failed(ProvenanceFailure.KeyUnavailable, "The signing key is unavailable; validation fails closed.");
        }

        if (ResolveRegisteredSource(provenance) is null)
        {
            return ProvenanceValidationResult.Failed(ProvenanceFailure.UnknownSource, $"Source pair '{provenance.SourceId}' / '{provenance.SourceType}' is not registered in the fixed source registry.");
        }

        TimeSpan age = _timeProvider.GetUtcNow() - signature.TimestampUtc;
        if (age > _options.TimestampTolerance || age < -_options.TimestampTolerance)
        {
            return ProvenanceValidationResult.Failed(ProvenanceFailure.ExpiredTimestamp, $"The stamp age ({age}) falls outside the tolerated window ({_options.TimestampTolerance}).");
        }

        string expected = ComputeSignature(provenance.SourceId, provenance.SourceType, signature.TimestampUtc, signature.Nonce, GetRole(message), GetText(message), key);
        return !FixedTimeEqualsBase64(signature.Signature, expected) ? ProvenanceValidationResult.Failed(ProvenanceFailure.InvalidSignature, "The HMAC signature does not match the message contents.") : ProvenanceValidationResult.Success();

    }








    /// <summary>
    ///     Computes the HMAC-SHA256 signature over the canonical payload with the given key.
    /// </summary>
    /// <param name="sourceId">The registered source identifier.</param>
    /// <param name="sourceType">The registered source type.</param>
    /// <param name="timestampUtc">The stamp timestamp.</param>
    /// <param name="nonce">The per-message nonce.</param>
    /// <param name="role">The chat role of the message.</param>
    /// <param name="text">The message text.</param>
    /// <param name="key">The shared signing key.</param>
    /// <returns>The Base64-encoded signature.</returns>
    private static string ComputeSignature(Guid sourceId, SourceType sourceType, DateTimeOffset timestampUtc, string nonce, string role, string text, string key)
    {
        string canonical = BuildCanonicalPayload(sourceId, sourceType, timestampUtc, nonce, role, text);

        return Convert.ToBase64String(HMACSHA256.HashData(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(canonical)));
    }








    /// <summary>
    ///     Creates the provenance pair stamp and the separate signature stamp for the
    ///     message. Exposed internally so tests can construct deterministic stamps (for
    ///     example, backdated timestamps).
    /// </summary>
    /// <param name="message">The message to derive the signed payload from.</param>
    /// <param name="sourceId">The registered source identifier.</param>
    /// <param name="sourceType">The registered source type.</param>
    /// <param name="timestampUtc">The timestamp to record on the stamp.</param>
    /// <returns>The provenance stamp and the signature stamp.</returns>
    internal (MessageProvenance Provenance, MessageSignature Signature) CreateStamps(ChatMessage message, Guid sourceId, SourceType sourceType, DateTimeOffset timestampUtc)
    {
        string nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(NonceByteCount));
        string signature = ComputeSignature(sourceId, sourceType, timestampUtc, nonce, GetRole(message), GetText(message), GetKeyOrThrow());
        return (new(sourceId, sourceType), new(timestampUtc, nonce, signature));
    }








    /// <summary>
    ///     Compares two Base64-encoded signatures in constant time.
    /// </summary>
    /// <param name="actual">The signature read from the stamp.</param>
    /// <param name="expected">The recomputed signature.</param>
    /// <returns><c>true</c> when both signatures decode and match.</returns>
    private static bool FixedTimeEqualsBase64(string actual, string expected)
    {
        byte[] actualBytes;
        byte[] expectedBytes;

        try
        {
            actualBytes = Convert.FromBase64String(actual);
            expectedBytes = Convert.FromBase64String(expected);
        }
        catch (FormatException)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(actualBytes, expectedBytes);
    }








    /// <summary>
    ///     Gets the signing key or throws when unavailable. Stamping cannot proceed
    ///     without a key — signing fails loudly rather than silently producing an
    ///     unverifiable stamp.
    /// </summary>
    /// <returns>The shared signing key.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the signing key is unavailable.</exception>
    private string GetKeyOrThrow()
    {
        return !_keyProvider.TryGetKey(out string key) || string.IsNullOrWhiteSpace(key) ? throw new InvalidOperationException("The signing key is unavailable; messages cannot be stamped for provenance.") : key;

    }








    /// <summary>
    ///     Gets the chat role value of the message for the canonical payload.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <returns>The role value, or an empty string when absent.</returns>
    private static string GetRole(ChatMessage message) =>
            message.Role.Value ?? string.Empty;








    /// <summary>
    /// Extracts the text content from the specified <see cref="ChatMessage"/> instance.
    /// </summary>
    /// <param name="message">The <see cref="ChatMessage"/> from which to retrieve the text.</param>
    /// <returns>
    /// The text content of the message if it is present; otherwise, an empty string.
    /// </returns>
    /// <remarks>
    /// This method ensures that a valid string is always returned, even if the message text is null.
    /// </remarks>
    private static string GetText(ChatMessage message) =>
            message.Text ?? string.Empty;








    /// <summary>
    ///     Prunes consumed nonces from the cache when the number of stored nonces exceeds
    ///     the maximum replay cache size specified in <see cref="SentinelSecurityOptions.MaxReplayCacheSize" />.
    ///     Nonces older than twice the timestamp tolerance defined in
    ///     <see cref="SentinelSecurityOptions.TimestampTolerance" /> are removed.
    /// </summary>
    /// <remarks>
    ///     This method ensures that the cache of consumed nonces does not grow indefinitely,
    ///     maintaining efficient memory usage and preventing replay attacks by removing
    ///     outdated entries.
    /// </remarks>
    private void PruneConsumedNonces()
    {
        if (_consumedNonces.Count <= _options.MaxReplayCacheSize)
        {
            return;
        }

        DateTimeOffset cutoff = _timeProvider.GetUtcNow() - 2 * _options.TimestampTolerance;
        foreach (KeyValuePair<string, DateTimeOffset> entry in _consumedNonces)
        {
            if (entry.Value < cutoff)
            {
                _consumedNonces.TryRemove(entry);
            }
        }
    }
}
