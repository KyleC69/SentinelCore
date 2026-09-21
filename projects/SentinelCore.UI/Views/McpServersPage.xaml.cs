// Solution: SentinelCore
// Project:   SentinelCore.UI
// File:         McpServersPage.xaml.cs
// Author: Kyle L. Crowder
// Build Num:  091418



using SentinelCore.UI.ViewModels;




namespace SentinelCore.UI.Views;





/// <summary>
///     Code-behind for the MCP Servers management page.
///     Responsibilities scoped to this file: ViewModel wiring.
/// </summary>
public partial class McpServersPage
{
    /// <summary>
    ///     Creates the page and binds the provided view-model.
    /// </summary>
    /// <param name="viewModel">The MCP servers view-model.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="viewModel" /> is <c>null</c>.</exception>
    public McpServersPage(McpServersViewModel? viewModel)
    {
        McpServersViewModel vm = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        InitializeComponent();
        DataContext = vm;
    }
}
