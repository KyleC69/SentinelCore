using SentinelCore.Abstractions;




namespace SentinelCore.Orchestrations.Agents.Middleware;





public class ExceptionMiddleware() : Executor<ChatMessage, ChatMessage>("ExceptionMiddleware")
{
    public override ValueTask<ChatMessage> HandleAsync(ChatMessage message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        Throw.IfNull(message);
        Throw.IfNull(context);



        throw new NotImplementedException();
    }

    /*


        public async Task<ChatMessage> ExceptionSyncHandlingAsync(IEnumerable<ChatMessage> messages, AgentSession? session, AgentRunOptions? options, AIAgent innerAgent, CancellationToken cancellationToken)
        {
            try
            {
                reporter.ReportInfo("[ExceptionHandler] Executing agent run...");

                // #######################################  Handler logic starts here #######################################


                // Handler logic goes here.


                // ###############################################
                ExecuteCoreAsync(messages, session, options, innerAgent, cancellationToken);
            }
            catch (Exception ex)
            {
                reporter.ReportError(ex, $"[Error] Runtime exception caught: {ex.Message}");
                if (ex.InnerException is not null)
                {
                    reporter.ReportError(ex, $"[Error] Inner exception: {ex.InnerException.Message}");
                }

                throw new SentinelCoreExecutionException($"[Error] Runtime exception caught in step {}");

            }
        }*/

}














