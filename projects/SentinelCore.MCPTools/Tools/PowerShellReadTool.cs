// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         PowerShellReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.Collections;
using System.ComponentModel;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text.Json;

using Microsoft.Win32;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     A strictly sandboxed, read-only PowerShell query tool.
///     Only whitelisted commands are permitted. All commands run in a constrained
///     runspace with no write, network, or execution capabilities.
/// </summary>
[SupportedOSPlatform("windows")]
[McpServerToolType]
public sealed partial class PowerShellReadTool
{

    /// <summary>
    ///     Maximum number of result objects returned from any single query.
    /// </summary>
    private const int DefaultMaxResults = 25;

    /// <summary>
    ///     Timeout in seconds for PowerShell command execution.
    ///     Commands exceeding this timeout are aborted.
    /// </summary>
    private const int ExecutionTimeoutSeconds = 20;

    /// <summary>
    ///     Maximum string length for any single property value in the output.
    ///     Values exceeding this length are truncated.
    /// </summary>
    private const int MaxPropertyStringLength = 1024;

    private const int MaxProjectionDepth = 16;

    /// <summary>
    ///     Maximum number of nodes projected for a single result object.
    /// </summary>
    /// <remarks>
    ///     Depth alone cannot bound the walk. Several .NET accessors (for example
    ///     <c>ServiceController.DependentServices</c> and <c>Process.Modules</c>) return a freshly
    ///     constructed wrapper on every read, so reference-identity cycle detection never fires and
    ///     the graph fans out exponentially towards <see cref="MaxProjectionDepth" />.
    /// </remarks>
    private const int MaxProjectionNodes = 512;

    /// <summary>
    ///     Absolute cap on projected nodes for one query, summed across all rows. Once the cap is
    ///     reached the remaining fetched rows are not projected and the truncation is flagged in
    ///     the payload, so a single query cannot raise the serialized payload without bound.
    /// </summary>
    private const int MaxQueryNodeBudget = 2048;

    /// <summary>
    ///     Maximum number of lines kept from a text-mode console output row.
    /// </summary>
    private const int MaxConsoleOutputLines = 200;

    /// <summary>
    ///     Hard cap on the serialized payload length in characters. When exceeded, the row set is
    ///     deterministically halved until it fits, so no query can emit a payload a model cannot
    ///     consume. This is the final guarantee against token-limit explosions.
    /// </summary>
    private const int MaxOutputChars = 128_000;

    /// <summary>
    ///     Serializes creation of <see cref="_sharedRunspace" />.
    /// </summary>
    private static readonly Lock _runspaceGate = new();

    /// <summary>
    ///     A <see cref="Runspace" /> executes one pipeline at a time; this gate keeps concurrent
    ///     tool calls from colliding on the shared runspace.
    /// </summary>
    private static readonly SemaphoreSlim _runspaceSemaphore = new(1, 1);

    /// <summary>
    ///     Long-lived runspace used by every query. Creating a runspace requires importing the
    ///     inbox script modules, which costs several seconds, so the runspace is opened once and
    ///     reused. A <see cref="Runspace" /> runs a single pipeline at a time, so concurrent tool
    ///     calls are serialized by <see cref="_runspaceSemaphore" />.
    /// </summary>
    private static Runspace? _sharedRunspace;

    /// <summary>
    ///     Inbox Windows PowerShell modules that host the whitelisted cmdlets. These are script
    ///     modules (.cdxml/.psm1), so they can only be loaded while the session is still in
    ///     FullLanguage. They are imported during runspace open, before the language mode is
    ///     reduced. A module that is missing on the host is skipped; the whitelist still rejects
    ///     any command that consequently cannot be resolved.
    /// </summary>
    private static readonly string[] RequiredModules =
    [
            "CimCmdlets",
            "Microsoft.PowerShell.Diagnostics",
            "Microsoft.PowerShell.LocalAccounts",
            "Microsoft.PowerShell.Management",
            "Microsoft.PowerShell.Security",
            "Microsoft.PowerShell.Utility",
            "Microsoft.WSMan.Management",
            "NetAdapter",
            "NetTCPIP",
            "NetSecurity",
            "DnsClient",
            "ScheduledTasks",
            "PnpDevice",
            "Storage",
            "AppLocker",
            "SecureBoot",
            "TrustedPlatformModule",
            "BitLocker",
            "SmbShare",
            "WindowsUpdate",
            "Defender",
            "ProcessMitigations",
            "PKI"
    ];



    public static readonly HashSet<string> AllowedCommands = new(StringComparer.OrdinalIgnoreCase)
    {
            // ---- System information ----
            "Get-ComputerInfo",
            "Get-Date",
            "Get-Host",
            "Get-Process",
            "Get-Service",
            "Get-EventLog",
            "Get-WinEvent",
            "systeminfo",

            // ---- Hardware & drivers ----
            "Get-CimInstance",
            "Get-WmiObject",
            "Get-PnpDevice",
            "Get-PnpDeviceProperty",
            "pnputil",

            // ---- Network (read-only) ----
            "Get-NetAdapter",
            "Get-NetIPAddress",
            "Get-NetRoute",
            "Get-NetTCPConnection",
            "Get-NetUDPEndpoint",
            "Get-NetNeighbor",
            "Get-DnsClientCache",
            "Get-DnsClientServerAddress",
            "Get-NetFirewallRule",
            "Get-NetFirewallProfile",
            "ipconfig",
            "netstat",
            "arp",
            "route",

            // ---- Security & policy ----
            "Get-AppLockerPolicy",
            "Get-Acl",
            "Get-ExecutionPolicy",
            "Get-AuthenticodeSignature",
            "Get-FileHash",
            "auditpol",

            // ---- Storage & filesystem (read-only) ----
            "Get-Volume",
            "Get-Partition",
            "Get-Disk",
            "Get-Item",
            "Get-ChildItem",
            "Get-Content",
            "Get-ItemProperty",
            "Test-Path",

            // ---- User & session ----
            "Get-LocalUser",
            "Get-LocalGroup",
            "Get-LocalGroupMember",
            "query",

            // ---- Scheduled tasks ----
            "Get-ScheduledTask",
            "Get-ScheduledTaskInfo",

            // ---- Windows Update ----
            "Get-HotFix",

            // ---- Environment ----
            // There is no Get-EnvironmentVariable cmdlet. Environment variables are reached through
            // the Env provider, for example Get-ChildItem Env: or Get-Item Env:PATH. The provider
            // drive form is validated as a drive-qualified path, not as a command token.

            // ---- Format & output ----
            // No pipeline or conversion stage is allow-listed. The tool forbids '|' and ';', so a
            // stage could never receive input, and every stage is either inert (Format-*, Out-String)
            // or a write path (Tee-Object writes files, Export-* writes files). Results are projected
            // into a typed payload by the tool itself.
    };

    /// <summary>
    ///     Commands and patterns that are explicitly forbidden regardless of context.
    ///     Any command matching these patterns is rejected before execution.
    /// </summary>
    private static readonly HashSet<string> ForbiddenCommands = new(StringComparer.OrdinalIgnoreCase)
    {
            // ---- Write / modify ----
            "Set-Content",
            "Set-Item",
            "Set-ItemProperty",
            "Set-Location",
            "Set-ExecutionPolicy",
            "Set-Service",
            "Set-NetFirewallRule",
            "Set-NetFirewallProfile",
            "New-Item",
            "New-ItemProperty",
            "New-Service",
            "New-LocalUser",
            "New-NetFirewallRule",
            "Remove-Item",
            "Remove-ItemProperty",
            "Remove-LocalUser",
            "Remove-LocalGroup",
            "Remove-LocalGroupMember",
            "Remove-NetFirewallRule",
            "Remove-Service",
            "Copy-Item",
            "Move-Item",
            "Rename-Item",
            "Clear-Content",
            "Clear-ItemProperty",
            "Out-File",
            "Out-Printer",
            "Export-Csv",
            "Export-Clixml",
            "Export-Json",

            // ---- Execution / invocation ----
            "Invoke-Command",
            "Invoke-Expression",
            "Invoke-WebRequest",
            "Invoke-RestMethod",
            "Invoke-CimMethod",
            "Invoke-WmiMethod",
            "Start-Process",
            "Start-Service",
            "Start-Sleep",
            "Stop-Process",
            "Stop-Service",
            "Restart-Service",
            "Enter-PSSession",
            "New-PSSession",
            "Connect-PSSession",
            "WinRM",

            // ---- Dangerous cmdlets ----
            "Invoke-Script",
            "Invoke-AsWorkflow",
            "Register-EngineEvent",
            "Register-WmiEvent",
            "Register-CimIndicationEvent",
            "Add-Content",
            "Add-Member",
            "Write-Host",
            "Write-Output",
            "Write-Verbose",
            "Write-Debug",
            "Write-Warning",
            "Write-Error",
            "Write-Information",

            // ---- Network (destructive) ----
            "Enable-NetFirewallRule",
            "Disable-NetFirewallRule",
            "New-NetRoute",
            "Remove-NetRoute",
            "Set-NetAdapter",
            "Restart-NetAdapter",
            "Disable-NetAdapter",
            "Enable-NetAdapter",
            "netsh",

            // ---- User management ----
            "Add-LocalGroupMember",
            "Disable-LocalUser",
            "Enable-LocalUser",
            "Set-LocalUser",

            // ---- Scheduled tasks (destructive) ----
            "Register-ScheduledTask",
            "Unregister-ScheduledTask",
            "Start-ScheduledTask",
            "Stop-ScheduledTask",
            "Enable-ScheduledTask",
            "Disable-ScheduledTask",

            // ---- Misc dangerous ----
            "cmd",
            "cmd.exe",
            "powershell",
            "pwsh",
            "bash",
            "sh",
            "schtasks",
            "reg",
            "reg.exe",
            "sc",
            "sc.exe",
            "shutdown",
            "restart-computer",
            "stop-computer"
    };








    static PowerShellReadTool()
    {
        // The MCP host instantiates tool types at server startup, so this runs before the
        // first runspace is created and before the engine computes its module search path.
        PowerShellModulePathResolver.EnsureConfigured();
    }








    /// <summary>
    ///     Creates the session state used for every query. The session starts in
    ///     <see cref="PSLanguageMode.FullLanguage" /> only long enough to import the inbox
    ///     Windows modules that the whitelisted cmdlets live in, then it is downgraded to
    ///     <see cref="PSLanguageMode.ConstrainedLanguage" /> before any user-supplied command
    ///     string is parsed (see <see cref="CreateConstrainedRunspace" />).
    /// </summary>
    /// <remarks>
    ///     Module import is required because .cdxml- and .psm1-based inbox modules
    ///     (NetAdapter, NetTCPIP, NetSecurity, Storage, ScheduledTasks, PnpDevice, DnsClient,
    ///     AppLocker, ...) are script modules. PowerShell refuses to load a FullLanguage script
    ///     module into a ConstrainedLanguage session, so importing them after the downgrade fails
    ///     with "the module could not be loaded". Importing first and constraining afterwards is
    ///     the supported way to expose those cmdlets while still executing the caller's command
    ///     string under ConstrainedLanguage.
    /// </remarks>
    private static InitialSessionState? CreateConstrainedSessionState()
    {
        // Ensure module locations are resolved before the engine builds its module path.
        PowerShellModulePathResolver.EnsureConfigured();

        // Create a default session state, pre-import the inbox modules, then restrict it.
        InitialSessionState iss = InitialSessionState.CreateDefault();

        // The inbox modules are script modules that require an unrestricted language mode to load.
        if (!ReferenceEquals(iss, null))
        {
            iss.LanguageMode = PSLanguageMode.FullLanguage;

            // ExecutionPolicy cannot be set from a constrained context, so pin it before the switch.
            // The script-module (.cdxml/.psm1) inbox modules used below are refused outright when
            // script execution is disabled, which is the default for a hosted runspace.
            iss.ExecutionPolicy = Microsoft.PowerShell.ExecutionPolicy.Bypass;

            // Preload only the modules that host whitelisted cmdlets. Modules are imported by name
            // from PSModulePath by the engine during Open(), before the language mode is reduced.
            iss.ImportPSModule(RequiredModules);

        }
        return iss;

    }








    /// <summary>
    ///     Opens a runspace from <see cref="CreateConstrainedSessionState" /> and immediately
    ///     reduces it to <see cref="PSLanguageMode.ConstrainedLanguage" />. The downgrade happens
    ///     before the runspace is handed to a <see cref="PowerShell" /> instance, so no
    ///     caller-supplied text is ever parsed in FullLanguage.
    /// </summary>
    private static Runspace CreateConstrainedRunspace()
    {
        Runspace runspace = RunspaceFactory.CreateRunspace(CreateConstrainedSessionState());
        try
        {
            runspace.Open();

            using PowerShell ps = PowerShell.Create();
            ps.Runspace = runspace;

            ps.AddScript(@"$PSStyle.OutputRendering = 'PlainText'");
            ps.Invoke();
            // The proxy is settable outside the pipeline, so the downgrade does not depend on
            // executing script or a cmdlet in the session that is being locked down.
            runspace.SessionStateProxy.LanguageMode = PSLanguageMode.ConstrainedLanguage;

            return runspace;
        }
        catch
        {
            runspace.Dispose();
            throw;
        }
    }








    /// <summary>
    ///     Returns the shared runspace, creating it on first use. Module import dominates the
    ///     cost of opening a runspace (several seconds on a warm host), so a single long-lived
    ///     runspace keeps per-query latency inside the execution timeout.
    /// </summary>
    /// <returns>An open runspace locked to <see cref="PSLanguageMode.ConstrainedLanguage" />.</returns>
    private static Runspace GetOrCreateSharedRunspace()
    {
        Runspace? existing = _sharedRunspace;
        if (existing is not null && existing.RunspaceStateInfo.State == RunspaceState.Opened)
        {
            return existing;
        }

        lock (_runspaceGate)
        {
            existing = _sharedRunspace;
            if (existing is null || existing.RunspaceStateInfo.State != RunspaceState.Opened)
            {
                existing?.Dispose();
                _sharedRunspace = CreateConstrainedRunspace();
            }

            using PowerShell ps = PowerShell.Create();
            ps.Runspace = _sharedRunspace;

            ps.AddScript(@"$PSStyle.OutputRendering = 'PlainText'");
            ps.Invoke();
            return _sharedRunspace!;
        }
    }








    /// <summary>
    ///     Returns a suffix describing the module path search failure, or an empty string
    ///     when module locations were resolved normally.
    /// </summary>
    private static string FormatModulePathNote()
    {
        string problem = PowerShellModulePathResolver.ConfigurationProblem;
        return string.IsNullOrEmpty(problem) ? string.Empty : $" [{problem}]";
    }








    /// <summary>
    ///     The set of PowerShell commands that are explicitly permitted.
    ///     Only these commands may be invoked through this tool.
    ///     Every entry is a read-only, non-destructive query command.
    /// </summary>
    [Description("Retrieve a list of allowed powershell commands")]
    [McpServerTool(Title = "List of allowed command for this tool.", Destructive = false, Name = "sc_utilities_list_powershell_commands", ReadOnly = true)]
    public List<string> GetAllowedCommands() => AllowedCommands.ToList();








    /// <summary>
    ///     Returns the list of allowed PowerShell commands for reference.
    /// </summary>
    [SupportedOSPlatform("windows")]
    [Description("Returns the list of PowerShell commands that are permitted by the PowerShell_Query tool.")]
    public Task<PowerShellResult> PowershellListAllowedCommandsAsync()
    {
        string[] sorted = [.. AllowedCommands.Order(StringComparer.OrdinalIgnoreCase)];

        // List<string>.ToString() returns the type name ("System.String[]"), not the commands.
        return Task.FromResult(PowerShellResult.Ok(results: string.Join(", ", sorted), "PowerShellReadTool"));
    }








    /// <summary>
    ///     Executes a validated, read-only PowerShell command in a constrained runspace and returns
    ///     a bounded, projected payload. Rows are capped at maxResults with a visible
    ///     RowLimitReached flag, the projection is budgeted both per row and per query with
    ///     visible NodeBudgetExceeded markers, strings are cleaned and truncated, and the
    ///     serialized payload is hard-capped by length. Nested columns are never silently hidden.
    /// </summary>
    /// <param name="command">The single, allow-listed PowerShell command to execute.</param>
    /// <param name="maxResults">Maximum number of result objects to return; must be 1-500.</param>
    /// <param name="skip">Number of result objects to skip, zero or greater; used for paging with maxResults.</param>
    /// <param name="cancellationToken">Cancels the query and aborts the pipeline.</param>
    /// <returns>A <see cref="PowerShellResult" /> carrying the projected rows, or a structured failure.</returns>
    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_utilities_query_powershell", ReadOnly = true, Destructive = false)]
    [Description("Executes a pre-approved, read-only PowerShell query in a sandboxed runspace. " + "Only whitelisted commands are permitted. Pipelines, redirects, script blocks, " + "subexpressions and .NET type access are rejected. Call AllowedCommands for a list of commands.")]
    public async Task<PowerShellResult> PowershellQueryAsync([Description("The PowerShell command to execute. Must be a single read-only command " + "from the approved list. No pipelines, redirects, or script blocks.")] string command, [Description("Maximum number of result objects to return. Must be between 1 and 500. Defaults to 25.")] int maxResults = DefaultMaxResults, [Description("Number of result objects to skip, zero or greater. Defaults to 0. Used for paging with maxResults.")] int skip = 0, [Description("Cancellation token for the operation.")] CancellationToken cancellationToken = default)
    {
        // Validation failures are returned before any runspace is touched.
        string? validationError = ValidateCommand(command);
        if (validationError is not null)
        {
            return PowerShellResult.Fail(validationError, "PowerShellReadTool");
        }

        // maxResults was previously only documented as 1-500 but never enforced, which allowed a
        // caller to pull an unbounded number of full rows. Enforce the declared bounds so a query
        // can never fan out beyond them.
        ToolResult? maxResultsValidation = InputValidator.ValidateMaxRecords(maxResults, "maxResults");
        if (maxResultsValidation is not null)
        {
            return PowerShellResult.Fail(maxResultsValidation.ErrorDetails ?? "maxResults is invalid.", "PowerShellReadTool");
        }

        if (skip < 0)
        {
            return PowerShellResult.Fail("skip must be zero or greater.", "PowerShellReadTool");
        }


        // The shared runspace executes one pipeline at a time, so concurrent tool calls queue here.
        await _runspaceSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            Runspace runspace = GetOrCreateSharedRunspace();

            // One extra row is fetched solely to detect truncation deterministically.
            using PowerShell ps = PowerShell.Create();
            ps.Runspace = runspace;
            ps.AddScript(command);
            ps.AddCommand("Select-Object");
            ps.AddParameter("First", maxResults + 1);
            ps.AddParameter("Skip", skip);

            PSDataCollection<PSObject> collected = await InvokeWithTimeoutAsync(ps, cancellationToken).ConfigureAwait(false);

            if (ps.HadErrors && collected.Count == 0)
            {
                string errors = string.Join("; ", ps.Streams.Error.Select(e => e.ToString()));
                return PowerShellResult.Fail(error:$"PowerShell query failed: {Helpers.CleanValue(errors)}{FormatModulePathNote()}", command: command);
            }

            List<object?> rows = new(Math.Min(collected.Count, maxResults));
            bool rowLimitReached = collected.Count > maxResults;
            bool projectionTruncated = false;
            long nodesUsed = 0;

            foreach (PSObject item in collected)
            {
                if (rows.Count >= maxResults)
                {
                    break;
                }

                if (nodesUsed >= MaxQueryNodeBudget)
                {
                    projectionTruncated = true;
                    break;
                }

                rows.Add(ProjectRow(item));
                nodesUsed += 1;
            }

            PowerShellQueryPayload payload = new(command, rows.Count, maxResults, rowLimitReached, projectionTruncated, rows);

            string json = JsonSerializer.Serialize(payload);

            // Absolute token bound: deterministically halve the row set until the serialized
            // payload fits the output cap.
            while (json.Length > MaxOutputChars && payload.Rows.Count > 1)
            {
                int keep = Math.Max(1, payload.Rows.Count / 2);
                payload = payload with { Rows = payload.Rows.Take(keep).ToList() };
                json = JsonSerializer.Serialize(payload);
            }

            bool sizeLimited = payload.Rows.Count < rows.Count;

            PowerShellResult result = PowerShellResult.Ok(
                results: payload,
                message: sizeLimited ? $"Completed; the payload was limited to {payload.Rows.Count} row(s) to fit the output size limit." : "Completed.",
                command: command);

            // Partial errors must not discard the rows that were produced, but they must not be
            // silent either: the error text is surfaced on the result while Success stays true.
            if (ps.HadErrors)
            {
                string errorSummary = string.Join("; ", ps.Streams.Error.Select(e => e.ToString()));
                result.Error = Helpers.CleanValue(errorSummary);
                result.Message = "Completed with one or more PowerShell error records; see Error.";
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            return PowerShellResult.Fail(error:$"Command execution timed out after {ExecutionTimeoutSeconds} seconds.", message: "PowerShellReadTool");
        }
        catch (Exception ex)
        {
            return PowerShellResult.Fail(error:$"PowerShell query failed: {Helpers.CleanValue(ex.Message)}{FormatModulePathNote()}", message: "PowerShellReadTool");
        }
        finally
        {
            _runspaceSemaphore.Release();
        }
    }

    /// <summary>
    ///     Invokes the pipeline asynchronously and aborts it when the operation exceeds
    ///     <see cref="ExecutionTimeoutSeconds" /> or the caller cancels.
    /// </summary>
    /// <param name="ps">The prepared pipeline to invoke.</param>
    /// <param name="cancellationToken">Cancels the invocation.</param>
    /// <returns>The collected output objects.</returns>
    /// <exception cref="OperationCanceledException">
    ///     Thrown when the timeout elapses or <paramref name="cancellationToken" /> is signalled.
    ///     <see cref="PowerShell.Stop" /> is called first so the runspace is released for reuse.
    /// </exception>
    private static async Task<PSDataCollection<PSObject>> InvokeWithTimeoutAsync(PowerShell ps, CancellationToken cancellationToken)
    {
        using CancellationTokenSource timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(TimeSpan.FromSeconds(ExecutionTimeoutSeconds));

        IAsyncResult asyncResult = ps.BeginInvoke();
        TaskCompletionSource<bool> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

        // A synchronous WaitOne cannot be cancelled once it has started, which is why the wait is
        // bridged to a Task. RegisterWaitForSingleObject signals the Task when the pipeline finishes;
        // the token registration cancels it, so the timeout always fires.
        RegisteredWaitHandle registration = ThreadPool.RegisterWaitForSingleObject(
            asyncResult.AsyncWaitHandle,
            static (state, _) => ((TaskCompletionSource<bool>)state!).TrySetResult(true),
            completion,
            Timeout.Infinite,
            executeOnlyOnce: true);

        try
        {
            using CancellationTokenRegistration cancellationRegistration = timeoutSource.Token.Register(static state => ((TaskCompletionSource<bool>)state!).TrySetCanceled(), completion);

            await completion.Task.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Stops the running pipeline; without this the long-lived runspace stays busy and later
            // calls block on the semaphore forever.
            ps.Stop();
            throw;
        }
        finally
        {
            registration.Unregister(null);
        }

        return ps.EndInvoke(asyncResult);
    }

    /// <summary>
    ///     Tracks how many nodes the projection has emitted for a single result object.
    /// </summary>
    /// <remarks>
    ///     Kept separate from <c>visited</c>, which holds only the current ancestor chain so that a
    ///     shared child reached from two siblings is still projected. Counting ancestors would let a
    ///     wide graph emit an unbounded number of nodes.
    /// </remarks>
    private sealed class NodeBudget
    {
        /// <summary>The number of nodes emitted so far.</summary>
        public int Emitted { get; private set; }

        /// <summary>Reserves one node, reporting whether the budget still allows it.</summary>
        /// <returns><see langword="true" /> while under <see cref="MaxProjectionNodes" />.</returns>
        public bool TryConsume() => ++Emitted <= MaxProjectionNodes;
    }

    /// <summary>
    ///     Projects an arbitrary pipeline object into a bounded, JSON-serializable graph.
    /// </summary>
    /// <param name="value">The value to project.</param>
    /// <param name="depth">The current recursion depth.</param>
    /// <param name="visited">Objects on the current ancestor chain, used to break cycles.</param>
    /// <param name="budget">The per-result node budget.</param>
    /// <returns>A scalar, a dictionary of property values, a list, or a bounded string.</returns>
    private static object? ProjectValue(object? value, int depth, HashSet<object> visited, NodeBudget budget)
    {
        if (!budget.TryConsume())
        {
            return "[projection truncated: node budget exceeded]";
        }

        if (value is null)
        {
            return null;
        }

        // PSObject wraps the real value; unwrapping also avoids projecting PowerShell's
        // adaptation members (PSPath, PSDrive, ...) that are not diagnostic content. The
        // wrapper reference is kept so note properties (PSCustomObject instances unwrap to
        // an object with no reflection-visible properties) can be recovered below.
        PSObject? wrapper = value as PSObject;
        if (wrapper is not null)
        {
            value = wrapper.BaseObject;
        }

        if (value is null or string)
        {
            return Truncate(value as string);
        }

        if (IsJsonScalar(value))
        {
            return value;
        }

        if (value is IDictionary dictionary)
        {
            Dictionary<string, object?> entries = new(StringComparer.Ordinal);
            foreach (DictionaryEntry entry in dictionary)
            {
                entries[entry.Key?.ToString() ?? string.Empty] = ProjectValue(entry.Value, depth + 1, visited, budget);
            }

            return entries;
        }

        if (value is IEnumerable sequence)
        {
            // PSObject wrapping an array may carry note properties that belong to the
            // collection itself. ConvertTo-Json silently dropped them, hiding columns;
            // they are preserved in an envelope ahead of the sequence body.
            List<PSNoteProperty> noteProperties = wrapper is not null
                ? [.. wrapper.Properties.OfType<PSNoteProperty>()]
                : [];

            List<object?> items = [];
            foreach (object? item in sequence)
            {
                if (items.Count >= MaxProjectionNodes)
                {
                    items.Add($"[sequence truncated at {MaxProjectionNodes} items]");
                    break;
                }

                items.Add(ProjectValue(item, depth + 1, visited, budget));
            }

            if (noteProperties.Count > 0)
            {
                Dictionary<string, object?> envelope = new(StringComparer.Ordinal)
                {
                        ["Count"] = items.Count,
                        ["Items"] = items
                };
                foreach (PSNoteProperty note in noteProperties)
                {
                    envelope[note.Name] = ProjectValue(note.Value, depth + 1, visited, budget);
                }

                return envelope;
            }

            return items;
        }

        // Depth and cycle checks bound the walk. Several .NET accessors (for example
        // ServiceController.DependentServices) build a fresh wrapper on every read, so
        // reference-identity checks alone never fire; the node budget covers that fan-out.
        if (depth >= MaxProjectionDepth || !visited.Add(value))
        {
            return Truncate(value.ToString());
        }

        try
        {
            Dictionary<string, object?> projected = new(StringComparer.Ordinal);
            foreach (PropertyInfo property in value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.GetIndexParameters().Length != 0 || property.GetMethod is null)
                {
                    continue;
                }

                object? propertyValue;
                try
                {
                    propertyValue = property.GetValue(value);
                }
                catch (Exception ex) when (ex is TargetInvocationException or TargetParameterCountException or NotSupportedException or InvalidOperationException)
                {
                    // A single throwing accessor must not fail the whole query.
                    projected[property.Name] = null;
                    continue;
                }

                projected[property.Name] = ProjectValue(propertyValue, depth + 1, visited, budget);
            }

            // A PSCustomObject (or any PSObject with note properties) has no CLR properties but
            // can carry ad-hoc members. ConvertTo-Json rendered these; reflection would return
            // an empty object and silently hide every column, so project them explicitly.
            if (projected.Count == 0 && wrapper is not null)
            {
                foreach (PSPropertyInfo note in wrapper.Properties)
                {
                    if (note is PSNoteProperty noteProperty)
                    {
                        projected[note.Name] = ProjectValue(noteProperty.Value, depth + 1, visited, budget);
                    }
                }
            }

            return projected;
        }
        finally
        {
            // visited is the ancestor path, not a global set: a shared child reached from two
            // sibling properties must still be projected in full both times.
            visited.Remove(value);
        }
    }

    /// <summary>
    ///     Projects a single pipeline item into a bounded, JSON-serializable graph.
    /// </summary>
    /// <param name="item">The pipeline item to project.</param>
    /// <returns>A scalar, a dictionary, a list of lines, or a bounded string.</returns>
    /// <remarks>
    ///     Top-level string rows are split into a cleaned, bounded list of lines. Text-mode
    ///     console commands (ipconfig, netstat, auditpol, pnputil, ...) produce one long
    ///     string per item; truncating that string at the property cap would cut mid-line and
    ///     lose columns. A line list keeps every column visible within bounded size.
    /// </remarks>
    private static object? ProjectRow(PSObject item)
    {
        // Text-mode console output arrives as one or more plain strings. Splitting into
        // lines keeps the payload bounded without cutting a single line mid-column.
        if (item.BaseObject is string text)
        {
            List<string> lines = Helpers.CleanOutputLines(text, MaxConsoleOutputLines);
            return lines.Count == 0 ? "(no output)" : lines;
        }

        return ProjectValue(item, 0, new HashSet<object>(), new NodeBudget());
    }

    /// <summary>
    ///     Truncates a string to <see cref="MaxPropertyStringLength" /> and strips control characters
    ///     so the payload remains clean and single-line.
    /// </summary>
    /// <param name="value">The string to clean.</param>
    /// <returns>The cleaned, bounded string, or <see langword="null" /> when the input was null.</returns>
    private static string? Truncate(string? value)
    {
        if (value is null)
        {
            return null;
        }

        string cleaned = Helpers.CleanValue(value);
        return cleaned.Length <= MaxPropertyStringLength ? cleaned : cleaned[..MaxPropertyStringLength] + "...";
    }

    /// <summary>
    ///     The typed payload returned by <c>PowerShell_Query</c>. Every truncation that can
    ///     occur is declared as an explicit field so a model can tell bounded output from
    ///     complete output without guessing.
    /// </summary>
    /// <param name="Command">The command that produced the rows.</param>
    /// <param name="ReturnedRows">The number of rows actually returned.</param>
    /// <param name="MaxResults">The requested row limit.</param>
    /// <param name="RowLimitReached">True when more rows existed than MaxResults; page with skip.</param>
    /// <param name="ProjectionTruncated">True when the query node budget stopped the projection early.</param>
    /// <param name="Rows">The projected rows.</param>
    public sealed record PowerShellQueryPayload(
        string Command,
        int ReturnedRows,
        int MaxResults,
        bool RowLimitReached,
        bool ProjectionTruncated,
        List<object?> Rows);

    /// <summary>
    ///     Validates that a command string is a single pipeline stage built from allow-listed
    ///     commands, by parsing it and inspecting the resulting AST.
    /// </summary>
    /// <remarks>
    ///     Parsing replaces substring matching, which both rejected legitimate arguments (a
    ///     hyphenated value looked like a cmdlet) and could be dodged with quoting or escaping. The
    ///     AST cannot lie about structure, so operators, redirects, subexpressions, script blocks,
    ///     assignments, and member or type access are refused by shape rather than by spelling.
    /// </remarks>
    /// <param name="command">The command text to validate.</param>
    /// <returns><see langword="null" /> when the command is permitted, otherwise the reason.</returns>
    public static string? ValidateCommand(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return "Command is required.";
        }

        ScriptBlockAst? ast = Parser.ParseInput(command, out _, out ParseError[] parseErrors);
        if (ast is null || parseErrors.Length > 0)
        {
            string detail = ast is null ? "the parser returned no AST" : parseErrors[0].Message;
            return $"Command could not be parsed: {detail}";
        }

        // A script block with a name, parameters, begin/process blocks or any statement other than
        // one pipeline is not a single read-only query.
        if (ast.ParamBlock is not null || ast.BeginBlock is not null || ast.ProcessBlock is not null)
        {
            return "Command must be a single pipeline. Script blocks, param and begin/process blocks are not allowed.";
        }

        IReadOnlyList<StatementAst> statements = ast.EndBlock?.Statements ?? [];
        if (statements.Count != 1)
        {
            return "Command must contain exactly one statement. Statement separators are not allowed.";
        }

        if (statements[0] is not PipelineAst pipeline)
        {
            return $"Command must be a single pipeline of allow-listed commands. A {statements[0].GetType().Name} statement is not permitted.";
        }

        if (pipeline.PipelineElements.Count != 1)
        {
            return "Command must contain exactly one pipeline stage. The pipeline operator is not allowed.";
        }

        if (pipeline.PipelineElements[0] is not CommandAst commandAst)
        {
            return "Command must invoke an allow-listed command. Expression pipelines are not permitted.";
        }

        if (commandAst.Redirections.Count > 0)
        {
            return "Command must not contain redirections.";
        }

        if (commandAst.InvocationOperator is not (TokenKind.Unknown or TokenKind.Ampersand))
        {
            return $"The invocation operator '{commandAst.InvocationOperator}' is not allowed.";
        }

        if (commandAst.CommandElements.Count == 0)
        {
            return "Command is required.";
        }

        foreach (CommandElementAst element in commandAst.CommandElements)
        {
            // Only literal arguments and parameters are permitted. Variables, subexpressions, arrays,
            // hashtables, member access and type literals all land in the rejected branch.
            if (element is not (StringConstantExpressionAst or CommandParameterAst))
            {
                return $"Command argument '{element.Extent.Text}' is not a literal value. Variables, subexpressions and expression arguments are not allowed.";
            }
        }

        string commandName = commandAst.GetCommandName();
        if (string.IsNullOrEmpty(commandName))
        {
            return "Command must invoke a named, allow-listed command.";
        }

        if (ForbiddenCommands.Contains(commandName))
        {
            return $"Command '{commandName}' is explicitly forbidden.";
        }

        if (!AllowedCommands.Contains(commandName))
        {
            return $"Command '{commandName}' is not in the allowed list. Only pre-approved read-only commands are permitted.";
        }

        return null;
    }

    /// <summary>
    ///     Determines whether a value can be handed to <see cref="JsonSerializer" /> unchanged.
    /// </summary>
    /// <param name="value">The value to test.</param>
    /// <returns><see langword="true" /> when the value is a JSON-serializable scalar.</returns>
    private static bool IsJsonScalar(object value)
    {
        Type type = value.GetType();

        return type.IsPrimitive
               || type.IsEnum
               || value is string or decimal or DateTime or DateTimeOffset or DateOnly or TimeOnly
                   or TimeSpan or Guid or Uri or Version or JsonElement;
    }







    /// <summary>
    ///     Resolves PowerShell module locations on any default Windows 10/11 installation
    ///     and prepends them to the process-level PSModulePath before the first runspace is
    ///     created. The hosted PowerShell engine (System.Management.Automation NuGet package)
    ///     derives $PSHOME from this application's own directory, so real module locations
    ///     are never discovered automatically.
    /// </summary>
    private static class PowerShellModulePathResolver
    {
        /// <summary>True once the process environment has been configured.</summary>
        private static bool _configured;

        /// <summary>
        ///     Describes why no module location could be resolved, or an empty string when
        ///     at least one location was found. Surfaced through tool failure results.
        /// </summary>
        internal static string ConfigurationProblem { get; private set; } = string.Empty;








        /// <summary>
        ///     Configures the process PSModulePath exactly once. Repeated calls return
        ///     immediately. Never throws; failures are recorded in
        ///     <see cref="ConfigurationProblem" /> and surfaced through tool results.
        /// </summary>
        internal static void EnsureConfigured()
        {
            if (_configured)
            {
                return;
            }

            _configured = true;

            try
            {
                List<string> roots = [];
                List<string> searched = [];

                foreach ((string root, string sentinelModule) in EnumerateCandidates())
                {
                    searched.Add(root);
                    if (IsValidModuleRoot(root, sentinelModule) && !roots.Contains(root, StringComparer.OrdinalIgnoreCase))
                    {
                        roots.Add(root);
                    }
                }

                if (roots.Count == 0)
                {
                    ConfigurationProblem = "No PowerShell module locations were found. Searched: " + string.Join(", ", searched);
                    return;
                }

                string current = Environment.GetEnvironmentVariable("PSModulePath", EnvironmentVariableTarget.Process) ?? string.Empty;
                List<string> parts = [.. current.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];

                // Prepend in reverse order so the first resolved root ends up first in the
                // module search order, matching how a real PowerShell 7 session is built.
                for (int i = roots.Count - 1; i >= 0; i--)
                {
                    if (!parts.Contains(roots[i], StringComparer.OrdinalIgnoreCase))
                    {
                        parts.Insert(0, roots[i]);
                    }
                }

                Environment.SetEnvironmentVariable("PSModulePath", string.Join(';', parts), EnvironmentVariableTarget.Process);
            }
            catch (Exception ex)
            {
                ConfigurationProblem = "Failed to configure PSModulePath: " + ex.Message;
            }
        }








        /// <summary>
        ///     Yields candidate module roots with their sentinel module folder, in priority
        ///     order: PowerShell 7 core module locations first (default install, then
        ///     installer registry, uninstaller registry, App Paths, and PATH fallbacks for
        ///     custom installs), then the inbox Windows PowerShell module location that
        ///     ships with every Windows 10/11 installation.
        /// </summary>
        private static IEnumerable<(string Root, string SentinelModule)> EnumerateCandidates()
        {
            const string coreSentinel = "Microsoft.PowerShell.Utility";
            const string inboxSentinel = "ScheduledTasks";

            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            yield return (Path.Combine(programFiles, "PowerShell", "7", "Modules"), coreSentinel);

            foreach (string installRoot in GetPowerShellCoreInstallLocations())
            {
                yield return (Path.Combine(installRoot, "Modules"), coreSentinel);
            }

            foreach (string installRoot in GetUninstallerInstallLocations())
            {
                yield return (Path.Combine(installRoot, "Modules"), coreSentinel);
            }

            string? appPathsExecutable = GetAppPathsPwshExecutable();
            if (!string.IsNullOrEmpty(appPathsExecutable))
            {
                string? appPathsDirectory = Path.GetDirectoryName(appPathsExecutable);
                if (!string.IsNullOrEmpty(appPathsDirectory))
                {
                    yield return (Path.Combine(appPathsDirectory, "Modules"), coreSentinel);
                }
            }

            string? pathVariable = Environment.GetEnvironmentVariable("Path");
            if (!string.IsNullOrEmpty(pathVariable))
            {
                foreach (string pathDirectory in pathVariable.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    if (File.Exists(Path.Combine(pathDirectory, "pwsh.exe")))
                    {
                        yield return (Path.Combine(pathDirectory, "Modules"), coreSentinel);
                    }
                }
            }

            yield return (Path.Combine(Environment.SystemDirectory, "WindowsPowerShell", "v1.0", "Modules"), inboxSentinel);
        }








        /// <summary>
        ///     Reads the pwsh.exe path from the App Paths registration, when present.
        /// </summary>
        private static string? GetAppPathsPwshExecutable()
        {
            using RegistryKey? key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\App Paths\pwsh.exe");
            return key?.GetValue(null) as string;
        }








        /// <summary>
        ///     Reads install roots recorded by the PowerShell 7 installer under the
        ///     PowerShellCore\InstalledVersions registry key (machine, 32-bit-on-64, and
        ///     per-user hives).
        /// </summary>
        private static IEnumerable<string> GetPowerShellCoreInstallLocations()
        {
            const string machinePath = @"Software\Microsoft\PowerShellCore\InstalledVersions";

            foreach ((RegistryKey hive, string keyPath) in new[] { (Registry.LocalMachine, machinePath), (Registry.LocalMachine, @"Software\WOW6432Node\Microsoft\PowerShellCore\InstalledVersions"), (Registry.CurrentUser, machinePath) })
            {
                using RegistryKey? root = hive.OpenSubKey(keyPath);
                if (root is null)
                {
                    continue;
                }

                foreach (string subKeyName in root.GetSubKeyNames())
                {
                    using RegistryKey? subKey = root.OpenSubKey(subKeyName);
                    if (subKey?.GetValue("InstallLocation") is string location && !string.IsNullOrWhiteSpace(location))
                    {
                        yield return location;
                    }
                }
            }
        }








        /// <summary>
        ///     Reads install roots from the Windows uninstaller registry for entries whose
        ///     display name identifies a PowerShell installation.
        /// </summary>
        private static IEnumerable<string> GetUninstallerInstallLocations()
        {
            const string machinePath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall";

            foreach ((RegistryKey hive, string keyPath) in new[] { (Registry.LocalMachine, machinePath), (Registry.LocalMachine, @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"), (Registry.CurrentUser, machinePath) })
            {
                using RegistryKey? root = hive.OpenSubKey(keyPath);
                if (root is null)
                {
                    continue;
                }

                foreach (string subKeyName in root.GetSubKeyNames())
                {
                    using RegistryKey? subKey = root.OpenSubKey(subKeyName);
                    if (subKey?.GetValue("DisplayName") is not string displayName || !displayName.StartsWith("PowerShell", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (subKey.GetValue("InstallLocation") is string location && !string.IsNullOrWhiteSpace(location))
                    {
                        yield return location;
                    }
                }
            }
        }








        /// <summary>
        ///     A candidate root is valid only when the directory exists and contains the
        ///     sentinel module folder, proving that real modules are installed there.
        /// </summary>
        private static bool IsValidModuleRoot(string root, string sentinelModule)
        {
            return !string.IsNullOrWhiteSpace(root) && Directory.Exists(root) && Directory.Exists(Path.Combine(root, sentinelModule));
        }
    }
}
