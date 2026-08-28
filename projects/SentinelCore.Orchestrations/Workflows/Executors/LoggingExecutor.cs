// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         LoggingExecutor.cs
// Author: Kyle L. Crowder
// Build Num:  082808



using Microsoft.Extensions.Logging;




namespace SentinelCore.Workflows.Executors;





public sealed class LoggingExecutor(ILoggerFactory loggerFactory) : Executor<ChatMessage, ChatMessage>("LoggingExecutor")
{

    public override async ValueTask<ChatMessage> HandleAsync(ChatMessage message, IWorkflowContext context, CancellationToken token)
    {
        ILogger logger = loggerFactory.CreateLogger(this.GetType());
        logger.LogInformation("Handling message: {MessageContent}", message.Text);
        await context.SendMessageAsync(message);
        return message;

    }
}