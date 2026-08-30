// Solution: SentinelCore
// Project:   SentinelCore.UI
// File:         MainWindow.xaml.cs
// Author: Kyle L. Crowder
// Build Num:  083003



using System.Windows;

using SentinelCore.UI.Services;
using SentinelCore.UI.ViewModels;




namespace SentinelCore.UI;





/// <summary>
///     Main shell window for the SentinelCore application.
///     Hosts a top navigation bar and a <see cref="System.Windows.Controls.Frame" /> that
///     navigates to feature pages through <see cref="INavigationService" />.
/// </summary>
public partial class MainWindow : Window
{
    private readonly INavigationService _navigationService;

    /// <summary>
    ///     The page key of the currently displayed page; used to suppress
    ///     redundant navigation when a tab is re-selected.
    /// </summary>
    private string? _currentPageKey;





    /// <summary>
    ///     Creates the main window and initializes frame navigation.
    /// </summary>
    /// <param name="navigationService">Frame navigation backed by DI.</param>
    public MainWindow(INavigationService navigationService)
    {
        InitializeComponent();

        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _navigationService.Initialize(MainFrame);

        // Tabs navigate on check; the navigation service reports back so the
        // checked tab always mirrors the actual page (case list → detail is
        // internal navigation and intentionally leaves tabs unselected).
        NavChatButton.Checked += (_, _) => NavigateTo(typeof(CoreChatViewModel).FullName!);
        NavCasesButton.Checked += (_, _) => NavigateTo(typeof(CaseListViewModel).FullName!);
        NavCreateCaseButton.Checked += (_, _) => NavigateTo(typeof(CreateCaseViewModel).FullName!);
        _navigationService.Navigated += OnNavigated;

        NavChatButton.IsChecked = true;
    }








    /// <summary>
    ///     Navigates unless the requested page is already displayed.
    /// </summary>
    private void NavigateTo(string pageKey)
    {
        if (_currentPageKey != pageKey)
        {
            _navigationService.NavigateTo(pageKey);
        }
    }





    /// <summary>
    ///     Updates the tracked page key when the navigation service completes a navigation.
    /// </summary>
    private void OnNavigated(object? sender, string pageKey)
    {
        _currentPageKey = pageKey;
    }
}
