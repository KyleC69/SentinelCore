// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         VerifyEvidenceExecutor.cs
// Author: Kyle L. Crowder
// Build Num:  081602



namespace SentinelCore.Workflows.Executors;





public class VerifyEvidenceExecutor : Executor
{

    public VerifyEvidenceExecutor() : base("VerifyEvidenceExecutor")
    {



    }








    /// <summary>
    ///     Configures the protocol by setting up routes and declaring the message types used for sending and yielding
    ///     output.
    /// </summary>
    /// <remarks>
    ///     This method serves as the primary entry point for protocol configuration. It integrates route
    ///     setup and message type declarations. For backward compatibility, it is currently invoked from the
    ///     RouteBuilder.
    /// </remarks>
    /// <returns>
    ///     An instance of <see cref="T:Microsoft.Agents.AI.Workflows.ExecutorProtocol" /> that represents the fully
    ///     configured protocol.
    /// </returns>
    protected override ProtocolBuilder ConfigureProtocol(ProtocolBuilder protocolBuilder)
    {
        throw new NotImplementedException();
    }








    public ValueTask<string> HandleAsync(string message, IWorkflowContext context, CancellationToken ct = default)
    {
        string newmessage = string.Empty;
        newmessage = message + ":: SafetyChecked";

        // You could also use context.SendMessageAsync(length) and return ValueTask.CompletedTask;
        // Returning the value is more concise for this case.
        return ValueTask.FromResult(newmessage);
    }
}