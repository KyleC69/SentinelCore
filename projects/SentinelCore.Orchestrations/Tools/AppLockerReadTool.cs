// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         AppLockerReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  081602



using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Management.Automation;
using System.Text;

using JetBrains.Annotations;




namespace SentinelCore.Tools;





/// <summary>
///     Read-only tool for querying AppLocker policy.
/// </summary>
public sealed class AppLockerReadTool : AITool
{
    public override string Description { get; } = "Read-only tool for querying AppLocker policy.";
    public override string Name { get; } = "AppLocker_Read";








    [Description("Retrieves the effective AppLocker policy as XML.")]
    [UsedImplicitly]
    public Task<ToolResult> applocker_get_effective_policy()
    {
        try
        {
            StringBuilder sb = new();
            using PowerShell? powerShell = PowerShell.Create();
            powerShell.AddScript("Get-AppLockerPolicy -Effective | ConvertTo-Xml -NoTypeInformation");
            Collection<PSObject>? results = powerShell.Invoke();
            if (powerShell.HadErrors)
            {
                string errors = string.Join("; ", powerShell.Streams.Error.Select(e => e.ToString()));
                return Task.FromResult(ToolResult.Fail($"PowerShell AppLocker query failed: {errors}"));
            }

            foreach (PSObject result in results) sb.AppendLine(result.ToString());

            return Task.FromResult(ToolResult.Ok(sb.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail($"AppLocker policy query failed: {ex.Message}"));
        }
    }








    [Description("Retrieves AppLocker rule collections from the effective policy.")]
    [UsedImplicitly]
    public Task<ToolResult> applocker_list_rule_collections()
    {
        try
        {
            StringBuilder sb = new();
            using PowerShell? powerShell = PowerShell.Create();
            powerShell.AddScript("$policy = Get-AppLockerPolicy -Effective; $policy.RuleCollections | Select-Object Name, RuleCollectionType, EnforcementMode | Format-List | Out-String");
            Collection<PSObject>? results = powerShell.Invoke();
            if (powerShell.HadErrors)
            {
                string errors = string.Join("; ", powerShell.Streams.Error.Select(e => e.ToString()));
                return Task.FromResult(ToolResult.Fail($"PowerShell AppLocker rule listing failed: {errors}"));
            }

            foreach (PSObject result in results) sb.AppendLine(result.ToString());

            return Task.FromResult(ToolResult.Ok(sb.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail($"AppLocker rule collection listing failed: {ex.Message}"));
        }
    }
}