// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         SentinelChatClientFactory.cs
// Author: Kyle L. Crowder
// Build Num:  112518



using System.ClientModel;
using System.Collections.Concurrent;

using Anthropic.SDK;

using Azure;
using Azure.AI.Inference;
using Azure.AI.OpenAI;

using Microsoft.Extensions.Logging;

using Microsoft.ML.OnnxRuntimeGenAI;

using OllamaSharp;

using OpenAI;
using OpenAI.Chat;

using SentinelCore.Contracts.Contracts;
using SentinelCore.Orchestrations.Abstractions;




namespace SentinelCore.Orchestrations.Agents;





/// <summary>
///     Default implementation of <see cref="IChatClientFactory" /> that creates
///     <see cref="IChatClient" /> instances from a <see cref="ModelProfile" />.
///     Library consumers can use this or provide their own <see cref="IChatClientFactory" /> implementation.
/// </summary>
public sealed class SentinelChatClientFactory : IChatClientFactory
{
    // HttpClient properties (BaseAddress, Timeout, ...) may only be set before the first request;
    // changing them afterwards throws "This instance has already started one or more requests".
    // One client is therefore created per endpoint with its final BaseAddress and reused from
    // then on, so every agent's requests stay on their own model's endpoint.
    private readonly ConcurrentDictionary<string, HttpClient> _httpClientsByEndpoint = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILoggerFactory _loggerFactory;








    public SentinelChatClientFactory(ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _loggerFactory = loggerFactory;
    }








    public IChatClient CreateChatClient(ModelProfile model)
    {
        IChatClient baseClient = model.Provider switch
        {
            ModelProfile.ModelProvider.Ollama => CreateOllamaClient(model),
            ModelProfile.ModelProvider.OpenAI => CreateOpenAIClient(model),
            ModelProfile.ModelProvider.AzureOpenAI => CreateAzureOpenAIClient(model),
            ModelProfile.ModelProvider.GitHubModels => CreateGitHubModelsClient(model),
            ModelProfile.ModelProvider.Anthropic => CreateAnthropicClient(model),
            ModelProfile.ModelProvider.OnnxRuntime => CreateOnnxClient(model),
            ModelProfile.ModelProvider.Foundry => CreateFoundryClient(model),
            ModelProfile.ModelProvider.Azure => CreateAzureClient(model),
            _ => throw new NotSupportedException($"Provider {model.Provider} not supported")
        };

        return baseClient;
    }








    private static IChatClient CreateAzureClient(ModelProfile model)
    {
        // Azure AI Foundry Model Inference API (https://<resource>.services.ai.azure.com/models).
        // The model id is passed as the default for ChatCompletionsOptions.Model; per-request
        // ChatOptions.ModelId (set by SentinelAgentFactory) takes precedence when present.
        ChatCompletionsClient client = new(
            new Uri(model.Endpoint ?? throw new ArgumentException("Azure AI model inference endpoint required")),
            new AzureKeyCredential(model.ApiKey ?? throw new ArgumentException("Azure AI model inference API key required")));

        return client.AsIChatClient(model.ModelId ?? throw new ArgumentException("Azure AI model ID required"));
    }








    private static IChatClient CreateFoundryClient(ModelProfile model)
    {
        // Azure AI Foundry exposes an OpenAI-compatible v1 endpoint
        // (https://<resource>.services.ai.azure.com/openai/v1) that accepts an API key,
        // so the stock OpenAI client works unchanged (same pattern as GitHub Models).
        OpenAIClient client = new(
            new ApiKeyCredential(model.ApiKey ?? throw new ArgumentException("Foundry API key required")),
            new OpenAIClientOptions { Endpoint = new Uri(model.Endpoint ?? throw new ArgumentException("Foundry endpoint required")) });

        ChatClient? chatClient = client.GetChatClient(model.ModelId ?? throw new ArgumentException("Foundry model ID required"));
        return chatClient.AsIChatClient()!;
    }

    private IChatClient CreateAnthropicClient(ModelProfile model)
    {
        ArgumentNullException.ThrowIfNull(model.Endpoint, nameof(model.Endpoint));
        ArgumentNullException.ThrowIfNull(model.ApiKey, nameof(model.ApiKey));
        ArgumentNullException.ThrowIfNull(model.ModelId, nameof(model.ModelId));

        HttpClient httpClient = _httpClientsByEndpoint.GetOrAdd(model.Endpoint, static uri => new()
        {
            BaseAddress = new(uri),
            Timeout = TimeSpan.FromMinutes(10)
        });

        AnthropicClient client = new(model.ApiKey, httpClient);

        // The SDK builds absolute request URLs from ApiUrlFormat (default api.anthropic.com/{0}/{1}),
        // so a custom profile endpoint (e.g. a gateway) is honored by rewriting the format string.
        client.ApiUrlFormat = $"{model.Endpoint.TrimEnd('/')}/{{0}}/{{1}}";

        // MessagesEndpoint implements IChatClient; the model id and tuning parameters
        // (temperature, max tokens, top-k/p) are supplied per request via ChatOptions.
        return client.Messages;
    }








    private static IChatClient CreateAzureOpenAIClient(ModelProfile model)
    {
        AzureOpenAIClient azureClient = new(new Uri(model.Endpoint ?? throw new ArgumentException("Azure OpenAI endpoint required")), new AzureKeyCredential(model.ApiKey ?? throw new ArgumentException("Azure OpenAI API key required")));
        ChatClient? chatClient = azureClient.GetChatClient(model.ModelId ?? throw new ArgumentException("Azure OpenAI model ID required"));
        return chatClient.AsIChatClient();
    }








    private static IChatClient CreateGitHubModelsClient(ModelProfile model)
    {
        OpenAIClient client = new(new ApiKeyCredential(model.ApiKey ?? throw new ArgumentException("GitHub token required")), new OpenAIClientOptions { Endpoint = new Uri(model.Endpoint ?? "https://models.inference.ai.azure.com") });
        ChatClient? chatClient = client.GetChatClient(model.ModelId ?? throw new ArgumentException("GitHub model ID required"));
        return chatClient.AsIChatClient()!;
    }









    private OllamaApiClient CreateOllamaClient(ModelProfile model)
    {
        string endpoint = model.Endpoint ?? "http://127.0.0.1:11434";
        string modelId = model.ModelId ?? "gemma4";

        HttpClient httpClient = _httpClientsByEndpoint.GetOrAdd(endpoint, static uri => new()
        {
            BaseAddress = new(uri),
            Timeout = TimeSpan.FromMinutes(10)
        });

        OllamaApiClient client = new(httpClient, modelId);
        client.SelectedModel = modelId;

        return client;
    }








    private static IChatClient CreateOnnxClient(ModelProfile model)
    {
        string modelPath = model.ModelPath ?? throw new ArgumentException("ONNX model path required (ModelProfile.ModelPath)");
        return new OnnxChatClient(modelPath, model.ExecutionProvider);
    }










    private static IChatClient CreateOpenAIClient(ModelProfile model)
    {
        OpenAIClient client = new(model.ApiKey ?? throw new ArgumentException("OpenAI API key required"));
        ChatClient? chatClient = client.GetChatClient(model.ModelId ?? throw new ArgumentException("OpenAI model ID required"));
        return chatClient.AsIChatClient()!;
    }





    /// <summary>
    ///     ONNX Runtime GenAI chat client. The managed wrapper requires an <see cref="OgaHandle" />
    ///     (native runtime lifetime) that outlives the chat client itself, so this wrapper owns both.
    ///     Text-only generation; image/audio inputs are not supported. Model tuning parameters
    ///     (temperature, max output tokens, top-k/p, stop sequences) flow through ChatOptions.
    /// </summary>
    private sealed class OnnxChatClient : IChatClient
    {
        private static readonly HashSet<string> KnownExecutionProviders = new(StringComparer.OrdinalIgnoreCase)
        {
            "CPU", "CUDA", "DML", "QNN", "OpenVINO", "VitisAI"
        };

        private readonly OgaHandle _ogaHandle;
        private readonly IChatClient _inner;
        private bool _disposed;

        public OnnxChatClient(string modelPath, string? executionProvider)
        {
            _ogaHandle = new OgaHandle();
            try
            {
                if (string.IsNullOrWhiteSpace(executionProvider))
                {
                    _inner = new OnnxRuntimeGenAIChatClient(modelPath, new OnnxRuntimeGenAIChatClientOptions());
                }
                else
                {
                    string provider = executionProvider.Trim();
                    if (!KnownExecutionProviders.Contains(provider))
                    {
                        throw new ArgumentException($"Unknown ONNX execution provider '{provider}'. Supported: CPU, CUDA, DML, QNN, OpenVINO, VitisAI", nameof(executionProvider));
                    }

                    // Selecting an execution provider requires building the model config with the
                    // provider appended (models ship a default config.json with its own providers).
                    Config config = new(modelPath);
                    config.ClearProviders();
                    config.AppendProvider(provider.ToLowerInvariant());
                    _inner = new OnnxRuntimeGenAIChatClient(config, true, new OnnxRuntimeGenAIChatClientOptions());
                }
            }
            catch
            {
                _ogaHandle.Dispose();
                throw;
            }
        }

        public Task<ChatResponse> GetResponseAsync(IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
            => _inner.GetResponseAsync(messages, options, cancellationToken);

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
            => _inner.GetStreamingResponseAsync(messages, options, cancellationToken);

        public object? GetService(Type serviceType, object? serviceKey = null)
            => _inner.GetService(serviceType, serviceKey);

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _inner.Dispose();
            _ogaHandle.Dispose();
        }
    }

}
