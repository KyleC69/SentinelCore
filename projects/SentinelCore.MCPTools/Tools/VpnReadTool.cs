// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         VpnReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.ComponentModel;
using System.Runtime.Versioning;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Read-only tool for enumerating configured VPN connections using the RAS phonebook and registry.
/// </summary>
[McpServerToolType]
[SupportedOSPlatform("windows")]
public sealed class VpnReadTool
{

    private const string RasPhonebookFileName = "rasphone.pbk";








    private static List<Dictionary<string, string?>> ParsePhonebook(string path)
    {
        List<Dictionary<string, string?>> entries = new();
        string[] lines = File.ReadAllLines(path);
        Dictionary<string, string?>? current = null;
        foreach (string raw in lines)
        {
            string line = raw.Trim();
            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                current = new Dictionary<string, string?> { ["Name"] = line.Trim('[', ']') };
                entries.Add(current);
            }
            else if (current is not null && line.Contains('='))
            {
                int idx = line.IndexOf('=');
                current[line[..idx].Trim()] = line[(idx + 1)..].Trim();
            }
        }

        return entries;
    }








    [McpServerTool(Name = "sc_networkaccess_list_vpn_connections", ReadOnly = true, Destructive = false)]
    [Description("Lists configured VPN/RAS connections from the current user's phonebook directory.")]
    public async Task<ToolResult> VpnListConnectionsAsync()
    {
        try
        {
            List<Dictionary<string, string?>> results = new();
            string phonebookDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Network", "Connections", "Pbk");
            string phonebookPath = Path.Combine(phonebookDir, RasPhonebookFileName);
            if (File.Exists(phonebookPath))
            {
                results.AddRange(ParsePhonebook(phonebookPath));
            }

            return ToolResult.Ok(results, "VpnReadTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail($"VPN connection listing failed: {ex.Message}", "VpnReadTool");
        }
    }








    [McpServerTool(Name = "sc_networkaccess_read_vpn_phonebook", ReadOnly = true, Destructive = false)]
    [Description("Reads the phonebook directory path and whether a user phonebook exists.")]
    public async Task<ToolResult> VpnReadPhonebookStatusAsync()
    {
        try
        {
            string phonebookDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Network", "Connections", "Pbk");
            string phonebookPath = Path.Combine(phonebookDir, RasPhonebookFileName);
            bool exists = File.Exists(phonebookPath);
            return ToolResult.Ok($"PhonebookPath={phonebookPath}, Exists={exists}", "VpnReadTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail($"VPN phonebook status read failed: {ex.Message}", "VpnReadTool");
        }
    }
}
