// Solution: SentinelCoreLib
// Project:   SentinelCoreLib
// File:         PerformanceReadTool.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.AI;




namespace SentinelCoreLib.Tools;





/// <summary>
///     Read-only tool for querying Windows performance counters (PDH API surface).
///     Uses the .NET PerformanceCounter wrapper, which internally uses PDH.
/// </summary>
public sealed class PerformanceReadTool : AITool
{
    [Description("Lists performance counter categories available on the system.")]
    public Task<ToolResult> performance_list_categories()
    {
        try
        {
            var categories = PerformanceCounterCategory.GetCategories().Select(c => c.CategoryName).OrderBy(n => n).ToList();

            string json = JsonSerializer.Serialize(categories, new JsonSerializerOptions { WriteIndented = true });
            return Task.FromResult(ToolResult.SuccessResult(json));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.FailureResult($"Performance category listing failed: {ex.Message}"));
        }
    }








    [Description("Lists counters for a given performance counter category and optional instance.")]
    public Task<ToolResult> performance_list_counters([Description("The performance counter category name, e.g. Processor.")] string categoryName, [Description("Optional instance name, e.g. _Total.")] string? instanceName = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                return Task.FromResult(ToolResult.FailureResult("categoryName is required."));
            }

            PerformanceCounterCategory category = new(categoryName);
            StringBuilder sb = new();
            sb.AppendLine($"Category={categoryName}");
            sb.AppendLine("Counters:");
            foreach (PerformanceCounter? counter in category.GetCounters(instanceName ?? string.Empty)) sb.AppendLine($"  {counter.CounterName}");

            return Task.FromResult(ToolResult.SuccessResult(sb.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.FailureResult($"Performance counter listing failed: {ex.Message}"));
        }
    }








    [Description("Reads the current value of a performance counter.")]
    public Task<ToolResult> performance_read_counter([Description("The performance counter category name.")] string categoryName, [Description("The counter name, e.g. % Processor Time.")] string counterName, [Description("Optional instance name, e.g. _Total.")] string? instanceName = null, [Description("Optional machine name. Defaults to local.")] string machineName = "")
    {
        try
        {
            if (string.IsNullOrWhiteSpace(categoryName) || string.IsNullOrWhiteSpace(counterName))
            {
                return Task.FromResult(ToolResult.FailureResult("categoryName and counterName are required."));
            }

            using PerformanceCounter counter = string.IsNullOrWhiteSpace(instanceName) ? new(categoryName, counterName, machineName) : new PerformanceCounter(categoryName, counterName, instanceName, machineName);

            counter.NextValue(); // prime
            float value = counter.NextValue();
            return Task.FromResult(ToolResult.SuccessResult($"Counter={categoryName}/{counterName}[{instanceName ?? "(none)"}]={value}"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.FailureResult($"Performance counter read failed: {ex.Message}"));
        }
    }
}