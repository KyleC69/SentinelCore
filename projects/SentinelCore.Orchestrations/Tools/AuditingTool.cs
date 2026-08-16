// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         AuditingTool.cs
// Author: Kyle L. Crowder
// Build Num:  081602



using System.ComponentModel;
using System.Diagnostics;




namespace SentinelCore.Tools;





[Description("Windows Auditing Tool (AuditPol)")]
public sealed class AuditingTool : AITool
{
    public override string Description { get; } = "Tool for querying Windows auditing policy via auditpol.";
    public override string Name { get; } = "Auditing";








    [Description("Gets the auditing policy by running command auditpol.exe /get /category:*")]
    public Task<ToolResult> GetAuditPolicyAsync()
    {
        try
        {
            Process process = new();
            process.StartInfo.FileName = "auditpol.exe";
            process.StartInfo.Arguments = "/get /category:*";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            process.WaitForExit();

            return Task.FromResult(ToolResult.Ok(process.StandardOutput.ReadToEnd()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail(ex.Message));
        }
    }
}