// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         ScheduledTaskReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  080801



using System.ComponentModel;
using System.Text;

using Microsoft.Win32.TaskScheduler;

using Task = System.Threading.Tasks.Task;




namespace SentinelCore.Tools;





/// <summary>
///     Read-only tool for querying Windows Scheduled Tasks.
/// </summary>
public sealed class ScheduledTaskReadTool : AITool
{
    public override string Description { get; } = "Read-only tool for querying Windows Scheduled Tasks.";
    public override string Name { get; } = "Scheduled_Task_Read";








    [Description("Lists scheduled tasks in the specified folder path.")]
    public Task<ToolResult> scheduled_task_list([Description("The task folder path, e.g. \\\\ or \\\\Microsoft\\Windows.")] string folderPath = "\\\\")
    {
        try
        {
            using TaskService taskService = new();
            TaskFolder? folder = taskService.GetFolder(folderPath);
            if (folder is null)
            {
                return Task.FromResult(ToolResult.Fail($"Task folder not found: {folderPath}"));
            }

            StringBuilder sb = new();
            foreach (Microsoft.Win32.TaskScheduler.Task? scheduledTask in folder.Tasks)
                sb.AppendLine($"Name={scheduledTask.Name}, Path={scheduledTask.Path}, State={scheduledTask.State}, Enabled={scheduledTask.Enabled}");

            foreach (TaskFolder? subFolder in folder.SubFolders) sb.AppendLine($"[Folder] {subFolder.Path}");

            return Task.FromResult(ToolResult.Ok(sb.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail($"Scheduled task listing failed: {ex.Message}"));
        }
    }








    [Description("Reads details of a specific scheduled task.")]
    public Task<ToolResult> scheduled_task_read([Description("The full task path, e.g. \\\\Microsoft\\Windows\\Defender\\Defender Scheduled Scan.")] string taskPath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(taskPath))
            {
                return Task.FromResult(ToolResult.Fail("taskPath is required."));
            }

            using TaskService taskService = new();
            Microsoft.Win32.TaskScheduler.Task? scheduledTask = taskService.GetTask(taskPath);
            if (scheduledTask is null)
            {
                return Task.FromResult(ToolResult.Fail($"Task not found: {taskPath}"));
            }

            StringBuilder sb = new();
            sb.AppendLine($"Name={scheduledTask.Name}");
            sb.AppendLine($"Path={scheduledTask.Path}");
            sb.AppendLine($"Enabled={scheduledTask.Enabled}");
            sb.AppendLine($"State={scheduledTask.State}");
            sb.AppendLine($"LastRunTime={scheduledTask.LastRunTime}");
            sb.AppendLine($"NextRunTime={scheduledTask.NextRunTime}");
            sb.AppendLine($"LastTaskResult={scheduledTask.LastTaskResult}");
            sb.AppendLine($"NumberOfMissedRuns={scheduledTask.NumberOfMissedRuns}");
            sb.AppendLine($"Definition.Triggers.Count={scheduledTask.Definition.Triggers.Count}");
            sb.AppendLine($"Definition.Actions.Count={scheduledTask.Definition.Actions.Count}");
            sb.AppendLine($"Definition.Settings.AllowDemandStart={scheduledTask.Definition.Settings.AllowDemandStart}");
            sb.AppendLine($"Definition.Settings.StartWhenAvailable={scheduledTask.Definition.Settings.StartWhenAvailable}");

            return Task.FromResult(ToolResult.Ok(sb.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail($"Scheduled task read failed: {ex.Message}"));
        }
    }
}