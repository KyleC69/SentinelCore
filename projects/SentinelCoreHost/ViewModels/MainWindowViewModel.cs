// Solution: SentinelCore
// Project:   SentinelCoreHost
// File:         MainWindowViewModel.cs
// Author: Kyle L. Crowder
// Build Num:  080801



using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Input;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

using SentinelCore.Abstractions;
using SentinelCore.CaseEngine;
using SentinelCore.CaseFlow;
using SentinelCore.Events;




namespace SentinelCoreHost.ViewModels;





/// <summary>
///     WPF view-model for the SentinelCore host.
/// </summary>
public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private int _alertedCount;
    private int _blockedCount;
    private readonly ICaseFlowEngine _caseFlowEngine;
    private int _escalatedCount;
    private readonly ISentinelCoreEvents _events;

    private string _inputText = string.Empty;
    private int _investigationCount;
    private bool _isBusy;

    private readonly ILogger<MainWindowViewModel> _logger;

    private int _openCount;
    private readonly IOrchestrationControl _orchestrationControl;
    // Initialise to avoid CS8618 warning.
    private string _statusMessage = string.Empty;
    private readonly object _syncRoot = new();








    public MainWindowViewModel(IOrchestrationControl orchestrationControl, ISentinelCoreEvents events, ICaseFlowEngine caseFlowEngine, ILogger<MainWindowViewModel> logger)
    {
        BindingOperations.EnableCollectionSynchronization(Messages, _syncRoot);
        _orchestrationControl = orchestrationControl ?? throw new ArgumentNullException(nameof(orchestrationControl));
        _events = events ?? throw new ArgumentNullException(nameof(events));
        _caseFlowEngine = caseFlowEngine ?? throw new ArgumentNullException(nameof(caseFlowEngine));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogInformation("MainWindowViewModel initialized.");

        _events.SentinelOutputEvent += OnSentinelOutput;
        _events.ErrorOccurred += OnErrorOccurred;



        SendCommand = new AsyncRelayCommand(SendSignalAsync, () => CanSend, ex => Messages.Add(new ChatMessage(ChatRole.System, ex.Message)));
        CancelCommand = new RelayCommand(Cancel, () => CanCancel);
        TestCommand = new AsyncRelayCommand(ExecuteTestCall, () => true);

        AddWelcomeMessage();
        _logger.LogTrace("Terminal Trace Window initialized. Should be ready for user input.");

        // Fire-and-forget initial case count load
        _ = RefreshCaseCountsAsync();
    }








    public int AlertedCount
    {
        get => _alertedCount;
        set => SetProperty(ref _alertedCount, value);
    }

    public int BlockedCount
    {
        get => _blockedCount;
        set => SetProperty(ref _blockedCount, value);
    }

    private bool CanCancel
    {
        get => IsBusy;
    }

    private bool CanSend
    {
        get => !IsBusy && !string.IsNullOrWhiteSpace(InputText);
    }

    public ICommand CancelCommand { get; }

    public int EscalatedCount
    {
        get => _escalatedCount;
        set => SetProperty(ref _escalatedCount, value);
    }

    public string InputText
    {
        get => _inputText;
        set
        {
            if (SetProperty(ref _inputText, value))
            {
                ((AsyncRelayCommand)SendCommand).RaiseCanExecuteChanged();
                ((RelayCommand)CancelCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public int InvestigationCount
    {
        get => _investigationCount;
        set => SetProperty(ref _investigationCount, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (SetProperty(ref _isBusy, value))
            {
                ((AsyncRelayCommand)SendCommand).RaiseCanExecuteChanged();
                ((RelayCommand)CancelCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public ObservableCollection<ChatMessage> Messages { get; } = new();

    public int OpenCount
    {
        get => _openCount;
        set => SetProperty(ref _openCount, value);
    }

    public ICommand SendCommand { get; }

    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    public ICommand TestCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;








    private void AddToMessages(ChatMessage args)
    {
        if (App.Current.Dispatcher.CheckAccess())
        {
            Messages.Add(args);
        }
        else
        {
            App.Current.Dispatcher.Invoke(() => Messages.Add(args));
        }
    }








    /// <summary>
    ///     Adds the startup welcome message. Called during view-model construction so the
    ///     message is present as soon as the view's DataContext is assigned.
    /// </summary>
    private void AddWelcomeMessage()
    {
        Messages.Add(new ChatMessage(ChatRole.Assistant, "# SentinelCore 🛡️\n\nForensic investigation platform — ready.\n\nDescribe a signal or security event and I will open an investigation case.\n\n- Press **Enter** to send\n- Press **Shift+Enter** for a new line"));
    }








    private void Cancel()
    {
        ((AsyncRelayCommand)SendCommand).Cancel();
    }








    private async Task ExecuteTestCall(CancellationToken token)
    {
        if (token.IsCancellationRequested)
        {
            return;
        }

        InvestigationPlanStep step1 = new() { OperationId = "Op002", Surface = "Registry", Instruction = "Retrieve the value of the 'Computer\\HKEY_LOCAL_MACHINE\\SOFTWARE\\Khronos\\Vulkan\\Drivers' registry key." };
        InvestigationPlanStep step = new() { OperationId = "Op001", Surface = "Registry", Instruction = "Retrieve the value of the 'HKEY_CURRENT_USER\\Environment\\SENTINEL_CORE' registry key." };
    }








    private void OnErrorOccurred(string arg1, Exception arg2)
    {
        // Add message to the statusbaritem textblock StatusLbl to surface in UI.

        StatusMessage = arg1;
    }








    private void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }








    private void OnSentinelOutput(SentinelOutputEventArgs args)
    {
        StatusMessage = $"Agent: {args.AgentName} {args.Message}";

    }








    /// <summary>
    ///     Refreshes all case status counts from the database.
    /// </summary>
    private async Task RefreshCaseCountsAsync()
    {
        try
        {
            OpenCount = await _caseFlowEngine.GetCaseCountByStatusAsync(CaseStatus.Open).ConfigureAwait(false);
            InvestigationCount = await _caseFlowEngine.GetCaseCountByStatusAsync(CaseStatus.Investigation).ConfigureAwait(false);
            EscalatedCount = await _caseFlowEngine.GetCaseCountByStatusAsync(CaseStatus.Escalated).ConfigureAwait(false);
            AlertedCount = await _caseFlowEngine.GetCaseCountByStatusAsync(CaseStatus.Alerted).ConfigureAwait(false);
            BlockedCount = await _caseFlowEngine.GetCaseCountByStatusAsync(CaseStatus.Blocked).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to refresh case status counts.");
        }
    }








    /// <summary>
    ///     Sends a signal asynchronously based on the current input text.
    /// </summary>
    /// <param name="token">
    ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> that represents the asynchronous operation.
    /// </returns>
    /// <remarks>
    ///     This method adds the user's input as a message to the conversation, initiates the orchestration process,
    ///     and clears the input text upon completion. The <see cref="IsBusy" /> property is used to indicate the
    ///     operation's progress. It interacts with the orchestration control to manage workflows.
    /// </remarks>
    private async Task SendSignalAsync(CancellationToken token)
    {
        IsBusy = true;
        try
        {
            ChatMessage msg = new(ChatRole.User, _inputText);
            Messages.Add(msg);
            OnPropertyChanged(nameof(Messages));
            InputText = string.Empty;

            await _orchestrationControl.InitializeOrchestrationAsync(msg, token);
        }
        finally
        {
            IsBusy = false;
        }
    }








    private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return false;
        }

        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
