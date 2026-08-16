// Solution: SentinelCore
// Project:   SentinelCoreAdmin
// File:         TraceLogPage.xaml.cs
// Author: Kyle L. Crowder
// Build Num:  081602



using System.Windows.Controls;

using JetBrains.Annotations;

using Microsoft.Web.WebView2.Core;

using SentinelCoreAdmin.ViewModels;




namespace SentinelCoreAdmin.Views;





public partial class TraceLogPage : Page
{
    private readonly TraceLogViewModel _viewModel;








    public TraceLogPage([CanBeNull] TraceLogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        _viewModel = viewModel;
        _viewModel.Initialize(webView);
    }








    private void OnNavigationCompleted([CanBeNull] object sender, [CanBeNull] CoreWebView2NavigationCompletedEventArgs e) => _viewModel.OnNavigationCompleted(sender, e);
}