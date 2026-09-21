


using SentinelCore.Abstractions;
using SentinelCore.Orchestrations.Agents.Models;




namespace SentinelCore.Orchestrations.agents;





public static class ChatMessageExtensions
{



    /// <summary>
    /// Adds a new assistant message to the specified <see cref="ChatMessage"/> instance.
    /// </summary>
    /// <param name="message">
    /// The existing <see cref="ChatMessage"/> instance to which the assistant message will be added.
    /// </param>
    /// <param name="content">
    /// The content of the assistant message to be added. Must not be null or empty.
    /// </param>
    /// <returns>
    /// A new <see cref="ChatMessage"/> instance representing the assistant message with the specified content.
    /// </returns>
    /// <exception cref="System.ArgumentException">
    /// Thrown if <paramref name="content"/> is null or empty.
    /// </exception>
    public static ChatMessage AddAssistantMessage(this ChatMessage message, string content)
    {

        Throw.IfNullOrEmpty(content);
        // Message tagged for later tracking, future spoofing safety valve potential
        return new ChatMessage(ChatRole.Assistant, content).WithAgentRequestMessageSource(new AgentRequestMessageSourceType("Origin"),"SystemGenerated");
    }



}





