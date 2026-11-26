// Solution: SentinelCore
// Project:   SentinelCore.Security
// File:         EnvironmentSentinelKeyProvider.cs
// Author: Kyle L. Crowder
// Build Num:  112518



namespace SentinelCore.Security.Cryptography;





/// <summary>
///     Reads the shared signing key from a fixed environment variable. The variable
///     name is a locked constant (see <see cref="DefaultVariableName" />); it is not
///     configurable through options.
/// </summary>
/// <remarks>
///     The key is immutable for the lifetime of the provider: the environment variable
///     is read exactly once, on the first key request, and the value is cached. Changing
///     or rotating the key requires a new process start — an intentional security
///     measure. Callers must never log the key.
/// </remarks>
public sealed class EnvironmentSentinelKeyProvider : ISentinelKeyProvider
{

    private readonly Lazy<string?> _key;
    private readonly string _variableName;

    /// <summary>The environment variable that holds the shared HMAC signing key.</summary>
    public const string DefaultVariableName = "SENTINEL_KEY";








    /// <summary>
    ///     Initializes the provider with the fixed environment variable name.
    /// </summary>
    public EnvironmentSentinelKeyProvider() : this(DefaultVariableName)
    {
    }








    /// <summary>
    ///     Initializes the provider with the environment variable name to read.
    /// </summary>
    /// <param name="variableName">The environment variable that holds the signing key.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="variableName" /> is empty or whitespace.</exception>
    public EnvironmentSentinelKeyProvider(string variableName)
    {
        if (string.IsNullOrWhiteSpace(variableName))
        {
            throw new ArgumentException("A variable name is required.", nameof(variableName));
        }

        _variableName = variableName;
        _key = new Lazy<string?>(() => Environment.GetEnvironmentVariable(_variableName), LazyThreadSafetyMode.ExecutionAndPublication);
    }








    /// <summary>
    ///     Attempts to obtain the shared signing key from the configured environment
    ///     variable. The variable is read exactly once; later mutations of the variable
    ///     have no effect on this provider.
    /// </summary>
    /// <param name="key">The key material when found; an empty string otherwise.</param>
    /// <returns><c>true</c> when the variable held a non-empty, non-whitespace value at first read.</returns>
    public bool TryGetKey(out string key)
    {
        string? value = _key.Value;

        if (string.IsNullOrWhiteSpace(value))
        {
            key = string.Empty;
            return false;
        }

        key = value;
        return true;
    }
}