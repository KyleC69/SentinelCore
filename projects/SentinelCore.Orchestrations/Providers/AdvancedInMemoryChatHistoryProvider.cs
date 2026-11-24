// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         AdvancedInMemoryChatHistoryProvider.cs
// Author: Kyle L. Crowder
// Build Num:  112101



using System.Diagnostics;
using System.Text.Json.Serialization;

using SentinelCore.Orchestrations.Models;




namespace SentinelCore.Orchestrations.Providers;





/// <summary>
///     An advanced in-memory chat history provider that stores chat messages in memory and maintains state across
///     sessions.
///     Agent Framework integration for managing chat history in memory.
/// </summary>
public sealed class AdvancedInMemoryChatHistoryProvider : ChatHistoryProvider
{
    private readonly int _maxHistorySize;
    private readonly ProviderSessionState<State> _sessionState;








    public AdvancedInMemoryChatHistoryProvider(Func<AgentSession?, State>? stateInitializer = null, string? stateKey = null, int maxHistorySize = 100)
    {
        _maxHistorySize = maxHistorySize;

        // Provide defaults if not specified to avoid ArgumentNullExceptions
        var initializer = stateInitializer ?? (session => new());
        string key = stateKey ?? "AdvancedInMemoryChatHistory";

        _sessionState = new(initializer, key);
    }








    public string StateKey
    {
        get => _sessionState.StateKey;
    }








    protected override async ValueTask InvokedCoreAsync(InvokedContext context, CancellationToken cancellationToken = default)
    {
        if (context.InvokeException != null)
        {
            // In a production environment, this should use IErrorReporter
            Debug.WriteLine($"Error during invocation: {context.InvokeException}");
            return;
        }

        IEnumerable<ChatMessage> filteredRequestMessages = context.RequestMessages.Where(m => m.GetMessageSourceType() != MessageSourceType.ChatHistory);

        State state = _sessionState.GetOrInitializeState(context.Session);

        lock (state.SyncRoot)
        {
            // Add both request and response messages to the state.
            IEnumerable<ChatMessage> allNewMessages = filteredRequestMessages.Concat(context.ResponseMessages ?? Enumerable.Empty<ChatMessage>());
            state.Messages.AddRange(allNewMessages);

            // Implement sliding window to prevent unbounded growth
            while (state.Messages.Count > _maxHistorySize)
                try
                {
                    // Pruning is intentionally not implemented yet — placeholder loop, tracked separately.
                    break;
                }
                catch
                {
                    break;
                }
        }

        _sessionState.SaveState(context.Session, state);
    }








    protected override async ValueTask<IEnumerable<ChatMessage>> InvokingCoreAsync(InvokingContext context, CancellationToken cancellationToken = default)
    {
        State state = _sessionState.GetOrInitializeState(context.Session);

        List<ChatMessage> historySnapshot;
        lock (state.SyncRoot)
        {
            historySnapshot = state.Messages.ToList();
        }

        IEnumerable<ChatMessage> stampedChatHistory = historySnapshot.Select(message => message.WithMessageSource(MessageSourceType.ChatHistory, this.GetType().FullName!));

        return stampedChatHistory.Concat(context.RequestMessages);
    }








    public sealed class State
    {
        public readonly object SyncRoot = new();

        [JsonPropertyName("messages")] public List<ChatMessage> Messages { get; set; } = [];
    }
}