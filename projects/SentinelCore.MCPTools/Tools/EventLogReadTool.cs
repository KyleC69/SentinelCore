// Solution: Sentinel-MCP-Projects
// Project:   SentinelCore-mcp
// File:         EventLogReadTool.cs
// Author: Kyle L. Crowder
// Build Num:  110104



using System.ComponentModel;
using System.Diagnostics.Eventing.Reader;
using System.Runtime.Versioning;

using ModelContextProtocol.Server;




namespace SentinelCoreMCP.Tools;





/// <summary>
///     Read-only tool for querying Windows Event Logs.
/// </summary>
[McpServerToolType]
public sealed class EventLogReadTool
{

    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_forensics_list_event_channels", ReadOnly = true, Destructive = false)]
    [Description("Lists available event log channels.")]
    public async Task<ToolResult> EventLogListChannelsAsync([Description("Maximum number of channels to return. Defaults to 50.")] int maxRecords = 50)
    {
        try
        {
            List<string> channels = new();
            int count = 0;
            foreach (string? logName in EventLogSession.GlobalSession.GetLogNames())
            {
                if (count >= maxRecords)
                {
                    break;
                }

                channels.Add(Helpers.CleanValue(logName));
                count++;
            }

            return ToolResult.Ok(channels, "EventLogReadTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "EventLogReadTool");
        }
    }








    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_forensics_query_event_log", ReadOnly = true, Destructive = false)]
    [Description("Queries events from a specific event log channel. Always sorted from newest to oldest, so the most recent events are returned first regardless of the filter. Use Get-WinEvent and the PowershellReadTool")]
    public async Task<ToolResult> EventLogQueryAsync([Description("The event log channel name, e.g. Application or System.")] string channel, [Description("Optional XPath filter expression. Defaults to all events.")] string? query = null, [Description("Maximum number of events to return. Defaults to 50.")] int maxEvents = 50)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(channel))
            {
                return ToolResult.Fail("channel is required.", "EventLogReadTool");
            }

            // Always read newest-to-oldest so the most recent events come back first,
            // with or without a user-supplied XPath filter.
            string xpath = string.IsNullOrWhiteSpace(query) ? "*" : query;
            List<object> results = new();
            int count = 0;
            EventLogQuery eventLogQuery = new(channel, PathType.LogName, xpath) { ReverseDirection = true };
            using EventLogReader reader = new(eventLogQuery);
            reader.BatchSize = 150;
            EventRecord? record;


            while ((record = reader.ReadEvent()) is not null && count < maxEvents)
            {
                results.Add(new
                {
                        TimeCreated = record.TimeCreated?.ToString(),
                        Level = Helpers.CleanValue(record.LevelDisplayName),
                        Provider = Helpers.CleanValue(record.ProviderName),
                        record.Id,
                        Message = Helpers.CleanValue(record.FormatDescription())
                });
                count++;
            }

            return ToolResult.Ok(results, "EventLogReadTool");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "EventLogReadTool");
        }
    }








    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "sc_forensics_read_event_config", ReadOnly = true, Destructive = false)]
    [Description("Reads event log configuration such as retention and file size.")]
    public async Task<ToolResult> EventLogReadConfigurationAsync([Description("The event log channel name.")] string channel)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(channel))
            {
                return ToolResult.Fail("channel is required.", "EventLogReadTool");
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

            return ToolResult.Ok(result, "Event log configuration read.");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail(ex.Message, "EventLogReadTool");
        }
    }
}
