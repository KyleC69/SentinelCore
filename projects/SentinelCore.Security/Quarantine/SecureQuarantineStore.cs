// Solution: SentinelCore
// Project:   SentinelCore.Security
// File:         SecureQuarantineStore.cs
// Author: Kyle L. Crowder
// Build Num:  112518



using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;




namespace SentinelCore.Security.Quarantine;





/// <summary>
///     File-backed quarantine store. Every quarantined message is written as a JSON
///     record into the secure quarantine directory configured via
///     <see cref="SentinelSecurityOptions.QuarantineDirectory" />. The directory is
///     locked with a Windows ACL that grants access only to the process identity —
///     no other user or process can read or modify quarantined content.
/// </summary>
/// <remarks>
///     Nothing is retained in memory beyond the write itself; records are read back
///     from disk by <see cref="Records" />. Message content is deliberately excluded
///     from log output — the content may be malicious and must not be echoed to logs.
/// </remarks>
public sealed class SecureQuarantineStore : IQuarantineStore
{

    private readonly string _directory;
    private readonly ILogger _logger;
    private readonly object _writeLock = new();
    private static readonly JsonSerializerOptions RecordJsonOptions = new() { WriteIndented = true };








    /// <summary>
    ///     Initializes the store, creating the quarantine directory if needed and
    ///     restricting it to the process identity.
    /// </summary>
    /// <param name="logger">The logger used to record quarantine events.</param>
    /// <param name="options">Optional configuration; defaults are used when <c>null</c>.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when the configured quarantine directory is empty or whitespace.</exception>
    public SecureQuarantineStore(ILogger<SecureQuarantineStore> logger, SentinelSecurityOptions? options = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        SentinelSecurityOptions resolved = options ?? new SentinelSecurityOptions();
        if (string.IsNullOrWhiteSpace(resolved.QuarantineDirectory))
        {
            throw new ArgumentException("The quarantine directory must be configured; quarantined messages must be persisted.", nameof(options));
        }

        _directory = resolved.QuarantineDirectory;

        Directory.CreateDirectory(_directory);
        RestrictToProgramIdentity(_directory);
    }








    /// <summary>
    ///     Quarantines the message by persisting a snapshot of its signed content as a
    ///     JSON record in the secure quarantine folder, logs the event, and raises
    ///     <see cref="RecordQuarantined" />.
    /// </summary>
    /// <param name="message">The offending message, stored unchanged as a snapshot.</param>
    /// <param name="reason">The validation failure that triggered quarantine.</param>
    /// <param name="detail">Human-readable details of the event.</param>
    /// <returns>The created quarantine record.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="message" /> is <c>null</c>.</exception>
    public QuarantineRecord Quarantine(ChatMessage message, ProvenanceFailure reason, string detail)
    {
        ArgumentNullException.ThrowIfNull(message);

        QuarantineRecord record = CreateRecord(DateTimeOffset.UtcNow, reason, detail, message);
        string fileName = $"{record.QuarantinedAtUtc:yyyyMMdd-HHmmss-fffffff}-{reason}-{Guid.NewGuid():N}.json";
        string path = Path.Combine(_directory, fileName);
        string tempPath = path + ".tmp";
        string json = JsonSerializer.Serialize(record, RecordJsonOptions);

        try
        {
            lock (_writeLock)
            {
                File.WriteAllText(tempPath, json, Encoding.UTF8);
                File.Move(tempPath, path);
            }
        }
        catch (Exception)
        {
            TryDeleteTempFile(tempPath);
            throw;
        }

        _logger.LogWarning("[SentinelSecurity] Message quarantined to {QuarantineFile}. Reason: {Reason}. Detail: {Detail}", fileName, reason, record.Detail);

        try
        {
            RecordQuarantined?.Invoke(record);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "[SentinelSecurity] A quarantine observer failed; the record is persisted but notification aborted. Reason: {Reason}. Detail: {Detail}", reason, record.Detail);
        }

        return record;
    }








    /// <summary>Raised immediately after a message is quarantined.</summary>
    public event Action<QuarantineRecord>? RecordQuarantined;



    /// <summary>Gets the quarantine records persisted in the secure folder, oldest first.</summary>
    public IReadOnlyList<QuarantineRecord> Records
    {
        get
        {
            if (!Directory.Exists(_directory))
            {
                return [];
            }

            List<QuarantineRecord> records = [];
            foreach (string path in Directory.EnumerateFiles(_directory, "*.json").OrderBy(path => path, StringComparer.Ordinal))
            {
                try
                {
                    records.Add(JsonSerializer.Deserialize<QuarantineRecord>(File.ReadAllText(path)) ?? throw new JsonException("The quarantine record file is empty."));
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
                {
                    // Skip records that cannot be read right now or are corrupt.
                }
            }

            return records;
        }
    }








    /// <summary>
    ///     Removes the temporary file after a failed atomic write so partial records
    ///     do not accumulate in the quarantine folder. Cleanup is best effort: its own
    ///     failure must not mask the original write failure.
    /// </summary>
    /// <param name="tempPath">The temporary file path to delete.</param>
    private static void TryDeleteTempFile(string tempPath)
    {
        try
        {
            File.Delete(tempPath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Best effort only; the original write failure remains the relevant one.
        }
    }







    /// <summary>
    ///     Builds the persisted snapshot of the offending message: chat role, text, and
    ///     the provenance and signature stamps exactly as they arrived.
    /// </summary>
    private static QuarantineRecord CreateRecord(DateTimeOffset quarantinedAtUtc, ProvenanceFailure reason, string detail, ChatMessage message)
    {
        MessageProvenance? provenance = message.GetProvenance();
        MessageSignature? signature = message.GetSignature();

        return new QuarantineRecord(quarantinedAtUtc, reason, detail ?? string.Empty, message.Role.Value ?? string.Empty, message.Text ?? string.Empty, provenance?.SourceId, provenance?.SourceType.ToString(), signature?.TimestampUtc, signature?.Nonce, signature?.Signature);
    }








    /// <summary>
    ///     Locks the quarantine directory down to the process identity: inheritance is
    ///     disabled, inherited rules are removed, and a single FullControl rule is
    ///     granted to the account the program runs as.
    /// </summary>
    private static void RestrictToProgramIdentity(string directory)
    {
        using WindowsIdentity identity = WindowsIdentity.GetCurrent();
        SecurityIdentifier programIdentity = identity.User ?? throw new InvalidOperationException("The process identity could not be resolved for quarantine folder protection.");

        DirectorySecurity security = new();
        security.SetAccessRuleProtection(true, false);
        security.AddAccessRule(new FileSystemAccessRule(programIdentity, FileSystemRights.FullControl, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
        new DirectoryInfo(directory).SetAccessControl(security);
    }
}
