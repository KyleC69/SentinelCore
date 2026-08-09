// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         VpnReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  080801



using System.ComponentModel;
using System.Text.Json;




namespace SentinelCore.Tools;





/// <summary>
///     Read-only tool for enumerating configured VPN connections using the RAS phonebook and registry.
/// </summary>
public sealed class VpnReadTool : AITool
{

    private const string RasPhonebookFileName = "rasphone.pbk";
    public override string Description { get; } = "Read-only tool for enumerating configured VPN connections using the RAS phonebook and registry.";
    public override string Name { get; } = "VPN_Read";








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








    [Description("Lists configured VPN/RAS connections from the current user's phonebook directory.")]
    public Task<ToolResult> vpn_list_connections()
    {
        try
        {
            List<Dictionary<string, string?>> results = new();
            string phonebookDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Network", "Connections", "Pbk");
            string phonebookPath = Path.Combine(phonebookDir, RasPhonebookFileName);
            if (File.Exists(phonebookPath)) results.AddRange(ParsePhonebook(phonebookPath));

            string json = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
            return Task.FromResult(ToolResult.Ok(json));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail($"VPN connection listing failed: {ex.Message}"));
        }
    }








    [Description("Reads the phonebook directory path and whether a user phonebook exists.")]
    public Task<ToolResult> vpn_read_phonebook_status()
    {
        try
        {
            string phonebookDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Network", "Connections", "Pbk");
            string phonebookPath = Path.Combine(phonebookDir, RasPhonebookFileName);
            bool exists = File.Exists(phonebookPath);
            return Task.FromResult(ToolResult.Ok($"PhonebookPath={phonebookPath}, Exists={exists}"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail($"VPN phonebook status read failed: {ex.Message}"));
        }
    }
}