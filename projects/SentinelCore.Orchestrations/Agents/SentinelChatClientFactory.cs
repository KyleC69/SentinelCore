// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         SentinelChatClientFactory.cs
// Author: Kyle L. Crowder
// Build Num:  081312



using System.Text.Json;

using Azure;
using Azure.AI.OpenAI;

using OllamaSharp;

using OpenAI;
using OpenAI.Chat;




namespace SentinelCore.Agents;





/// <summary>
///     Factory for creating <see cref="IChatClient" /> instances from a <see cref="ModelProfile" />.
///     Library consumers can use this or provide their own <see cref="IChatClient" /> implementation.
/// </summary>
public static class SentinelChatClientFactory
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true, PropertyNameCaseInsensitive = true };








    private static IChatClient CreateAnthropicClient(ModelProfile model)
    {
        throw new NotSupportedException("Anthropic provider not yet implemented. Use OpenAI-compatible endpoint or add Microsoft.Agents.AI.Anthropic package.");
    }








    private static IChatClient CreateAzureOpenAIClient(ModelProfile model)
    {
        AzureOpenAIClient azureClient = new(new Uri(model.Endpoint ?? throw new ArgumentException("Azure OpenAI endpoint required")), new AzureKeyCredential(model.ApiKey ?? throw new ArgumentException("Azure OpenAI API key required")));
        ChatClient? chatClient = azureClient.GetChatClient(model.ModelId ?? throw new ArgumentException("Azure OpenAI model ID required"));
        return chatClient.AsIChatClient();
    }








    /// <summary>
    ///     Creates an <see cref="IChatClient" /> based on the model profile.
    ///     Returns the raw provider client without SentinelCore middleware wrappers.
    /// </summary>
    /// <param name="model">The model profile containing provider, endpoint, and credentials.</param>
    /// <returns>A configured <see cref="IChatClient" /> for the specified provider.</returns>
    public static IChatClient CreateChatClient(ModelProfile model)
    {
        IChatClient baseClient = model.Provider switch
        {
                ModelProfile.ModelProvider.Ollama => CreateOllamaClient(model),
                ModelProfile.ModelProvider.OpenAI => CreateOpenAIClient(model),
                ModelProfile.ModelProvider.AzureOpenAI => CreateAzureOpenAIClient(model),
                ModelProfile.ModelProvider.GitHubModels => CreateGitHubModelsClient(model),
                ModelProfile.ModelProvider.Anthropic => CreateAnthropicClient(model),
                ModelProfile.ModelProvider.ONNX => CreateOnnxClient(model),
                _ => throw new NotSupportedException($"Provider {model.Provider} not supported")
        };

        return baseClient;
    }








    private static IChatClient CreateGitHubModelsClient(ModelProfile model)
    {
        OpenAIClient client = new(new System.ClientModel.ApiKeyCredential(model.ApiKey ?? throw new ArgumentException("GitHub token required")), new OpenAIClientOptions { Endpoint = new Uri(model.Endpoint ?? "https://models.inference.ai.azure.com") });
        ChatClient? chatClient = client.GetChatClient(model.ModelId ?? throw new ArgumentException("GitHub model ID required"));
        return chatClient.AsIChatClient();
    }








    private static IChatClient CreateOllamaClient(ModelProfile model)
    {
        OllamaApiClient client = new(new Uri(model.Endpoint ?? "http://127.0.0.1:11434"), model.ModelId ?? "gemma4");
        client.SelectedModel = model.ModelId ?? "gemma4";





        return client;
    }








    private static IChatClient CreateOnnxClient(ModelProfile model)
    {
        throw new NotSupportedException("ONNX provider requires Microsoft.Agents.AI.Onnx package. Add the package and implement this method.");
    }








    private static IChatClient CreateOpenAIClient(ModelProfile model)
    {
        OpenAIClient client = new(model.ApiKey ?? throw new ArgumentException("OpenAI API key required"));
        ChatClient? chatClient = client.GetChatClient(model.ModelId ?? throw new ArgumentException("OpenAI model ID required"));
        return chatClient.AsIChatClient();
    }
}