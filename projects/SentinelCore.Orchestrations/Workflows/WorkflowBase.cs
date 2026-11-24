// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         WorkflowBase.cs
// Author: Kyle L. Crowder
// Build Num:  112101



using System.Diagnostics;
using System.Text;

using Microsoft.Agents.AI.Workflows.Specialized.Magentic;
using Microsoft.Extensions.Logging;

using SentinelCore.Contracts.Abstractions;
using SentinelCore.Orchestrations.Services;




namespace SentinelCore.Orchestrations.Workflows;





/// <summary>
///     Base class for all workflow orchestrations. Provides common utilities,
///     state management, and event processing capabilities.
/// </summary>
public abstract class WorkflowBase
{

    protected IWorkflowEventProcessor _eventProcessor;
    protected ILogger<WorkflowBase> _logger;
    protected ILoggerFactory _loggerFactory;
    protected ISystemReporter _reporter;

    /// <summary>Upper bound for creating every agent the workflow needs.</summary>
    protected static readonly TimeSpan AgentInitializationTimeout = TimeSpan.FromMinutes(5);

    /// <summary>Upper bound for the classifier agent call that produces the investigation objective.</summary>
    protected static readonly TimeSpan ClassificationTimeout = TimeSpan.FromMinutes(10);

    /// <summary>Upper bound for the whole streaming run so a stalled provider or executor cannot block termination.</summary>
    protected static readonly TimeSpan ExecutionTimeout = TimeSpan.FromMinutes(45);

    /// <summary>Upper bound for disposing the run once streaming has ended.</summary>
    protected static readonly TimeSpan RunShutdownTimeout = TimeSpan.FromSeconds(30);

    /// <summary>Upper bound for starting the run and scheduling the turn token.</summary>
    protected static readonly TimeSpan RunStartupTimeout = TimeSpan.FromMinutes(2);








    protected WorkflowBase(ISystemReporter reporter, ILoggerFactory logger)
    {
        _reporter = reporter;
        _loggerFactory = logger;
        _logger = logger.CreateLogger<WorkflowBase>();
    }








    protected static string FormatProgressLedger(MagenticProgressLedger ledger)
    {
        return string.Join(Environment.NewLine, $"Request satisfied: {ledger.IsRequestSatisfied}", $"In loop: {ledger.IsInLoop}", $"Making progress: {ledger.IsProgressBeingMade}", $"Next speaker: {ledger.NextSpeaker}", $"Instruction: {ledger.InstructionOrQuestion}");
    }








    /// <summary>
    ///     Gets the file path for workflow event logging in the LocalApplicationData\SentinelCore folder.
    /// </summary>
    /// <returns>The full path to the workflow event log file.</returns>
    private string GetWorkflowEventLogFilePath()
    {
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string logFileName = $"WorkflowEvents_{timestamp}.log";
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string sentinelCoreFolder = Path.Combine(localAppData, "SentinelCore");

        // Ensure the directory exists
        Directory.CreateDirectory(sentinelCoreFolder);

        return Path.Combine(sentinelCoreFolder, logFileName);
    }








    /// <summary>
    ///     Writes the contents of an agent message (text, tool traffic, usage, and non-text content) to the console.
    /// </summary>
    /// <param name="agent">The name of the agent that produced the contents.</param>
    /// <param name="contents">The contents to write, if any.</param>
    private static void LogAIContents(string agent, IEnumerable<AIContent>? contents)
    {
        if (contents is null)
        {
            return;
        }

        foreach (AIContent content in contents)
        {
            switch (content)
            {
                case TextContent text:
                {
                    if (!string.IsNullOrWhiteSpace(text.Text))
                    {
                        Console.WriteLine($"[{agent}]");
                        Console.WriteLine(text.Text);
                        Console.WriteLine("");
                    }

                    break;
                }

                case FunctionCallContent functionCall:
                {
                    Console.WriteLine($"[{agent}] [tool-call] " + $"{functionCall.Name}({functionCall.Arguments})");

                    Console.WriteLine("");
                    break;
                }

                case FunctionResultContent functionResult:
                {
                    Console.WriteLine($"[{agent}] [tool-result] " + $"{functionResult.CallId} -> {functionResult.Result}");

                    Console.WriteLine("");
                    break;
                }

                case UsageContent usage:
                {
                    Console.WriteLine($"[{agent}] [usage] " + $"Input={usage.Details?.InputTokenCount ?? 0}, " + $"Output={usage.Details?.OutputTokenCount ?? 0}");

                    break;
                }

                case DataContent data:
                {
                    Console.WriteLine($"[{agent}] [data] {data.MediaType}");

                    break;
                }

                case UriContent uri:
                {
                    Console.WriteLine($"[{agent}] [uri] {uri.Uri}");

                    break;
                }

                default:
                {
                    Console.WriteLine($"[{agent}] [content:{content.GetType().Name}]");

                    Console.WriteLine(content.ToString());

                    break;
                }
            }
        }
    }








    /// <summary>
    ///     Logs a workflow event to the isolated workflow event log file.
    /// </summary>
    /// <param name="workflowEvent">The workflow event to log.</param>
    /// <param name="token">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="Task" /> representing the asynchronous operation.</returns>
    private async Task LogWorkflowEventToFileAsync(WorkflowEvent workflowEvent, CancellationToken token)
    {
        try
        {
            string logFilePath = GetWorkflowEventLogFilePath();
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string eventType = workflowEvent.GetType().Name;

            string logEntry = $"[{timestamp}] {eventType}";

            // Add event-specific details to the log entry
            switch (workflowEvent)
            {
                case AgentResponseUpdateEvent updateEvent:
                    logEntry += $" - Executor: {updateEvent.ExecutorId}, ResponseId: {updateEvent.Update.ResponseId ?? "null"}, MessageId: {updateEvent.Update.MessageId ?? "null"}";
                    if (updateEvent.Update.Contents.Count > 0)
                    {
                        logEntry += $", Contents Count: {updateEvent.Update.Contents.Count}";
                        logEntry += $", Text: \"{updateEvent.Update.Text}\"";
                    }

                    break;

                case ExecutorCompletedEvent completedEvent:
                    logEntry += $" - Executor: {completedEvent.ExecutorId}";
                    break;

                case MagenticPlanCreatedEvent planCreated:
                    logEntry += $" - Plan Length: {planCreated.FullTaskLedger.Text?.Length ?? 0} chars";
                    break;

                case MagenticReplannedEvent replanned:
                    logEntry += $" - Replan Length: {replanned.FullTaskLedger.Text?.Length ?? 0} chars";
                    break;

                case MagenticProgressLedgerUpdatedEvent progressUpdated:
                    MagenticProgressLedger ledger = progressUpdated.ProgressLedger;
                    logEntry += $" - Satisfied: {ledger.IsRequestSatisfied}, InLoop: {ledger.IsInLoop}, Progressing: {ledger.IsProgressBeingMade}";
                    logEntry += $", NextSpeaker: {ledger.NextSpeaker ?? "null"}";
                    break;

                case WorkflowOutputEvent outputEvent:
                    logEntry += $" - Data Type: {outputEvent.Data?.GetType().FullName ?? "null"}";
                    if (outputEvent.Data != null)
                    {
                        string dataStr = outputEvent.Data.ToString();

                        logEntry += $", Data: {dataStr}";
                    }

                    break;

                case WorkflowErrorEvent workflowError:
                    logEntry += $" - Exception: {workflowError.Exception?.Message ?? "Unknown error"}";
                    if (workflowError.Exception != null)
                    {
                        logEntry += $" - Type: {workflowError.Exception.GetType().FullName}";
                    }

                    break;

                case ExecutorFailedEvent executorFailed:
                    logEntry += $" - Executor: {executorFailed.ExecutorId}";
                    logEntry += $" - Error: {executorFailed.Data?.ToString() ?? "unknown error"}";
                    break;

                case ExecutorInvokedEvent executorInvokedEvent:
                    logEntry += $" - Executor: {executorInvokedEvent.ExecutorId}";
                    break;

                default:
                    logEntry += " - No additional details";
                    break;
            }

            logEntry += Environment.NewLine;

            await SaveLinesToFileAsync(logFilePath, logEntry, token);
        }
        catch (Exception ex)
        {
            // Don't let logging failures break the workflow
            Console.WriteLine($"Failed to log workflow event to file: {ex.Message}");
        }
    }








    /// <summary>
    ///     Writes a visual separator to the console between logical sections of harness output.
    /// </summary>
    private static void PrintLogSpace()
    {
        Console.WriteLine(Environment.NewLine);
        Console.WriteLine(new string('-', 60));
    }








    /// <summary>
    ///     To process non streaming run events
    /// </summary>
    /// <param name="run"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    private async Task ProcessResults(Run run, CancellationToken token)
    {
        const bool ShowWorkflowEvents = false;
        const bool ShowToolCalls = true;
        const bool ShowToolResults = true;
        const bool ShowAgentMessages = false;
        const bool ShowUnknownEvents = true;

        Dictionary<string, (string Agent, StringBuilder Text)> responseBuffers = new(StringComparer.Ordinal);



        static string GetSpeaker(ChatMessage message)
        {
            return message.AuthorName ?? message.Role.Value ?? "Unknown";
        }



        static string GetAgentName(string? agentId)
        {
            return string.IsNullOrWhiteSpace(agentId) ? "Agent" : agentId;
        }



        static string ExtractTextFromContents(IEnumerable<AIContent>? contents)
        {
            if (contents is null)
            {
                return string.Empty;
            }

            StringBuilder builder = new();

            foreach (AIContent content in contents)
            {
                switch (content)
                {
                    case TextContent textContent:
                        builder.Append(textContent.Text);
                        break;

                }
            }

            return builder.ToString();
        }



        void AppendUpdateText(string responseId, string agent, string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            if (!responseBuffers.TryGetValue(responseId, out (string Agent, StringBuilder Text) entry))
            {
                entry = (agent, new());
                responseBuffers[responseId] = entry;
            }

            entry.Text.Append(text);
        }



        Console.WriteLine("");
        Console.WriteLine("============================================================");
        Console.WriteLine("WORKFLOW TRACE");
        Console.WriteLine("============================================================");
        Console.WriteLine("");

        foreach (WorkflowEvent evt in run.OutgoingEvents)
        {
            await LogWorkflowEventToFileAsync(evt, token);

            if (ShowWorkflowEvents)
            {
                Console.WriteLine($"[EVENT] {evt.GetType().Name} -> {evt.Data?.GetType().FullName ?? "<null>"}");
            }

            switch (evt.Data)
            {
                //------------------------------------------------------------------
                // COLLECTION OF CHAT MESSAGES
                //------------------------------------------------------------------
                case ICollection<ChatMessage> messages:
                {
                    foreach (ChatMessage message in messages)
                    {
                        string speaker = GetSpeaker(message);

                        string text = !string.IsNullOrWhiteSpace(message.Text) ? message.Text : ExtractTextFromContents(message.Contents);

                        if (ShowAgentMessages && !string.IsNullOrWhiteSpace(text))
                        {
                            Console.WriteLine($"[{speaker}]");
                            Console.WriteLine(text);
                            Console.WriteLine("");
                        }

                        if (ShowToolCalls || ShowToolResults)
                        {
                            LogAIContents(speaker, message.Contents);
                        }
                    }

                    break;
                }

                //------------------------------------------------------------------
                // SINGLE CHAT MESSAGE
                //------------------------------------------------------------------
                case ChatMessage message:
                {
                    string speaker = GetSpeaker(message);

                    string text = !string.IsNullOrWhiteSpace(message.Text) ? message.Text : ExtractTextFromContents(message.Contents);

                    if (ShowAgentMessages && !string.IsNullOrWhiteSpace(text))
                    {
                        Console.WriteLine($"[{speaker}]");
                        Console.WriteLine(text);
                        Console.WriteLine("");
                    }

                    if (ShowToolCalls || ShowToolResults)
                    {
                        LogAIContents(speaker, message.Contents);
                    }

                    break;
                }

                //------------------------------------------------------------------
                // RESPONSE UPDATES
                //------------------------------------------------------------------
                case AgentResponseUpdate update:
                {
                    string responseId = update.ResponseId ?? update.MessageId ?? Guid.NewGuid().ToString();

                    string agent = GetAgentName(update.AgentId);

                    string updateText = !string.IsNullOrWhiteSpace(update.Text) ? update.Text : ExtractTextFromContents(update.Contents);

                    AppendUpdateText(responseId, agent, updateText);

                    if (ShowToolCalls || ShowToolResults)
                    {
                        LogAIContents(agent, update.Contents);
                    }

                    break;
                }

                //------------------------------------------------------------------
                // FINAL AGENT RESPONSE
                //------------------------------------------------------------------
                case AgentResponse response:
                {
                    string agent = GetAgentName(response.AgentId);

                    string text = !string.IsNullOrWhiteSpace(response.Text) ? response.Text : string.Join(Environment.NewLine, response.Messages?.SelectMany(m => m.Contents).OfType<TextContent>().Select(t => t.Text) ?? Enumerable.Empty<string>());

                    if (ShowAgentMessages && !string.IsNullOrWhiteSpace(text))
                    {
                        Console.WriteLine("");
                        Console.WriteLine("============================================================");
                        Console.WriteLine($"FINAL RESPONSE : {agent}");
                        Console.WriteLine("============================================================");
                        Console.WriteLine(text);
                        Console.WriteLine("");
                    }

                    if (response.Messages is not null)
                    {
                        foreach (ChatMessage msg in response.Messages)
                        {
                            LogAIContents(GetSpeaker(msg), msg.Contents);
                        }
                    }

                    break;
                }

                //------------------------------------------------------------------
                // UNKNOWN
                //------------------------------------------------------------------
                default:
                {
                    if (ShowUnknownEvents)
                    {
                        Console.WriteLine($"[UNHANDLED] {evt.Data?.GetType().FullName ?? "<null>"}");
                    }

                    break;
                }
            }
        }

        if (responseBuffers.Count > 0)
        {
            Console.WriteLine("");
            Console.WriteLine("============================================================");
            Console.WriteLine("REHYDRATED AGENT RESPONSES");
            Console.WriteLine("============================================================");
            Console.WriteLine("");

            foreach ((string Agent, StringBuilder Text) response in responseBuffers.Values)
            {
                if (response.Text.Length == 0)
                {
                    continue;
                }

                Console.WriteLine($"[{response.Agent}]");
                Console.WriteLine(response.Text.ToString());
                Console.WriteLine("");
            }
        }

        Console.WriteLine("");
        Console.WriteLine("============================================================");
        Console.WriteLine("END OF RUN");
        Console.WriteLine("============================================================");
    }








    /// <summary>
    ///     Reports an execution failure to the console and to the system reporter without terminating the harness.
    /// </summary>
    /// <param name="message">A description of the failure.</param>
    /// <param name="exception">The exception that caused the failure, if any.</param>
    protected void ReportExecutionFailure(string message, Exception? exception = null)
    {
        try
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(exception is null ? message : $"{message}{Environment.NewLine}{exception}");
        }
        finally
        {
            Console.ResetColor();
        }

        _reporter.ReportError(message, exception);
    }








    /// <summary>
    ///     Appends the specified lines to a file asynchronously.
    /// </summary>
    /// <param name="filePath">The path to the file where lines should be saved.</param>
    /// <param name="lines">The text to append to the file.</param>
    /// <param name="token">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="Task" /> representing the asynchronous operation.</returns>
    public async Task SaveLinesToFileAsync(string filePath, string lines, CancellationToken token)
    {
        try
        {
            await File.AppendAllTextAsync(filePath, lines, token).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to save lines to file {filePath}: {e.Message}");
            throw;
        }
    }








    /// <summary>
    ///     Writes the final conversation transcript and report to the Debug window.
    /// </summary>
    /// <remarks>
    ///     This is the only harness output that belongs in the Debug window; every other output is console output.
    ///     <see cref="Trace" /> is used instead of <see cref="Debug" /> because <see cref="Debug" /> calls are compiled out of
    ///     Release builds, which silently suppressed the transcript, and it is flushed immediately so the transcript is
    ///     visible before the harness process exits.
    /// </remarks>
    /// <param name="transcript">The final conversation transcript, if the workflow produced one.</param>
    /// <param name="reportText">The final report text, if the workflow produced one.</param>
    protected static void WriteFinalTranscriptToDebugWindow(IReadOnlyList<ChatMessage>? transcript, string? reportText)
    {
        StringBuilder output = new();
        output.AppendLine();
        output.AppendLine(new('=', 80));
        output.AppendLine("Final Conversation Transcript:");
        output.AppendLine(new('=', 80));
        output.AppendLine();

        if (transcript is { Count: > 0 })
        {
            foreach (ChatMessage message in transcript)
            {
                output.AppendLine($"{message.AuthorName ?? message.Role.ToString()}: {message.Text}");
                output.AppendLine();
            }
        }
        else
        {
            output.AppendLine("(the workflow produced no conversation transcript)");
            output.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(reportText))
        {
            output.AppendLine(new('=', 80));
            output.AppendLine("Final Report:");
            output.AppendLine(new('=', 80));
            output.AppendLine();
            output.AppendLine(reportText);
            output.AppendLine();
        }

        Trace.WriteLine(output.ToString());
        Trace.Flush();
    }








    /// <summary>
    ///     Writes a Magentic orchestration message (initial plan, replan, or progress ledger) to the console.
    /// </summary>
    /// <param name="title">The Magentic message category.</param>
    /// <param name="content">The message content, if any.</param>
    protected static void WriteMagenticMessage(string title, string? content)
    {
        Console.WriteLine();
        Console.WriteLine($"[Magentic {title}]");
        Console.WriteLine(content);
    }








    /// <summary>
    ///     Writes a streamed agent update to the console and accumulates its text.
    /// </summary>
    /// <remarks>
    ///     The accumulated text is the fallback for runs that never emit a final output event, so the Debug window still
    ///     receives the last agent answer instead of nothing.
    /// </remarks>
    /// <param name="updateEvent">The streamed update event.</param>
    /// <param name="lastResponseId">The response identifier of the previously written update.</param>
    /// <param name="streamedAnswer">Accumulates the text of the response currently being streamed.</param>
    protected static void WriteStreamingUpdate(AgentResponseUpdateEvent updateEvent, ref string? lastResponseId, StringBuilder streamedAnswer)
    {
        string responseId = updateEvent.Update.ResponseId ?? updateEvent.Update.MessageId ?? updateEvent.ExecutorId;
        if (!string.Equals(responseId, lastResponseId, StringComparison.Ordinal))
        {
            if (lastResponseId is not null)
            {
                Console.WriteLine();
                Console.WriteLine();
            }

            Console.Write($"- Stream Update {updateEvent.ExecutorId}: ");
            lastResponseId = responseId;
            streamedAnswer.Clear();
        }

        if (!string.IsNullOrEmpty(updateEvent.Update.Text))
        {
            streamedAnswer.Append(updateEvent.Update.Text);
            Console.Write(updateEvent.Update.Text);
        }
    }
}