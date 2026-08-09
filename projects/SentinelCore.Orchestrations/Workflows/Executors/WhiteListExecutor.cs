// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         WhiteListExecutor.cs
// Author: Kyle L. Crowder
// Build Num:  080801



using SentinelCore.Abstractions;




namespace SentinelCore.Workflows.Executors;





/// <summary>
///     This will check the signal against the operators whitelist. This will probably be stored in DB.
///     A list of environmentally acceptable signals that should be ignored.
///     ** NOTE: This must be a bullet proof design and never leave any room for a poor identification. If the signal is
///     not on the whitelist
///     It must flow through normal pathways, If it is on the list It will be logged and the flow terminated.
/// </summary>
public sealed partial class WhiteListExecutor : Executor
{
    private readonly ISystemReporter _reporter;








    public WhiteListExecutor(ISystemReporter reporter) : base("Whitelist")
    {
        _reporter = reporter;
    }








    /// <summary>
    ///     ///     This will check the signal against the operators whitelist.
    ///     Currently this executor is being used for testing the viability of prompt override commands. A type of back door to
    ///     other pathways.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="context"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    [MessageHandler]
    public async ValueTask<SuppressionDecision> HandleAsync(ChatMessage message, IWorkflowContext context, CancellationToken ct = default)
    {
        _reporter.ReportInfo("Starting whitelist executor...");

        SuppressionDecision results = new();
        if (message.Text.StartsWith("CASEGEN:", StringComparison.CurrentCulture))
        {
            _reporter.ReportInfo("Detected CASEGEN command. Bypassing whitelist check.");
            results.Command = CommandValue.CASEGEN;
            results.Prompt = message.Text.Substring(8); // Extract the prompt after "CASEGEN:"
        }
        else
        {
            results.Command = CommandValue.OTHER;
            results.Prompt = message.Text;
        }

        await context.SendMessageAsync(results); //send the results to the next executor in the workflow
        return results;

    }
}





public class SuppressionDecision
{
    public CommandValue Command { get; set; }
    // Initialise to avoid CS8618.
    public string Prompt { get; set; } = string.Empty;
    public bool Suppress { get; set; }
}





public enum CommandValue
{
    CASEGEN, OTHER
}
