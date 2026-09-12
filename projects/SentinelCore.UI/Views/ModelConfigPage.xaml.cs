// Solution: SentinelCore
// Project:   SentinelCore.UI
// File:         ModelConfigPage.xaml.cs
// Author: Kyle L. Crowder
// Build Num:  091200



using System.Windows;
using System.Windows.Controls;

using SentinelCore.UI.Models;
using SentinelCore.UI.ViewModels;




namespace SentinelCore.UI.Views;





/// <summary>
///     Code-behind for the Model Configuration page.
///     Responsibilities scoped to this file: ViewModel wiring and pushing the
///     PasswordBox value into the card (PasswordBox.Password is not bindable).
/// </summary>
public partial class ModelConfigPage : Page
{
    private readonly ModelConfigViewModel _viewModel;








    /// <summary>
    ///     Creates the page and binds the provided view-model.
    /// </summary>
    /// <param name="viewModel">The model configuration view-model.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="viewModel" /> is <c>null</c>.</exception>
    public ModelConfigPage(ModelConfigViewModel? viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        InitializeComponent();
        DataContext = _viewModel;
    }








    /// <summary>
    ///     Copies the PasswordBox value into the bound card's ApiKey property.
    ///     PasswordBox.Password is a security-sensitive, non-bindable property,
    ///     so the transfer happens in code-behind on focus loss.
    /// </summary>
    private void OnApiKeyLostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox passwordBox && passwordBox.Tag is AgentModelCard card)
        {
            card.ApiKey = string.IsNullOrEmpty(passwordBox.Password) ? null : passwordBox.Password;
        }
    }
}
