// Solution: SentinelCoreLib
// Project:   SentinelCoreLib
// File:         EventLogReadTool.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



using System.ComponentModel;
using System.Diagnostics.Eventing.Reader;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.AI;




namespace SentinelCoreLib.Tools;





/// <summary>
///     Read-only tool for querying Windows Event Logs.
/// </summary>
public sealed class EventLogReadTool : AITool
{
    [Description("Lists available event log channels.")]
    public Task<ToolResult> event_log_list_channels()
    {
        try
        {
            StringBuilder sb = new();
            foreach (string? logName in EventLogSession.GlobalSession.GetLogNames()) sb.AppendLine(logName);

            return Task.FromResult(ToolResult.SuccessResult(sb.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.FailureResult($"Event log channel listing failed: {ex.Message}"));
        }
    }








    [Description("Queries events from a specific event log channel.")]
    public Task<ToolResult> event_log_query([Description("The event log channel name, e.g. Application or System.")] string channel, [Description("Optional XPath filter expression. Defaults to all events.")] string? query = null, [Description("Maximum number of events to return. Defaults to 50.")] int maxEvents = 50)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(channel))
            {
                return Task.FromResult(ToolResult.FailureResult("channel is required."));
            }

            string xpath = string.IsNullOrWhiteSpace(query) ? "*" : query;
            StringBuilder sb = new();
            int count = 0;
            using EventLogReader reader = new(new EventLogQuery(channel, PathType.LogName, xpath));
            EventRecord? record;
            while ((record = reader.ReadEvent()) is not null && count < maxEvents)
            {
                sb.AppendLine($"TimeCreated={record.TimeCreated}, Level={record.LevelDisplayName}, Provider={record.ProviderName}, Id={record.Id}, Message={record.FormatDescription()}");
                count++;
            }

            return Task.FromResult(ToolResult.SuccessResult(sb.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.FailureResult($"Event log query failed: {ex.Message}"));
        }
    }








    [Description("Reads event log configuration such as retention and file size.")]
    public Task<ToolResult> event_log_read_configuration([Description("The event log channel name.")] string channel)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(channel))
            {
                return Task.FromResult(ToolResult.FailureResult("channel is required."));
            }

            EventLogConfiguration config = new(channel);
            var result = new
            {
                    ChannelName = config.LogName,
                    config.LogType,
                    config.IsEnabled,
                    config.MaximumSizeInBytes,
                    config.LogFilePath,
                    config.IsClassicLog
            };

            return Task.FromResult(ToolResult.SuccessResult(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true })));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.FailureResult($"Event log configuration read failed: {ex.Message}"));
        }
    }
}