// Solution: SentinelCoreLib
// Project:   SentinelCoreHost
// File:         MainWindowViewModel.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

using Microsoft.Extensions.AI;

using SentinelCoreLib.Application;




namespace SentinelCoreHost.ViewModels;





/// <summary>
///     WPF view-model for the SentinelCore host.
/// </summary>
public sealed class MainWindowViewModel : INotifyPropertyChanged
{

    private string _inputText = string.Empty;

    private readonly InvestigationControl _investigationControl;
    private bool _isBusy;








    public MainWindowViewModel(InvestigationControl investigationControl)
    {
        _investigationControl = investigationControl;

        SendCommand = new AsyncRelayCommand(ExecuteInvestigationAsync, () => CanSend, ex => Messages.Add(new ChatMessage(ChatRole.System, content: ex.Message)));
        CancelCommand = new RelayCommand(Cancel, () => CanCancel);

        AddWelcomeMessage();
    }








    private bool CanCancel
    {
        get => IsBusy;
    }

    public ICommand CancelCommand { get; }

    private bool CanSend
    {
        get => !IsBusy && !string.IsNullOrWhiteSpace(InputText);
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

    public ICommand SendCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;








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








    /// <summary>
    ///     Executes an investigation asynchronously based on the current input text.
    ///     Agent activity and reasoning are exposed to UI using Event system and the UI needs to subscribe the events for
    ///     interaction flow.
    ///     This method currently manages the busy state of the view-model, may be removed in future versions, currently
    ///     instituted for debugging agent management.
    /// </summary>
    /// <param name="token">
    ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> that represents the asynchronous operation.
    /// </returns>
    /// <remarks>
    ///     This method adds the user's input as a message to the conversation, initiates the case orchestration
    ///     process, and clears the input text upon completion. The <see cref="IsBusy" /> property is used to indicate
    ///     the operation's progress.
    /// </remarks>
    private async Task ExecuteInvestigationAsync(CancellationToken token)
    {
        IsBusy = true;
        try
        {
            ChatMessage msg = new(ChatRole.User, _inputText);
            Messages.Add(msg);

            InputText = string.Empty;


            await _investigationControl.StartCaseOrchestration(msg);


        }
        finally
        {
            IsBusy = false;
        }
    }








    private void OnPropertyChanged([CallerMemberName] string propertyName = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));








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