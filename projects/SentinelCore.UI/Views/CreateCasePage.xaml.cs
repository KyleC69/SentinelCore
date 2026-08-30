// Solution: SentinelCore
// Project:   SentinelCore.UI
// File:         CreateCasePage.xaml.cs
// Author: Kyle L. Crowder
// Build Num:  083003



using System.Windows.Controls;

using SentinelCore.UI.ViewModels;



namespace SentinelCore.UI.Views;





/// <summary>
///     Code-behind for the Create Case page.
///     Responsibilities scoped to this file: ViewModel wiring.
/// </summary>
public partial class CreateCasePage : Page
{

    public CreateCasePage(CreateCaseViewModel? viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
