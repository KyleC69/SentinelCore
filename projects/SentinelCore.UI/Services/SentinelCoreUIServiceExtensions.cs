// Solution: SentinelCore
// Project:   SentinelCore.UI
// File:         SentinelCoreUIServiceExtensions.cs
// Author: Kyle L. Crowler
// Build Num:  083003



using System.Windows.Controls;

using Microsoft.Extensions.DependencyInjection;

using SentinelCore.UI.Models;
using SentinelCore.UI.Services;
using SentinelCore.UI.ViewModels;
using SentinelCore.UI.Views;




namespace SentinelCore.UI.Services;


/// <summary>
///     Extension methods for registering all SentinelCore.UI services,
///     view-models, views, and navigation mappings in a single call.
///     This is the single composition point for the UI layer — adding a
///     new page only requires adding entries here and in the page-type map.
/// </summary>
public static class SentinelCoreUIServiceExtensions
{
    /// <summary>
    ///     Registers all UI-layer services, view-models, views, and the
    ///     <see cref="IViewLocator" /> page-type map.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    public static void AddSentinelCoreUI(this IServiceCollection services)
    {
        // Platform services
        services.AddSingleton<IDispatcherService, WpfDispatcherService>();
        services.AddSingleton<IClipboardService, WpfClipboardService>();

        // ViewModels
        services.AddTransient<CoreChatViewModel>();
        services.AddTransient<CaseListViewModel>();
        services.AddTransient<CaseDetailViewModel>();
        services.AddTransient<CreateCaseViewModel>();

        // Views (each page is resolved from DI so its ViewModel is injected)
        services.AddTransient<CoreChatPage>();
        services.AddTransient<CaseListPage>();
        services.AddTransient<CaseDetailPage>();
        services.AddTransient<CreateCasePage>();

        // Navigation — ViewLocator holds the ViewModel→Page type map
        Dictionary<string, Type> pageTypeMap = new()
        {
            [typeof(CoreChatViewModel).FullName!] = typeof(CoreChatPage),
            [typeof(CaseListViewModel).FullName!] = typeof(CaseListPage),
            [typeof(CaseDetailViewModel).FullName!] = typeof(CaseDetailPage),
            [typeof(CreateCaseViewModel).FullName!] = typeof(CreateCasePage)
        };

        services.AddSingleton<IViewLocator>(sp => new ViewLocator(sp, pageTypeMap));
        services.AddSingleton<INavigationService, NavigationService>();
    }
}
