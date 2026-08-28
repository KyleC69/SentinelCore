// Solution: SentinelCore
// Project:   SentinelCoreAdmin
// File:         CoreChatViewModel.cs
// Author: Kyle L. Crowder
// Build Num:  082808



using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Data;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using JetBrains.Annotations;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

using SentinelCore.Abstractions;
using SentinelCore.Application;
using SentinelCore.Cfe;
using SentinelCore.Events;




namespace SentinelCoreAdmin.ViewModels;





/// <summary>
///     WPF view-model for the SentinelCoreAdmin chat page.
///     Uses CommunityToolkit.Mvvm v8 source generators for
///     property change notification and command management.
/// </summary>
public sealed partial class CoreChatViewModel : ObservableObject, IDisposable
{

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(SendCommand))] [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    private int _alertedCount;

    /// <summary>
    ///     A <see cref="CancellationToken" /> that is cancelled when the application is shutting down.
    ///     This is threaded through the send path so in-flight orchestrations are cancelled cooperatively.
    /// </summary>
    private readonly CancellationToken _appShutdownToken;

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(SendCommand))] [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    private int _blockedCount;

    private readonly ICaseFlowEngine _caseFlowEngine;

    private bool _disposed;

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(SendCommand))] [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    private int _escalatedCount;

    private readonly ISentinelCoreEvents _events;

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(SendCommand))] [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    private string _inputText = string.Empty;

    [ObservableProperty] private int _investigationCount;

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(SendCommand))] [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
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

    private readonly object _syncRoot = new();








    public CoreChatViewModel([CanBeNull] IOrchestrationControl orchestrationControl, [CanBeNull] ISentinelCoreEvents events, [CanBeNull] ICaseFlowEngine caseFlowEngine, [CanBeNull] ILogger<CoreChatViewModel> logger) : this(orchestrationControl, events, caseFlowEngine, logger, CancellationToken.None)
    {
    }








    /// <summary>
    ///     Creates a new instance with an explicit application shutdown token.
    ///     The token is linked to every send operation so in-flight work
    ///     is cancelled when the app shuts down.
    /// </summary>
    public CoreChatViewModel([NotNull] IOrchestrationControl orchestrationControl, [NotNull] ISentinelCoreEvents events, [NotNull] ICaseFlowEngine caseFlowEngine, [NotNull] ILogger<CoreChatViewModel> logger, CancellationToken appShutdownToken)
    {
        BindingOperations.EnableCollectionSynchronization(Messages, _syncRoot);

        _orchestrationControl = orchestrationControl ?? throw new ArgumentNullException(nameof(orchestrationControl));
        _events = events ?? throw new ArgumentNullException(nameof(events));
        _caseFlowEngine = caseFlowEngine ?? throw new ArgumentNullException(nameof(caseFlowEngine));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _appShutdownToken = appShutdownToken;

        _logger.LogInformation("CoreChatViewModel initialized.");

        _events.SentinelOutputEvent += OnSentinelOutput;
        _events.ErrorOccurred += OnErrorOccurred;

        AddWelcomeMessage();
        _logger.LogTrace("CoreChatViewModel ready for user input.");

        // Fire-and-forget initial case count load, cancelled if the app shuts down
        _ = RefreshCaseCountsAsync();
    }








    public ObservableCollection<ChatMessage> Messages { get; } = new();








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








    private void AddToMessages([CanBeNull] ChatMessage args)
    {
        if (Application.Current.Dispatcher.CheckAccess())
        {
            Messages.Add(args);
        }
        else
        {
            Application.Current.Dispatcher.Invoke(() => Messages.Add(args));
        }
    }








    private void AddWelcomeMessage()
    {
        Messages.Add(new ChatMessage(ChatRole.Assistant, "# SentinelCore 🛡️\n\nForensic investigation platform — ready.\n\nDescribe a signal or security event and I will open an investigation case.\n\n- Press **Enter** to send\n- Press **Shift+Enter** for a new line"));
    }








    private bool CanCancel() => IsBusy;


    private bool CanSend() => !IsBusy && !string.IsNullOrWhiteSpace(InputText);








    [RelayCommand(CanExecute = nameof(CanCancel))]
    private Task CancelAsync(CancellationToken token)
    {
        _linkedCts?.Cancel();
        return Task.CompletedTask;
    }








    /// <summary>
    ///     Copies a single chat message's raw text to the system clipboard.
    /// </summary>
    /// <param name="text">The message text to copy.</param>
    [RelayCommand]
    private void CopyMessage(string? text)
    {
        if (!string.IsNullOrEmpty(text))
        {
            Clipboard.SetText(text);
        }
    }








    private void OnErrorOccurred([NotNull] string message, [CanBeNull] Exception exception)
    {
        StatusMessage = message;
    }








    private void OnSentinelOutput([NotNull] SentinelOutputEventArgs args)
    {
        StatusMessage = $"Agent: {args.AgentName} {args.Message}";
    }








    private async Task RefreshCaseCountsAsync()
    {
        try
        {
            OpenCount = await _caseFlowEngine.GetCaseCountByStatusAsync(CaseStatus.Open, _appShutdownToken).ConfigureAwait(false);
            InvestigationCount = await _caseFlowEngine.GetCaseCountByStatusAsync(CaseStatus.Investigation, _appShutdownToken).ConfigureAwait(false);
            EscalatedCount = await _caseFlowEngine.GetCaseCountByStatusAsync(CaseStatus.Escalated, _appShutdownToken).ConfigureAwait(false);
            AlertedCount = await _caseFlowEngine.GetCaseCountByStatusAsync(CaseStatus.Alerted, _appShutdownToken).ConfigureAwait(false);
            BlockedCount = await _caseFlowEngine.GetCaseCountByStatusAsync(CaseStatus.Blocked, _appShutdownToken).ConfigureAwait(false);
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
                foreach (ChatMessage response in result.OutputMessages)
                {
                    AddToMessages(response);
                }
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
        finally
        {
            IsBusy = false;
            _linkedCts?.Dispose();
            _linkedCts = null;
        }
    }
}