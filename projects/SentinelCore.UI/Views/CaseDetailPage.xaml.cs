// Solution: SentinelCore
// Project:   SentinelCore.UI
// File:         CaseDetailPage.xaml.cs
// Author: Kyle L. Crowder
// Build Num:  091300



using System.Windows.Controls;

using SentinelCore.UI.ViewModels;




namespace SentinelCore.UI.Views;





/// <summary>
///     Code-behind for the Case Detail page.
///     Responsibilities scoped to this file: ViewModel wiring.
/// </summary>
public partial class CaseDetailPage : Page
{
    /// <summary>
    ///     Creates the page and binds the provided view-model.
    /// </summary>
    /// <param name="viewModel">The case detail view-model.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="viewModel" /> is <c>null</c>.</exception>
    public CaseDetailPage(CaseDetailViewModel? viewModel)
    {
        _ = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        InitializeComponent();
        DataContext = viewModel;
    }
}