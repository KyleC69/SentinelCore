// Solution: SentinelCore
// Project:   SentinelCore.UI
// File:         CreateCasePage.xaml.cs
// Author: Kyle L. Crowder
// Build Num:  091200



using System.Windows.Controls;

using SentinelCore.UI.ViewModels;




namespace SentinelCore.UI.Views;





/// <summary>
///     Code-behind for the Create Case page.
///     Responsibilities scoped to this file: ViewModel wiring.
/// </summary>
public partial class CreateCasePage : Page
{
    /// <summary>
    ///     Creates the page and binds the provided view-model.
    /// </summary>
    /// <param name="viewModel">The create-case view-model.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="viewModel" /> is <c>null</c>.</exception>
    public CreateCasePage(CreateCaseViewModel? viewModel)
    {
        _ = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        InitializeComponent();
        DataContext = viewModel;
    }
}