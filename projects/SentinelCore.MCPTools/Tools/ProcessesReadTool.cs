// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         ProcessesReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Versioning;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Read-only tool for enumerating running processes and basic metadata.
///     Uses the managed Process API (a safe read-only interface over ToolHelp32Snapshot / NtQuery APIs).
/// </summary>
[SupportedOSPlatform("windows")]
[McpServerToolType]
public sealed class ProcessesReadTool
{

    [McpServerTool(Name = "sc_execution_list_processes", ReadOnly = true, Destructive = false)]
    [Description("Lists running processes with PID, name, and basic metadata.")]
    public async Task<ToolResult> ProcessListAsync([Description("Optional process name filter (partial match).")] string? nameFilter = null, [Description("Maximum number of processes to return. Defaults to 50.")] int maxRecords = 50)
    {
        try
        {
            List<object> results = new();
            foreach (Process process in Process.GetProcesses())
                try
                {
                    if (!string.IsNullOrWhiteSpace(nameFilter) && process.ProcessName.IndexOf(nameFilter, StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }

                    if (results.Count >= maxRecords)
                    {
                        break;
                    }

                    results.Add(new
                    {
                            process.Id,
                            process.ProcessName,
                            process.MainWindowTitle,
                            process.SessionId,
                            process.Responding,
                            StartTime = SafeGet(() => process.StartTime),
                            WorkingSet = process.WorkingSet64,
                            PagedMemorySize = process.PagedMemorySize64
                    });
                }
                catch
                {

                    // Skip processes we cannot inspect (e.g., protected/elevated).

                }

            return ToolResult.Ok(results, "ProcessesReadTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "Process listing failed.");
        }
    }








    [McpServerTool(Name = "sc_execution_read_process", ReadOnly = true, Destructive = false)]
    [Description("Reads details for a specific process by PID.")]
    public async Task<ToolResult> ProcessReadAsync([Description("The process identifier.")] int processId)
    {
        try
        {
            using Process process = Process.GetProcessById(processId);
            process.Refresh();

            List<object> moduleList = new();
            try
            {
                foreach (ProcessModule module in process.Modules)
                    moduleList.Add(new { module.ModuleName, module.FileName });
            }
            catch
            {

                moduleList.Add(new { ModuleName = "(unavailable)", FileName = "(unavailable)" });
            }

            var processInfo = new
            {
                    process.Id,
                    Name = process.ProcessName,
                    process.MainWindowTitle,
                    process.SessionId,
                    process.Responding,
                    StartTime = SafeGet(() => process.StartTime),
                    process.WorkingSet64,
                    process.PagedMemorySize64,
                    process.VirtualMemorySize64,
                    process.HandleCount,
                    ThreadCount = process.Threads.Count,
                    Modules = moduleList
            };
            return ToolResult.Ok(processInfo, "ProcessesReadTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "Process read failed.");
        }
    }








    private T? SafeGet<T>(Func<T> getter)
    {
        try
        {
            return getter();
        }
        catch
        {

            return default;

        }
    }
}
