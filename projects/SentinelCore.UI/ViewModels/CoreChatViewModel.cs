// Solution: SentinelCore
// Project:   SentinelCore.UI
// File:         CoreChatViewModel.cs
// Author: Kyle L. Crowler
// Build Num:  083003



using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

using SentinelCore.Abstractions;
using SentinelCore.Application;
using SentinelCore.Cfe;
using SentinelCore.Events;
using SentinelCore.UI.Services;

using System.Collections.ObjectModel;




namespace SentinelCore.UI.ViewModels;


/// <summary>
///     WPF view-model for the SentinelCore chat page.
///     Uses CommunityToolkit.Mvvm v8 source generators for
///     property change notification and command management.
///     Decoupled from <see cref="System.Windows.Application" /> via
///     <see cref="IDispatcherService" /> and <see cref="IClipboardService" /> for testability.
/// </summary>
public sealed partial class CoreChatViewModel : ObservableObject, IDisposable, INavigationAware
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    private int _alertedCount;

    /// <summary>
    ///     A <see cref="CancellationToken" /> that is cancelled when the application is shutting down.
    ///     This is threaded through the send path so in-flight orchestrations are cancelled cooperatively.
    /// </summary>
    private readonly CancellationToken _appShutdownToken;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    private int _blockedCount;

    private readonly ICaseFlowEngine _caseFlowEngine;

    private readonly IClipboardService _clipboardService;

    private readonly IDispatcherService _dispatcher;

    private bool _disposed;

    private readonly IModelConfigGate _modelConfigGate;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    private int _escalatedCount;

    private readonly ISentinelCoreEvents _events;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    private string _inputText = string.Empty;

    [ObservableProperty] private int _investigationCount;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    private bool _isBusy;

    /// <summary>
    ///     A linked CTS that combines the per-send cancellation with the app shutdown token.
    ///     Created fresh for each <see cref="SendAsync" /> call and disposed when the send completes.
    /// </summary>
    private CancellationTokenSource? _linkedCts;

    private readonly ILogger<CoreChatViewModel> _logger;

    [ObservableProperty] private int _openCount;

    private readonly IOrchestrationControl _orchestrationControl;

    [ObservableProperty] private string _statusMessage = string.Empty;





    /// <summary>
    ///     Creates a new instance with an explicit application shutdown token.
    ///     The token is linked to every send operation so in-flight work
    ///     is cancelled when the app shuts down.
    /// </summary>
    /// <param name="orchestrationControl">The orchestration control for initializing workflows.</param>
    /// <param name="events">The event bus for SentinelCore output and error events.</param>
    /// <param name="caseFlowEngine">The case flow engine for querying case status counts.</param>
    /// <param name="logger">The logger for this view-model.</param>
    /// <param name="dispatcher">The dispatcher service for thread-affinity marshaling.</param>
    /// <param name="clipboardService">The clipboard service for copying message text.</param>
    /// <param name="modelConfigGate">The gate that reports whether agent models are configured.</param>
    /// <param name="appShutdownToken">A token cancelled when the application begins shutting down.</param>
    public CoreChatViewModel(
        IOrchestrationControl orchestrationControl,
        ISentinelCoreEvents events,
        ICaseFlowEngine caseFlowEngine,
        ILogger<CoreChatViewModel> logger,
        IDispatcherService dispatcher,
        IClipboardService clipboardService,
        IModelConfigGate modelConfigGate,
        CancellationToken appShutdownToken)
    {
        _orchestrationControl = orchestrationControl ?? throw new ArgumentNullException(nameof(orchestrationControl));
        _events = events ?? throw new ArgumentNullException(nameof(events));
        _caseFlowEngine = caseFlowEngine ?? throw new ArgumentNullException(nameof(caseFlowEngine));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _clipboardService = clipboardService ?? throw new ArgumentNullException(nameof(clipboardService));
        _modelConfigGate = modelConfigGate ?? throw new ArgumentNullException(nameof(modelConfigGate));
        _appShutdownToken = appShutdownToken;


        _logger.LogInformation("CoreChatViewModel initialized.");

        _events.SentinelOutputEvent += OnSentinelOutput;
        _events.ErrorOccurred += OnErrorOccurred;

        AddWelcomeMessage();
        _logger.LogTrace("CoreChatViewModel ready for user input.");

        // Fire-and-forget initial case count load, cancelled if the app shuts down
        _ = RefreshCaseCountsAsync();
    }






    /// <summary>
    ///     Gets the message describing incomplete model configuration, or an empty
    ///     string when every agent is configured. Bound to the warning banner.
    /// </summary>
    public string ConfigGateMessage => _modelConfigGate.BuildGateMessage();

    /// <summary>
    ///     Gets a value indicating whether model configuration is incomplete,
    ///     driving the warning banner visibility.
    /// </summary>
    public bool IsConfigIncomplete => !_modelConfigGate.IsConfigurationComplete;




    /// <summary>
    ///     Gets the observable collection of chat messages displayed in the UI.
    ///     All mutations are marshaled to the UI thread by <see cref="AddToMessages" />.
    /// </summary>
    public ObservableCollection<ChatMessage> Messages { get; } = new();





    /// <summary>
    ///     Releases the linked cancellation source and unsubscribes from the
    ///     shared event bus so no callbacks reach a dead view-model.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Cancel any in-flight orchestration
        _linkedCts?.Cancel();
        _linkedCts?.Dispose();
        _linkedCts = null;

        // Unsubscribe from events to prevent memory leaks and callbacks after disposal
        _events.SentinelOutputEvent -= OnSentinelOutput;
        _events.ErrorOccurred -= OnErrorOccurred;
    }






    private void AddToMessages(ChatMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (_dispatcher.CheckAccess())
        {
            Messages.Add(message);
        }
        else
        {
            _dispatcher.Invoke(() => Messages.Add(message));
        }
    }






    private void AddWelcomeMessage()
    {
        Messages.Add(new ChatMessage(ChatRole.Assistant, "# SentinelCore 🛡️\n\nForensic investigation platform — ready.\n\nDescribe a signal or security event and I will open an investigation case.\n\n- Press **Enter** to send\n- Press **Shift+Enter** for a new line"));
    }






    private bool CanCancel()
    {
        return IsBusy;
    }






    private bool CanSend()
    {
        // Gate: agent models must be configured before any orchestration can run.
        return !IsBusy && !string.IsNullOrWhiteSpace(InputText) && _modelConfigGate.IsConfigurationComplete;
    }






    [RelayCommand(CanExecute = nameof(CanCancel))]
    private Task CancelAsync(CancellationToken token)
    {
        _linkedCts?.Cancel();
        return Task.CompletedTask;
    }






    /// <summary>
    ///     Copies a single chat message's raw text to the system clipboard
    ///     via the <see cref="IClipboardService" /> abstraction.
    /// </summary>
    /// <param name="text">The message text to copy.</param>
    [RelayCommand]
    private void CopyMessage(string? text)
    {
        if (!string.IsNullOrEmpty(text))
        {
            _clipboardService.SetText(text);
        }
    }






    private void OnErrorOccurred(string message, Exception? exception)
    {
        StatusMessage = message;
    }






    private void OnSentinelOutput(SentinelOutputEventArgs args)
    {
        StatusMessage = $"Agent: {args.AgentName} {args.Message}";
    }






    private async Task RefreshCaseCountsAsync()
    {
        try
        {
            // Single grouped query instead of one round-trip per status.
            IReadOnlyDictionary<CaseStatus, int> counts = await _caseFlowEngine.GetCaseStatusCountsAsync(_appShutdownToken).ConfigureAwait(false);

            if (_dispatcher.CheckAccess())
            {
                ApplyCaseCounts(counts);
            }
            else
            {
                await _dispatcher.InvokeAsync(() => ApplyCaseCounts(counts)).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // App is shutting down — ignore
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to refresh case status counts.");
        }
    }





    /// <summary>
    ///     Applies freshly loaded case counts to the telemetry properties.
    ///     Must run on the UI thread.
    /// </summary>
    /// <param name="counts">The per-status case counts.</param>
    private void ApplyCaseCounts(IReadOnlyDictionary<CaseStatus, int> counts)
    {
        OpenCount = counts.TryGetValue(CaseStatus.Open, out int open) ? open : 0;
        InvestigationCount = counts.TryGetValue(CaseStatus.Investigation, out int investigation) ? investigation : 0;
        EscalatedCount = counts.TryGetValue(CaseStatus.Escalated, out int escalated) ? escalated : 0;
        AlertedCount = counts.TryGetValue(CaseStatus.Alerted, out int alerted) ? alerted : 0;
        BlockedCount = counts.TryGetValue(CaseStatus.Blocked, out int blocked) ? blocked : 0;
    }






    /// <summary>
    ///     Navigated away from the chat page. No persistent resources hold yet.
    /// </summary>
    public void OnNavigatedFrom()
    {
    }






    /// <summary>
    ///     Navigated back to the chat page — re-query case counts and refresh the
    ///     configuration gate so the banner reflects changes made on the config page.
    /// </summary>
    public void OnNavigatedTo(object? parameter)
    {
        _ = RefreshCaseCountsAsync();
        OnPropertyChanged(nameof(IsConfigIncomplete));
        OnPropertyChanged(nameof(ConfigGateMessage));
        SendCommand.NotifyCanExecuteChanged();
    }






    [RelayCommand(CanExecute = nameof(CanSend))]
    private async Task SendAsync(CancellationToken token)
    {
        IsBusy = true;
        _linkedCts?.Dispose();

        // Link the per-send cancellation with the app shutdown token so that
        // a user-initiated cancel OR an app shutdown both cancel the operation.
        _linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_appShutdownToken, token);

        try
        {
            ChatMessage msg = new(ChatRole.User, InputText);
            AddToMessages(msg);
            InputText = string.Empty;

            WorkflowExecutionResult? result = await _orchestrationControl.InitializeOrchestrationAsync(msg, _linkedCts.Token);

            if (result?.OutputMessages is not null)
            {
                foreach (ChatMessage response in result.OutputMessages) AddToMessages(response);
            }
        }
        catch (OperationCanceledException) when (_appShutdownToken.IsCancellationRequested)
        {
            _logger.LogInformation("Send cancelled — application is shutting down.");
            StatusMessage = "Shutting down…";
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Send cancelled by user.");
            StatusMessage = "Request cancelled.";
        }
        catch (Exception ex)
        {
            // Surface orchestration failures in-chat instead of letting them escape to
            // the dispatcher's unhandled-exception handler, which shuts the app down.
            _logger.LogError(ex, "Orchestration failed while sending a chat message.");
            StatusMessage = $"Request failed: {ex.Message}";
            AddToMessages(new ChatMessage(ChatRole.Assistant, $"⚠️ The request failed: {ex.Message}"));
        }
        finally
        {
            IsBusy = false;
            _linkedCts?.Dispose();
            _linkedCts = null;
        }

        // An orchestration may have created or advanced cases — refresh the telemetry panel.
        _ = RefreshCaseCountsAsync();
    }
}
