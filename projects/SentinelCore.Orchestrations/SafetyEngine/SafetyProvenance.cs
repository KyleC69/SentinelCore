#nullable enable

using System.Globalization;
using System.Text;

using SentinelCore.Abstractions;

namespace SentinelCore.Orchestrations.SafetyEngine;

/// <summary>
///     Encapsulates the metadata tags that carry safety decisions.
///     The safety module is the only component allowed to create, read, and verify these tags.
/// </summary>
public static class SafetyTagger
{
    private const string ObfuscationKey = "SentinelCore.Safety.Tagging";

    /// <summary>
    ///     Adds the score and verdict tags for the current safety evaluation.
    /// </summary>
    public static ChatMessage Attach(ChatMessage message, int score, string result)
    {
        ArgumentNullException.ThrowIfNull(message);

        ChatMessage taggedMessage = message;

        if (score > 0)
        {
            taggedMessage = taggedMessage.WithAgentRequestMessageSource(new AgentRequestMessageSourceType("SafetyScore"), Obfuscate(score.ToString(CultureInfo.InvariantCulture)));
        }

        if (!string.IsNullOrWhiteSpace(result))
        {
            taggedMessage = taggedMessage.WithAgentRequestMessageSource(new AgentRequestMessageSourceType("SafetyResult"), Obfuscate(result));
        }

        return taggedMessage;
    }

    /// <summary>
    ///     Verifies that a safety tag was created by the security module and remains unmodified.
    /// </summary>
    public static bool Verify(string? encryptedValue, string expectedPlainText)
    {
        if (string.IsNullOrWhiteSpace(encryptedValue))
        {
            return false;
        }

        string? decryptedValue = Deobfuscate(encryptedValue);
        return string.Equals(decryptedValue, expectedPlainText, StringComparison.Ordinal);
    }

    private static string Obfuscate(string plainText)
    {
        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
        byte[] keyBytes = Encoding.UTF8.GetBytes(ObfuscationKey);
        byte[] result = new byte[plainBytes.Length];

        for (int i = 0; i < plainBytes.Length; i++)
        {
            result[i] = (byte)(plainBytes[i] ^ keyBytes[i % keyBytes.Length]);
        }

        return Convert.ToBase64String(result);
    }

    private static string? Deobfuscate(string encryptedValue)
    {
        try
        {
            byte[] encryptedBytes = Convert.FromBase64String(encryptedValue);
            byte[] keyBytes = Encoding.UTF8.GetBytes(ObfuscationKey);
            byte[] result = new byte[encryptedBytes.Length];

            for (int i = 0; i < encryptedBytes.Length; i++)
            {
                result[i] = (byte)(encryptedBytes[i] ^ keyBytes[i % keyBytes.Length]);
            }

            return Encoding.UTF8.GetString(result);
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
