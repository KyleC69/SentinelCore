// Solution: SentinelCore
// Project:   SentinelCore.Tests
// File:         FakeChatClient.cs
// Author: Kyle L. Crowder
// Build Num:  082808



using System.Runtime.CompilerServices;




namespace SentinelCore.Tests.TestInfrastructure;





/// <summary>
///     A test double for <see cref="IChatClient" /> that returns a pre-configured
///     <see cref="ChatResponse" /> without making any network calls.
/// </summary>
public sealed class FakeChatClient : IChatClient
{
    private readonly ChatResponse _response;








    public FakeChatClient(ChatResponse response)
    {
        _response = response;
    }








    public ChatResponse GetResponseResult
    {
        get => _response;
    }








    public void Dispose()
    {
    }








    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options, CancellationToken cancellationToken)
    {
        return Task.FromResult(_response);
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
        ChatResponseUpdate update = new(_response.Messages[0].Role, _response.Messages[0].Text);
        yield return update;
        await Task.CompletedTask;
    }








    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}