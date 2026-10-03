// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         ResilientChatClient.cs
// Author: Kyle L. Crowder
// Build Num:  100310



using SentinelCore.Contracts.Abstractions;
using SentinelCore.Contracts.Contracts;




namespace SentinelCore.Orchestrations.Agents;





/// <summary>
///     Provides endpoint preflight validation and bounded retry for transient chat-client failures.
/// </summary>
/// <remarks>
///     This wrapper is applied in the agent construction pipeline so workflow executors remain focused on orchestration
///     logic.
/// </remarks>
public sealed class ResilientChatClient : DelegatingChatClient
{

    private readonly string _agentName;
    private readonly ModelProfile _model;
    private readonly ISystemReporter _reporter;
    private const int MaxRetryAttempts = 3;
    private static readonly HttpClient EndpointProbeClient = new() { Timeout = TimeSpan.FromSeconds(10) };
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(2);








    /// <summary>
    ///     Initializes a new instance of the <see cref="ResilientChatClient" /> class.
    /// </summary>
    /// <param name="innerClient">The wrapped chat client.</param>
    /// <param name="model">The model profile associated with the wrapped client.</param>
    /// <param name="reporter">The system reporter used for diagnostics.</param>
    /// <param name="agentName">The agent name associated with this client instance.</param>
    public ResilientChatClient(IChatClient innerClient, ModelProfile model, ISystemReporter reporter, string agentName) : base(innerClient)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _reporter = reporter ?? throw new ArgumentNullException(nameof(reporter));
        _agentName = string.IsNullOrWhiteSpace(agentName) ? "Agent" : agentName;
    }








    /// <summary>
    ///     Gets a chat response with bounded retries for transient provider failures.
    /// </summary>
    /// <param name="messages">The message sequence for the provider call.</param>
    /// <param name="options">Optional chat options.</param>
    /// <param name="cancellationToken">Cancellation token for cooperative cancellation.</param>
    /// <returns>A <see cref="ChatResponse" /> returned by the wrapped provider client.</returns>
    public override async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messages);

        if (!await ValidateEndpointAsync(cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException($"[{_agentName}] Model endpoint is unavailable or not responding.");
        }

        for (int attempt = 1; attempt <= MaxRetryAttempts; attempt++)
            try
            {
                return await base.GetResponseAsync(messages, options, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested && attempt < MaxRetryAttempts)
            {
                _reporter.ReportWarning($"[{_agentName}] Provider call timed out on attempt {attempt}/{MaxRetryAttempts}. Retrying.", ex);
                await DelayRetryAsync(attempt, cancellationToken).ConfigureAwait(false);
            }
            catch (TimeoutException ex) when (!cancellationToken.IsCancellationRequested && attempt < MaxRetryAttempts)
            {
                _reporter.ReportWarning($"[{_agentName}] Provider timeout on attempt {attempt}/{MaxRetryAttempts}. Retrying.", ex);
                await DelayRetryAsync(attempt, cancellationToken).ConfigureAwait(false);
            }
            catch (HttpRequestException ex) when (!cancellationToken.IsCancellationRequested && attempt < MaxRetryAttempts)
            {
                _reporter.ReportWarning($"[{_agentName}] Provider connection failed on attempt {attempt}/{MaxRetryAttempts}. Retrying.", ex);
                await DelayRetryAsync(attempt, cancellationToken).ConfigureAwait(false);
            }

        throw new InvalidOperationException($"[{_agentName}] Provider call did not return a response after {MaxRetryAttempts} attempts.");
    }








    /// <summary>
    ///     Waits for a brief delay before a retry attempt.
    /// </summary>
    /// <param name="attempt">The current retry attempt.</param>
    /// <param name="cancellationToken">The cancellation token that can cancel the delay.</param>
    /// <returns>A task representing the asynchronous delay.</returns>
    private static async Task DelayRetryAsync(int attempt, CancellationToken cancellationToken)
    {
        TimeSpan delay = RetryDelay + TimeSpan.FromMilliseconds(attempt * 250);
        await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
    }








    /// <summary>
    ///     Performs a lightweight endpoint probe for the configured model provider endpoint.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> when the endpoint is reachable; otherwise, <c>false</c>.</returns>
    private async Task<bool> ValidateEndpointAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_model.Endpoint))
        {
            _reporter.ReportWarning($"[{_agentName}] Model endpoint is not configured for provider {_model.Provider}.");
            return false;
        }

        string endpoint = _model.Endpoint;

        try
        {
            Uri baseUri = new(endpoint);
            Uri probeUri = _model.Provider == ModelProfile.ModelProvider.Ollama ? new Uri(baseUri, "/api/tags") : baseUri;

            HttpMethod method = _model.Provider == ModelProfile.ModelProvider.Ollama ? HttpMethod.Get : HttpMethod.Head;

            using HttpRequestMessage request = new(method, probeUri);
            using HttpResponseMessage response = await EndpointProbeClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

            // Reachability check: any HTTP response means endpoint is reachable.
            if ((int)response.StatusCode >= 500)
            {
                _reporter.ReportWarning($"[{_agentName}] Model endpoint probe returned server error {(int)response.StatusCode} ({response.StatusCode}) at {probeUri}.");
                return false;
            }

            return true;
        }
        catch (HttpRequestException ex) when (_model.Provider != ModelProfile.ModelProvider.Ollama)
        {
            // Some providers reject HEAD; retry once with GET for reachability-only validation.
            try
            {
                Uri fallbackUri = new(endpoint);
                using HttpRequestMessage fallbackRequest = new(HttpMethod.Get, fallbackUri);
                using HttpResponseMessage fallbackResponse = await EndpointProbeClient.SendAsync(fallbackRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

                if ((int)fallbackResponse.StatusCode >= 500)
                {
                    _reporter.ReportWarning($"[{_agentName}] Model endpoint probe fallback returned server error {(int)fallbackResponse.StatusCode} ({fallbackResponse.StatusCode}) at {fallbackUri}.");
                    return false;
                }

                return true;
            }
            catch (Exception fallbackEx)
            {
                _reporter.ReportWarning($"[{_agentName}] Model endpoint probe failed at {endpoint}.", fallbackEx);
                return false;
            }
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _reporter.ReportWarning($"[{_agentName}] Model endpoint probe timed out at {endpoint}.", ex);
            return false;
        }
        catch (Exception ex)
        {
            _reporter.ReportWarning($"[{_agentName}] Model endpoint probe failed at {endpoint}.", ex);
            return false;
        }
    }
}