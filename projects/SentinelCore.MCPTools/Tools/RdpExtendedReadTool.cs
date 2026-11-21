// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         RdpExtendedReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.ComponentModel;
using System.Runtime.Versioning;

using ModelContextProtocol.Server;

using SentinelCoreMCP.Tools.Interop;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Read-only tool for querying active RDP sessions on the local machine.
///     Uses the WTS API via <see cref="Interop.SessionHelper" /> instead of shelling
///     out to the <c>query session</c> command.
/// </summary>
[McpServerToolType]
[SupportedOSPlatform("windows")]
public sealed class RdpExtendedReadTool
{

    /// <summary>
    ///     Lists active RDP sessions on the local machine.
    /// </summary>
    /// <param name="maxRecords">Maximum number of sessions to return. Defaults to 50.</param>
    /// <returns>A <see cref="ToolResult" /> containing typed session records.</returns>
    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_identity_list_rdp_sessions", ReadOnly = true, Destructive = false)]
    [Description("Lists active RDP sessions on the local machine.")]
    public async Task<ToolResult> RdpListSessionsAsync([Description("Maximum number of sessions to return. Defaults to 50.")] int maxRecords = 50)
    {
        try
        {
            ToolResult? maxValidation = InputValidator.ValidateMaxRecords(maxRecords);
            if (maxValidation is not null)
            {
                return maxValidation;
            }

            return await SessionHelper.EnumerateSessionsAsync(maxRecords).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "RDP session listing");
        }
    }
}
