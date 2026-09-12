// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         DpapiTokenCache.cs
// Author: Kyle L. Crowder
// Build Num:  091200



using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using ModelContextProtocol.Authentication;




namespace SentinelCore.Orchestrations.Mcp;





/// <summary>
///     DPAPI-encrypted in-memory token cache implementing <see cref="ITokenCache" />.
///     Tokens are encrypted for the current user and optionally persisted to disk.
/// </summary>
public sealed class DpapiTokenCache : ITokenCache
{

    private readonly string? _filePath;
    private readonly object _lock = new();
    private readonly Dictionary<string, TokenContainer> _tokens = new();

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = false, PropertyNameCaseInsensitive = true };








    /// <summary>
    ///     Initializes a new instance of the <see cref="DpapiTokenCache" /> class.
    /// </summary>
    /// <param name="filePath">
    ///     Optional file path used to persist encrypted tokens across process restarts.
    ///     When <c>null</c>, tokens are held only in memory and are lost when the process exits.
    /// </param>
    public DpapiTokenCache(string? filePath = null)
    {
        _filePath = filePath;
        Load();
    }








    /// <inheritdoc />
    public ValueTask<TokenContainer?> GetTokensAsync(CancellationToken cancellationToken = default)
    {
        // Return the first cached token. SentinelCore currently handles a single token set per cache.
        lock (_lock)
        {
            TokenContainer? token = _tokens.Values.FirstOrDefault();
            return new ValueTask<TokenContainer?>(token);
        }
    }








    /// <inheritdoc />
    public ValueTask StoreTokensAsync(TokenContainer tokens, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tokens);

        lock (_lock)
        {
            _tokens["default"] = tokens;
        }

        Persist();
        return ValueTask.CompletedTask;
    }








    private void Load()
    {
        if (string.IsNullOrWhiteSpace(_filePath) || !File.Exists(_filePath))
        {
            return;
        }

        try
        {
            byte[] encrypted = File.ReadAllBytes(_filePath);
            byte[] decrypted = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
            string json = Encoding.UTF8.GetString(decrypted);
            Dictionary<string, TokenContainer>? tokens = JsonSerializer.Deserialize<Dictionary<string, TokenContainer>>(json, JsonOptions);

            if (tokens is not null)
            {
                lock (_lock)
                {
                    foreach (KeyValuePair<string, TokenContainer> entry in tokens)
                    {
                        _tokens[entry.Key] = entry.Value;
                    }
                }
            }
        }
        catch (CryptographicException)
        {
            // DPAPI unprotect failed; treat as no persisted tokens.
        }
        catch (JsonException)
        {
            // Corrupted cache; treat as no persisted tokens.
        }
    }








    private void Persist()
    {
        if (string.IsNullOrWhiteSpace(_filePath))
        {
            return;
        }

        string? directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        lock (_lock)
        {
            string json = JsonSerializer.Serialize(_tokens, JsonOptions);
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            byte[] encrypted = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(_filePath, encrypted);
        }
    }
}