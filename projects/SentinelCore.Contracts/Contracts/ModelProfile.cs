// Solution: SentinelCore
// Project:   SentinelCore.Contracts
// File:         ModelProfile.cs
// Author: Kyle L. Crowder
// Build Num:  082808



using SentinelCore.Abstractions;

namespace SentinelCore.Contracts;





/// <summary>
///     Configuration for a single model endpoint used by an agent.
///     Single source of truth for model tuning parameters — consumed by
///     <c>ChatClientAgentOptions.ChatOptions</c> during agent construction.
/// </summary>
public sealed class ModelProfile
{

    public ModelProfile(string endpoint, string modelId, float? temperature, int? maxOutputTokens = 16000, int topK = 1, float topP = .1f, ModelProvider provider = ModelProvider.Ollama, string? apiKey = null, string? modelPath = null, string? executionProvider = null)
    {
        Throw.IfNull(endpoint);
        Throw.IfNull(modelId);
        Throw.IfNull(temperature);
        Endpoint = endpoint;
        Provider = provider;
        ModelId = modelId;
        Temperature = temperature ?? 0;
        TopK = topK;
        TopP = topP;
        MaxOutputTokens = maxOutputTokens;
        ApiKey = apiKey;
        ModelPath = modelPath;
        ExecutionProvider = executionProvider;
    }








    public ModelProfile()
    {
    }








    /// <summary>
    ///     API key for providers that require authentication (OpenAI, Azure OpenAI, GitHub Models, Anthropic).
    /// </summary>
    public string? ApiKey { get; init; }

    /// <summary>
    ///     The model endpoint URL (e.g. "http://127.0.0.1:11434" for Ollama).
    /// </summary>
    public string? Endpoint { get; init; }

    /// <summary>
    ///     Execution provider for ONNX models (e.g., "CPU", "CUDA", "DML", "QNN", "OpenVINO", "VitisAI").
    /// </summary>
    public string? ExecutionProvider { get; init; }

    /// <summary>
    ///     Maximum number of tokens the model is allowed to generate.
    /// </summary>
    public int? MaxOutputTokens { get; set; }

    /// <summary>
    ///     The model identifier (e.g. "llama3.2", "gpt-4").
    /// </summary>
    public string? ModelId { get; set; }

    /// <summary>
    ///     Model file path for ONNX/local models.
    /// </summary>
    public string? ModelPath { get; init; }

    /// <summary>
    ///     The model provider (e.g. Ollama, OpenAI, AzureOpenAI, GitHubModels, Anthropic, Foundry, Azure, ONNX).
    /// </summary>
    public ModelProvider Provider { get; init; }

    /// <summary>
    ///     Temperature for this specific model. Lower values make the model more deterministic, while higher values increase
    ///     randomness and creativity. Some models may not support modifying the temperature. In such cases, this value will be
    ///     ignored.
    /// </summary>
    public float Temperature { get; set; }

    /// <summary>
    ///     Top-k sampling parameter. Limits sampling to the k most probable tokens.
    /// </summary>
    public int TopK { get; set; }

    /// <summary>
    ///     Top-p (nucleus) sampling parameter.
    /// </summary>
    public float TopP { get; set; }








    //Development use only. These are not intended to be used in production, but rather to provide a convenient way to create model profiles for testing and development.
    public static ModelProfile Glm5()
    {
        return new ModelProfile("http://127.0.0.1:11111", "glm-5.1:cloud", 0.1f, 14000, 1, .3f);
    }








    public static ModelProfile Gpt120()
    {
        return new ModelProfile("http://127.0.0.1:11111", "gemma4", 0.1f, 14000, 1, .3f);
    }








    public static ModelProfile Gpt20()
    {
        return new ModelProfile("http://127.0.0.1:11111", "gpt-oss:20b-cloud", 0.1f, 14000, 1, .3f);
    }








    public enum ModelProvider
    {
        Ollama,
        OpenAI,
        AzureOpenAI,
        GitHubModels,
        Anthropic,
        Foundry,
        Azure,
        OnnxRuntime
    }
}