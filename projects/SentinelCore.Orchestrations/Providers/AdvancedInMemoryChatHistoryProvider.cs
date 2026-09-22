

using System.Text.Json.Serialization;
using SentinelCore.Orchestrations.Agents.Models;

namespace SentinelCore.Orchestrations.Providers;



public sealed class AdvancedInMemoryChatHistoryProvider : ChatHistoryProvider
{
    private readonly ProviderSessionState<State> _sessionState;








    public AdvancedInMemoryChatHistoryProvider(Func<AgentSession?, State>? stateInitializer = null, string? stateKey = null)
    {
        this._sessionState = new ProviderSessionState<State>(stateInitializer ?? (_ => new State()), stateKey ?? this.GetType().Name);
    }








    public string StateKey => this._sessionState.StateKey;








    protected override ValueTask<IEnumerable<ChatMessage>> InvokingCoreAsync(InvokingContext context, CancellationToken cancellationToken = default)
    {
        // Retrieve the chat history from the session state.
        var chatHistory = this._sessionState.GetOrInitializeState(context.Session).Messages;

        // Stamp the messages with this class as the source, so that they can be filtered out later if needed when storing the agent input/output.
        var stampedChatHistory = chatHistory.Select(message => message.WithAgentRequestMessageSource(AgentRequestMessageSourceType.ChatHistory, this.GetType().FullName!));

        // Merge the original input with the chat history to produce a combined agent input.
        return new(stampedChatHistory.Concat(context.RequestMessages));
    }








    protected override ValueTask InvokedCoreAsync(InvokedContext context, CancellationToken cancellationToken = default)
    {
        if (context.InvokeException is not null)
        {
            return default;
        }

        // Since we are receiving all messages that were contributed earlier, including those from chat history, we need to filter out the messages that came from chat history
        // so that we don't store message we already have in storage.
        var filteredRequestMessages = context.RequestMessages.Where(m => m.GetAgentRequestMessageSourceType() != AgentRequestMessageSourceType.ChatHistory);

        var state = this._sessionState.GetOrInitializeState(context.Session);

        // Add both request and response messages to the state.
        var allNewMessages = filteredRequestMessages.Concat(context.ResponseMessages ?? []);
        state.Messages.AddRange(allNewMessages);

        this._sessionState.SaveState(context.Session, state);

        return default;
    }








    public sealed class State
    {
        [JsonPropertyName("messages")] public ChatMessages Messages { get; set; } = [];
    }
}
