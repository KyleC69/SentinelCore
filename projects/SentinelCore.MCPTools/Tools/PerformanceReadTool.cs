// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         PerformanceReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Read-only tool for querying Windows performance counters (PDH API surface).
///     Uses the .NET PerformanceCounter wrapper, which internally uses PDH.
/// </summary>
[McpServerToolType]
public sealed class PerformanceReadTool
{

    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_hardware_list_perf_categories", ReadOnly = true, Destructive = false)]
    [Description("Lists performance counter categories available on the system.")]
    public async Task<ToolResult> PerformanceListCategoriesAsync()
    {
        try
        {
            var categories = PerformanceCounterCategory.GetCategories().Select(c => c.CategoryName).OrderBy(n => n).ToList();

            return ToolResult.Ok(categories, "PerformanceReadTool");
        }
        catch
        {

            return ToolResult.Fail("Operation failed", "PerformanceReadTool");

        }
    }








    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_hardware_list_perf_counters", ReadOnly = true, Destructive = false)]
    [Description("Lists counters for a given performance counter category and optional instance.")]
    public async Task<ToolResult> PerformanceListCountersAsync([Description("The performance counter category name, e.g. Processor.")] string categoryName, [Description("Optional instance name, e.g. _Total.")] string? instanceName = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                return ToolResult.Fail("Operation failed", "PerformanceReadTool");
            }

            PerformanceCounterCategory category = new(categoryName);
            StringBuilder sb = new();
            sb.AppendLine($"Category={categoryName}");
            sb.AppendLine("Counters:");
            foreach (PerformanceCounter? counter in category.GetCounters(instanceName ?? string.Empty))
                sb.AppendLine($"  {counter.CounterName}");

            return ToolResult.Ok(sb.ToString(), "PerformanceReadTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "PerformanceReadTool");
        }
    }








    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_hardware_read_perf_counter", ReadOnly = true, Destructive = false)]
    [Description("Reads the current value of a performance counter.")]
    public async Task<ToolResult> PerformanceReadCounterAsync([Description("The performance counter category name.")] string categoryName, [Description("The counter name, e.g. % Processor Time.")] string counterName, [Description("Optional instance name, e.g. _Total.")] string? instanceName = null, [Description("Optional machine name. Defaults to local.")] string machineName = "")
    {
        try
        {
            if (string.IsNullOrWhiteSpace(categoryName) || string.IsNullOrWhiteSpace(counterName))
            {
                return ToolResult.Fail("Operation failed", "PerformanceReadTool");
            }

            // PerformanceCounter rejects an empty MachineName, so a blank value must resolve to the local machine (".").
            string resolvedMachineName = string.IsNullOrWhiteSpace(machineName) ? "." : machineName;
            using PerformanceCounter counter = string.IsNullOrWhiteSpace(instanceName) ? new PerformanceCounter(categoryName, counterName, resolvedMachineName) : new PerformanceCounter(categoryName, counterName, instanceName, resolvedMachineName);

            counter.NextValue(); // prime
            float value = counter.NextValue();
            return ToolResult.Ok($"Counter={categoryName}/{counterName}[{instanceName ?? "(none)"}]={value}", "PerformanceReadTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "PerformanceReadTool");
        }
    }
}
