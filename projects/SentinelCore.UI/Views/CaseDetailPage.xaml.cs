// Solution: SentinelCore
// Project:   SentinelCore.UI
// File:         CaseDetailPage.xaml.cs
// Author: Kyle L. Crowder
// Build Num:  083003



using System.Windows.Controls;

using SentinelCore.UI.ViewModels;



namespace SentinelCore.UI.Views;





/// <summary>
///     Code-behind for the Case Detail page.
///     Responsibilities scoped to this file: ViewModel wiring.
/// </summary>
public partial class CaseDetailPage : Page
{

    public CaseDetailPage(CaseDetailViewModel? viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
