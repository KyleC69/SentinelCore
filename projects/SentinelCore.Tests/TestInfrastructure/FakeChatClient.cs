// Solution: SentinelCore
// Project:   SentinelCore.Tests
// File:         FakeChatClient.cs
// Author: Kyle L. Crowder
// Build Num:  092308



using System.Runtime.CompilerServices;




namespace SentinelCore.Tests.TestInfrastructure;





/// <summary>
///     A test double for <see cref="IChatClient" /> that returns a pre-configured
///     <see cref="ChatResponse" /> without making any network calls.
/// </summary>
public sealed class FakeChatClient : IChatClient
{



    public FakeChatClient(ChatResponse response)
    {
        GetResponseResult = response;
    }








    public ChatResponse GetResponseResult { get; }








    public void Dispose()
    {
    }








    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options, CancellationToken cancellationToken)
    {
        return Task.FromResult(GetResponseResult);
    }








    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        return null;
    }








    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // FakeChatClient is used for non-streaming tests; return the full
        // response as a single update. If a streaming test is needed, a
        // dedicated fake should be created.
        ChatResponseUpdate update = new(GetResponseResult.Messages[0].Role, GetResponseResult.Messages[0].Text);
        yield return update;
        await Task.CompletedTask;
    }








    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}