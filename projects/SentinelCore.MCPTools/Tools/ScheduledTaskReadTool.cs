// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         ScheduledTaskReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104


using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Versioning;

using Microsoft.Win32.TaskScheduler;

using ModelContextProtocol.Server;

using ScheduledTask = Microsoft.Win32.TaskScheduler.Task;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Read-only tool for querying Windows Scheduled Tasks.
/// </summary>
[McpServerToolType]
[SupportedOSPlatform("windows")]
public sealed class ScheduledTaskReadTool
{
    [McpServerTool(Name = "sc_persistence_list_tasks", ReadOnly = true, Destructive = false)]
    [Description("Lists scheduled tasks in the specified folder path.")]
    public  ToolResult ScheduledTaskListAsync([Description("The task folder path, e.g. \\ or \\Microsoft\\Windows.")] string folderPath = "\\", [Description("Maximum number of tasks to return. Defaults to 50.")] int maxRecords = 50)
    {
        try
        {
            using TaskService taskService = new();
            TaskFolder? folder = taskService.GetFolder(folderPath);
            if (folder is null)
            {
                return ToolResult.Fail($"Task folder not found: {folderPath}", "ScheduledTaskReadTool");
            }

            List<object> results = new();
            int count = 0;
            foreach (ScheduledTask scheduledTask in folder.Tasks)
            {
                if (count >= maxRecords)
                {
                    break;
                }

                results.Add(new { Name = Helpers.CleanValue(scheduledTask.Name), Path = Helpers.CleanValue(scheduledTask.Path), State = scheduledTask.State.ToString(), scheduledTask.Enabled });
                count++;
            }

            foreach (TaskFolder subFolder in folder.SubFolders)
            {
                results.Add(new { Folder = Helpers.CleanValue(subFolder.Path) });
            }

            return ToolResult.Ok(results, "ScheduledTaskReadTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "Task folder listing");
        }
    }








    [McpServerTool(Name = "sc_persistence_read_task", ReadOnly = true, Destructive = false)]
    [Description("Reads details of a specific scheduled task.")]
    public async Task<ToolResult> ScheduledTaskReadAsync([Description("The full task path, e.g. \\Microsoft\\Windows\\Defender\\Defender Scheduled Scan.")] string taskPath)
    {
        if (string.IsNullOrWhiteSpace(taskPath))
        {
            return ToolResult.Fail("Operation failed", "ScheduledTaskReadTool");
        }

        try
        {
            await System.Threading.Tasks.Task.Delay(1);
            using TaskService taskService = new();
            ScheduledTask? scheduledTask = taskService.GetTask(taskPath);
            if (scheduledTask is null)
            {
                return ToolResult.Fail($"Task not found: {taskPath}", "ScheduledTaskReadTool");
            }

            var result = new
            {
                    Name = Helpers.CleanValue(scheduledTask.Name),
                    Path = Helpers.CleanValue(scheduledTask.Path),
                    scheduledTask.Enabled,
                    State = scheduledTask.State.ToString(),
                    LastRunTime = scheduledTask.LastRunTime.ToString(),
                    NextRunTime = scheduledTask.NextRunTime.ToString(),
                    scheduledTask.LastTaskResult,
                    scheduledTask.NumberOfMissedRuns,
                    TriggerCount = scheduledTask.Definition.Triggers.Count,
                    ActionCount = scheduledTask.Definition.Actions.Count,
                    scheduledTask.Definition.Settings.AllowDemandStart,
                    scheduledTask.Definition.Settings.StartWhenAvailable
            };

            return ToolResult.Ok(result, "ScheduledTaskReadTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "ScheduledTaskReadTool");
        }
    }
}
