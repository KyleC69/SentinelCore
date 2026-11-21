// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         AuditingTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Versioning;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





[McpServerToolType]
[SupportedOSPlatform("windows")]
public sealed class AuditingTool
{

    [McpServerTool(Name = "sc_hardening_read_audit_policy", ReadOnly = true, Destructive = false)]
    [Description("Gets the auditing policy by running command auditpol.exe /get /category:*")]
    public async Task<ToolResult> GetAuditPolicyAsync()
    {
        try
        {
            using Process process = new();
            process.StartInfo.FileName = "auditpol.exe";
            process.StartInfo.Arguments = "/get /category:*";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.Start();

            string stdout = process.StandardOutput.ReadToEnd();
            string stderr = process.StandardError.ReadToEnd();

            if (!process.WaitForExit(30_000))
            {
                process.Kill();
                return ToolResult.Fail("Audit policy query timed out after 30 seconds.", "AuditingTool");
            }

            if (process.ExitCode != 0)
            {
                return ToolResult.Fail($"Audit policy query exited with code {process.ExitCode}. {stderr}", "AuditingTool");
            }

            return ToolResult.Ok(Helpers.CleanOutputLines(stdout), "AuditingTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "Audit policy query");
        }
    }
}
