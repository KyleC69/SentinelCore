// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         UacReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  081602



using System.ComponentModel;
using System.Security.Principal;
using System.Text;

using Microsoft.Win32;




namespace SentinelCore.Tools;





/// <summary>
///     Read-only tool for querying UAC and token elevation state.
/// </summary>
public sealed class UacReadTool : AITool
{
    public override string Description { get; } = "Read-only tool for querying UAC and token elevation state.";
    public override string Name { get; } = "UAC_Read";








    [Description("Reads UAC policy settings from the registry.")]
    public Task<ToolResult> uac_read_settings()
    {
        try
        {
            StringBuilder sb = new();
            using RegistryKey? key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", false);
            if (key is null)
            {
                return Task.FromResult(ToolResult.Fail("System UAC policy key not found."));
            }

            foreach (string valueName in key.GetValueNames()) sb.AppendLine($"{valueName}={key.GetValue(valueName)}");

            return Task.FromResult(ToolResult.Ok(sb.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail($"UAC settings read failed: {ex.Message}"));
        }
    }








    [Description("Reports whether the current process token is elevated.")]
    public Task<ToolResult> uac_read_token_elevation()
    {
        try
        {
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new(identity);
            bool isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
            bool elevated = !identity.IsSystem && isAdmin;
            return Task.FromResult(ToolResult.Ok($"IsElevated={elevated}, IsAdministrator={isAdmin}"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail($"Token elevation read failed: {ex.Message}"));
        }
    }
}