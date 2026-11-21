// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         ServerManifestReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Versioning;
using System.Security.Principal;
using System.Text;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Builds the server's tool manifest from the registered <see cref="McpServerToolTypeAttribute" />
///     classes and their <see cref="McpServerToolAttribute" /> methods. Reflection runs once and the
///     result is cached, so the manifest, the <c>sc_utilities_read_server_manifest</c> tool, and the
///     server instructions always agree: there is one source of truth for the tool catalogue.
/// </summary>
internal static class ServerManifestBuilder
{
    private static readonly Lock CacheGate = new();

    private static (string Instructions, object Payload)? _cache;








    /// <summary>
    ///     Returns the cached manifest, building it on first use via reflection over every
    ///     registered <see cref="McpServerToolAttribute" /> in this assembly.
    /// </summary>
    /// <returns>The instructions text and the structured manifest payload.</returns>
    public static (string Instructions, object Payload) GetManifest()
    {
        lock (CacheGate)
        {
            if (_cache is not null)
            {
                return _cache.Value;
            }

            List<string> toolNames = CollectRegisteredToolNames();
            List<ToolGroupManifest> groups = GroupToolNames(toolNames);

            string version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "unknown";

            string[] whitelist = [.. PowerShellReadTool.AllowedCommands.Order(StringComparer.OrdinalIgnoreCase)];

            object payload = new
            {
                    Server = "io.github.kylec69/SentinelCoreMCP",
                    Version = version,
                    TotalTools = toolNames.Count,
                    NamingScheme = "sc_<groupprefix>_<verb>_<noun>",
                    Groups = groups,
                    PowerShellWhitelist = whitelist,
                    ProcessId = Environment.ProcessId,
                    OperatingSystem = Environment.OSVersion.VersionString,
                    IsElevated = IsElevated()
            };

            string instructions = BuildInstructions(version, toolNames.Count, groups, whitelist);

            _cache = (instructions, payload);
            return _cache.Value;
        }
    }








    /// <summary>
    ///     Produces the server instructions text: naming scheme, groups with counts, the
    ///     generated-from-code tool contract summary, and how to retrieve the exact catalogue.
    /// </summary>
    /// <param name="version">Server version.</param>
    /// <param name="totalTools">Total registered tool count.</param>
    /// <param name="groups">The grouped tool manifest.</param>
    /// <param name="whitelist">The sorted PowerShell whitelist.</param>
    /// <returns>Plain text safe for the model to consume.</returns>
    private static string BuildInstructions(string version, int totalTools, List<ToolGroupManifest> groups, string[] whitelist)
    {
        StringBuilder sb = new();
        sb.AppendLine($"SentinelCore-MCP {version} provides {totalTools} read-only tools for inspecting Windows system state.");

        sb.Append("Tool names follow sc_<groupprefix>_<verb>_<noun>. Groups: ");
        sb.Append(string.Join(", ", groups.Select(g => $"{g.Prefix} ({g.ToolCount})")));
        sb.AppendLine(".");

        sb.AppendLine("Every tool returns ToolResult with Success, Results, Message, and, on failure, ErrorDetails plus ErrorCode and ErrorCategory.");
        sb.AppendLine("All tools declare ReadOnly = true; only sc_memoryinspection_capture_process_dump returns raw dump output and it never modifies the host.");

        sb.Append("sc_utilities_query_powershell accepts only a single whitelisted read-only command, no pipelines or redirections. Allowed commands: ");
        sb.Append(string.Join(", ", whitelist));
        sb.AppendLine(".");
        sb.AppendLine("Call sc_utilities_read_server_manifest for the exact tool catalogue, group counts, process identity, and elevation state.");

        return sb.ToString();
    }








    /// <summary>
    ///     Collects every registered MCP tool name in this assembly by reflecting over
    ///     <see cref="McpServerToolTypeAttribute" /> classes and their
    ///     <see cref="McpServerToolAttribute" /> methods. Deterministic: names are sorted.
    /// </summary>
    /// <returns>The sorted list of registered tool names.</returns>
    private static List<string> CollectRegisteredToolNames()
    {
        SortedSet<string> names = new(StringComparer.OrdinalIgnoreCase);

        foreach (Type type in typeof(ServerManifestBuilder).Assembly.GetTypes())
        {
            if (!type.IsClass || type.IsAbstract || type.GetCustomAttribute<McpServerToolTypeAttribute>() is null)
            {
                continue;
            }

            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                if (method.GetCustomAttribute<McpServerToolAttribute>() is McpServerToolAttribute toolAttribute && !string.IsNullOrWhiteSpace(toolAttribute.Name))
                {
                    names.Add(toolAttribute.Name);
                }
            }
        }

        return [.. names];
    }








    /// <summary>
    ///     Groups tool names by the <c>sc_</c> group segment (the element after the root).
    ///     Names that can be parsed are grouped; every other case is counted as unknown so
    ///     the manifest still accounts for the full catalogue.
    /// </summary>
    /// <param name="toolNames">All registered tool names.</param>
    /// <returns>One <see cref="ToolGroupManifest" /> per observed group segment.</returns>
    private static List<ToolGroupManifest> GroupToolNames(List<string> toolNames)
    {
        // The canonical groups and purposes from the ADR-006 scheme (Docs/ToolingComponent.md).
        Dictionary<string, string> descriptions = new(StringComparer.OrdinalIgnoreCase)
        {
                ["casework"] = "Case lifecycle, evidence and report persistence",
                ["execution"] = "Process and command execution",
                ["persistence"] = "Scheduled tasks, services, drivers",
                ["forensics"] = "Artefact and timeline examination",
                ["memoryinspection"] = "Process memory and module inspection",
                ["identity"] = "Accounts, groups, tokens, privileges",
                ["networkaccess"] = "Connections, listeners, shares, remote access",
                ["cryptography"] = "Certificates, keys, hashing",
                ["hardening"] = "Policy, audit, firewall, Defender",
                ["configuration"] = "Registry, files, environment, installed software",
                ["hardware"] = "Devices, drivers, firmware, storage",
                ["utilities"] = "Shell, scripting, formatting helpers",
                ["webresearch"] = "External research and documentation lookup",
                ["internal"] = "In-process AIFunction tools hosted by Orchestrations"
        };

        Dictionary<string, List<string>> byGroup = new(StringComparer.OrdinalIgnoreCase);
        List<string> unknown = [];

        foreach (string name in toolNames)
        {
            string[] parts = name.Split('_');
            if (parts.Length >= 3 && string.Equals(parts[0], "sc", StringComparison.OrdinalIgnoreCase))
            {
                (byGroup.TryGetValue(parts[1], out List<string>? list) ? list : byGroup[parts[1]] = []).Add(name);
                continue;
            }

            unknown.Add(name);
        }

        List<ToolGroupManifest> groups = [];

        foreach (KeyValuePair<string, List<string>> pair in byGroup)
        {
            descriptions.TryGetValue(pair.Key, out string? description);
            groups.Add(new ToolGroupManifest(pair.Key, $"sc_{pair.Key.ToLowerInvariant()}_", description ?? "Uncatalogued group", pair.Value.Count, pair.Value));
        }

        if (unknown.Count > 0)
        {
            groups.Add(new ToolGroupManifest("unknown", string.Empty, "Tool names that do not follow the sc_ scheme", unknown.Count, unknown));
        }

        return [.. groups.OrderBy(g => g.Name, StringComparer.OrdinalIgnoreCase)];
    }








    /// <summary>
    ///     Reports whether the server process is running with an elevated token.
    /// </summary>
    /// <returns>True when the process token is elevated.</returns>
    private static bool IsElevated()
    {
        try
        {
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();
            return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }








    /// <summary>
    ///     One named group in the server manifest.
    /// </summary>
    /// <param name="Name">The group segment, e.g. "forensics".</param>
    /// <param name="Prefix">The full name prefix, e.g. "sc_forensics_".</param>
    /// <param name="Description">The group purpose from the scheme.</param>
    /// <param name="ToolCount">The number of tools in the group.</param>
    /// <param name="Tools">The exact tool names in the group.</param>
    public sealed record ToolGroupManifest(string Name, string Prefix, string Description, int ToolCount, List<string> Tools);
}





/// <summary>
///     Read-only tool exposing the server's own manifest: registered tools grouped by
///     <c>sc_</c> prefix, the PowerShell whitelist, process identity, and elevation state.
///     Answers "what tools exist and how are they named" without any client guesswork.
/// </summary>
[McpServerToolType]
[SupportedOSPlatform("windows")]
public sealed class ServerManifestReadTool
{
    /// <summary>
    ///     Reads the server manifest: the exact tool catalogue grouped by name prefix, the
    ///     PowerShell command whitelist, the server version, process identity, and whether
    ///     the process token is elevated.
    /// </summary>
    /// <returns>A <see cref="ToolResult" /> containing the server manifest.</returns>
    [McpServerTool(Name = "sc_utilities_read_server_manifest", ReadOnly = true, Destructive = false)]
    [Description("Reads the server manifest: the exact tool catalogue grouped by sc_ prefix, the PowerShell whitelist, server version, process identity, and elevation state.")]
    public ToolResult ReadManifest()
    {
        try
        {
            (string _, object payload) = ServerManifestBuilder.GetManifest();
            return ToolResult.Ok(payload, "Server manifest read.");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "Server manifest read");
        }
    }
}
