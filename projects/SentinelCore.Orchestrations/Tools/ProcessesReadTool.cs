// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         ProcessesReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  081602



using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;




namespace SentinelCore.Tools;





/// <summary>
///     Read-only tool for enumerating running processes and basic metadata.
///     Uses the managed Process API (a safe read-only interface over ToolHelp32Snapshot / NtQuery APIs).
/// </summary>
public sealed class ProcessesReadTool : AITool
{
    public override string Description { get; } = "Read-only tool for enumerating running processes and basic metadata.";
    public override string Name { get; } = "Processes_Read";








    private static T? SafeGet<T>(Func<T> getter)
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








    [Description("Lists running processes with PID, name, and basic metadata.")]
    public Task<ToolResult> process_list([Description("Optional process name filter (partial match).")] string? nameFilter = null)
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

            string json = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
            return Task.FromResult(ToolResult.Ok(json));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail($"Process listing failed: {ex.Message}"));
        }
    }








    [Description("Reads details for a specific process by PID.")]
    public Task<ToolResult> process_read([Description("The process identifier.")] int processId)
    {
        try
        {
            using Process process = Process.GetProcessById(processId);
            process.Refresh();
            StringBuilder sb = new();
            sb.AppendLine($"Id={process.Id}");
            sb.AppendLine($"Name={process.ProcessName}");
            sb.AppendLine($"MainWindowTitle={process.MainWindowTitle}");
            sb.AppendLine($"SessionId={process.SessionId}");
            sb.AppendLine($"Responding={process.Responding}");
            sb.AppendLine($"StartTime={SafeGet(() => process.StartTime)}");
            sb.AppendLine($"WorkingSet64={process.WorkingSet64}");
            sb.AppendLine($"PagedMemorySize64={process.PagedMemorySize64}");
            sb.AppendLine($"VirtualMemorySize64={process.VirtualMemorySize64}");
            sb.AppendLine($"HandleCount={process.HandleCount}");
            sb.AppendLine($"Threads={process.Threads.Count}");
            sb.AppendLine("Modules:");
            try
            {
                foreach (ProcessModule module in process.Modules)
                    sb.AppendLine($"  {module.ModuleName}={module.FileName}");
            }
            catch (Exception ex)
            {
                sb.AppendLine($"  (modules unavailable: {ex.Message})");
            }

            return Task.FromResult(ToolResult.Ok(sb.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail($"Process read failed: {ex.Message}"));
        }
    }
}