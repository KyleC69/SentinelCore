// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         ModelNoiseSafety.cs
// Author: Kyle L. Crowder
// Build Num:  081312



using System.Runtime.CompilerServices;
using System.Text;

using JetBrains.Annotations;




namespace SentinelCore.Agents;





public class ModelNoiseSafety : DelegatingChatClient

{
    public ModelNoiseSafety(IChatClient innerChatClient) : base(innerChatClient)
    {
    }








    public override async Task<ChatResponse> GetResponseAsync([NotNull] IEnumerable<ChatMessage> messages, ChatOptions? options, CancellationToken cancellationToken)
    {
        try
        {
            ChatResponse response = await base.GetResponseAsync(messages, options, cancellationToken).ConfigureAwait(false);


            ChatResponse clean = SanitizeMessageContents(response);


            return clean;
        }
        catch (HttpRequestException ec) when (ec.Message.Contains("404"))
        {
            //   _logger.LogError("Check for incorrect endpoint or model id in settings.");
            //   _events.RaiseError("Check the model profile settings", null);
        }

        return new ChatResponse(new ChatMessage(ChatRole.System, "Check the model profile settings for correct endpoint and model name"));
    }








    /// <summary>
    ///     Asks the <see cref="T:Microsoft.Extensions.AI.IChatClient" /> for an object of the specified type
    ///     <paramref name="serviceType" />.
    /// </summary>
    /// <param name="serviceType">The type of object being requested.</param>
    /// <param name="serviceKey">An optional key that can be used to help identify the target service.</param>
    /// <returns>The found object, otherwise <see langword="null" />.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="serviceType" /> is <see langword="null" />.</exception>
    /// <remarks>
    ///     The purpose of this method is to allow for the retrieval of strongly typed services that might be provided by the
    ///     <see cref="T:Microsoft.Extensions.AI.IChatClient" />,
    ///     including itself or any services it might be wrapping. For example, to access the
    ///     <see cref="T:Microsoft.Extensions.AI.ChatClientMetadata" /> for the instance,
    ///     <see cref="M:Microsoft.Extensions.AI.IChatClient.GetService(System.Type,System.Object)" /> may be used to request
    ///     it.
    /// </remarks>
    public override object? GetService(Type serviceType, object? serviceKey = null)
    {
        throw new NotImplementedException();
    }








    /// <summary>
    /// </summary>
    /// <param name="messages"></param>
    /// <param name="options"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        StringBuilder textAccumulator = new StringBuilder();

        await foreach (ChatResponseUpdate update in base.GetStreamingResponseAsync(messages, options, cancellationToken).ConfigureAwait(false))
        {
            //  PublishFunctionResults(update.Contents);

            foreach (TextContent text in update.Contents.OfType<TextContent>())
                if (!string.IsNullOrWhiteSpace(text.Text))
                {
                    textAccumulator.Append(text.Text);
                }

            yield return update;
        }

        //  PublishTextOutput(textAccumulator.ToString());


        //   return base.GetStreamingResponseAsync(messages, options, cancellationToken);
    }








    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
    public new void Dispose()
    {
        base.Dispose(true);
    }








    private ChatResponse SanitizeMessageContents(ChatResponse messageContents)
    {
        ChatResponse r = new ChatResponse();
        foreach (ChatMessage msg in messageContents.Messages)
        {
            AIContent ai = new AIContent { Annotations = null, RawRepresentation = null, AdditionalProperties = null };
        }

        return r;
    }
}