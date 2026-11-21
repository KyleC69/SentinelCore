// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         AppLockerReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Runtime.Versioning;
using System.Text;

using JetBrains.Annotations;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Read-only tool for querying AppLocker policy.
/// </summary>
[McpServerToolType]
[SupportedOSPlatform("windows")]
public sealed class AppLockerReadTool
{

    [McpServerTool(Name = "sc_hardening_read_applocker_policy", ReadOnly = true, Destructive = false)]
    [Description("Retrieves the effective AppLocker policy as XML.")]
    [UsedImplicitly]
    public async Task<ToolResult> ApplockerGetEffectivePolicyAsync()
    {
        try
        {
            StringBuilder sb = new();
            InitialSessionState iss = InitialSessionState.CreateDefault();
            // Call RunspaceFactory.CreateRunspace(InitialSessionState) to
            // create the runspace where the pipeline is run.
            Runspace? rs = RunspaceFactory.CreateRunspace(iss);
            rs.Open();

            using PowerShell? ps = PowerShell.Create();
            ps.Runspace = rs;
            ps.AddScript("Import-Module AppLocker -ErrorAction SilentlyContinue; Get-AppLockerPolicy -Effective | ConvertTo-Json -NoTypeInformation");

            Collection<PSObject>? results = ps.Invoke();
            if (ps.HadErrors)
            {
                string errors = string.Join("; ", ps.Streams.Error.Select(e => e.ToString()));
                return ToolResult.Ok($"PowerShell AppLocker query failed: {errors}", "AppLockerReadTool");
            }

            foreach (PSObject result in results) sb.AppendLine(result.ToString());

            return ToolResult.Ok(sb.ToString(), "AppLockerReadTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Ok(ex.Message, "AppLockerReadTool");
        }
    }








    [McpServerTool(Name = "sc_hardening_list_applocker_rules", ReadOnly = true, Destructive = false)]
    [Description("Retrieves AppLocker rule collections from the effective policy.")]
    [UsedImplicitly]
    public async Task<ToolResult> ApplockerListRuleCollectionsAsync()
    {
        try
        {
            StringBuilder sb = new();
            using PowerShell? powerShell = PowerShell.Create(RunspaceMode.NewRunspace);

            powerShell.AddScript("Import-Module AppLocker -ErrorAction SilentlyContinue; $policy = Get-AppLockerPolicy -Effective; $policy.RuleCollections | Select-Object Name, RuleCollectionType, EnforcementMode | ConvertTo-Json");
            Collection<PSObject>? results = powerShell.Invoke();
            if (powerShell.HadErrors)
            {
                string errors = string.Join("; ", powerShell.Streams.Error.Select(e => e.ToString()));
                return ToolResult.Ok($"PowerShell AppLocker rule listing failed: {errors}", "AppLockerReadTool");
            }

            foreach (PSObject result in results) sb.AppendLine(result.ToString());

            return ToolResult.Ok(sb.ToString(), "AppLockerReadTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Ok(ex.Message, "AppLockerReadTool");
        }
    }
}
