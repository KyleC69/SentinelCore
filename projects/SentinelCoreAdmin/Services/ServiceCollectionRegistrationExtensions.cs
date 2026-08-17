// Solution: SentinelCore
// Project:   SentinelCoreAdmin
// File:         ServiceCollectionRegistrationExtensions.cs
// Author: Kyle L. Crowder
// Build Num:  081602



using JetBrains.Annotations;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using SentinelCore.Abstractions;
using SentinelCore.Cfe;
using SentinelCore.Contracts;
using SentinelCore.Events;
using SentinelCore.Infrastructure.DependencyInjection;

using SentinelCoreAdmin.Activation;
using SentinelCoreAdmin.Contracts.Activation;
using SentinelCoreAdmin.Contracts.Services;
using SentinelCoreAdmin.Contracts.Views;
using SentinelCoreAdmin.Core.Contracts.Services;
using SentinelCoreAdmin.Core.Services;
using SentinelCoreAdmin.ViewModels;
using SentinelCoreAdmin.Views;




namespace SentinelCoreAdmin.Services;





/// <summary>
///     Extension methods for registering services by category/area of concern.
///     Each method groups related registrations to keep
///     <see cref="App.ConfigureServices" /> clean and discoverable.
/// </summary>
public static class ServiceCollectionRegistrationExtensions
{
    /// <summary>
    ///     Registers the application host service and toast activation handler.
    /// </summary>
    public static IServiceCollection AddAppHostModule([NotNull] this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHostedService<ApplicationHostService>();
        services.AddSingleton<IActivationHandler, ToastNotificationActivationHandler>();

        return services;
    }








    /// <summary>
    ///     Registers core infrastructure services (file, system, persistence, theme).
    /// </summary>
    public static IServiceCollection AddCoreServicesModule([NotNull] this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IFileService, FileService>();
        services.AddSingleton<ISystemService, SystemService>();
        services.AddSingleton<IApplicationInfoService, ApplicationInfoService>();
        services.AddSingleton<IPersistAndRestoreService, PersistAndRestoreService>();
        services.AddSingleton<IThemeSelectorService, ThemeSelectorService>();

        return services;
    }








    /// <summary>
    ///     Registers identity and Microsoft Graph services.
    /// </summary>
    public static IServiceCollection AddIdentityModule([NotNull] this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IIdentityCacheService, IdentityCacheService>();
        services.AddSingleton<IMicrosoftGraphService, MicrosoftGraphService>();
        services.AddSingleton<IIdentityService, IdentityService>();
        services.AddHttpClient("msgraph", client => { client.BaseAddress = new Uri("https://graph.microsoft.com/v1.0/"); });

        return services;
    }








    /// <summary>
    ///     Registers navigation and window management services.
    /// </summary>
    public static IServiceCollection AddNavigationModule([NotNull] this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IPageService, PageService>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IRightPaneService, RightPaneService>();
        services.AddSingleton<IWindowManagerService, WindowManagerService>();

        return services;
    }








    /// <summary>
    ///     Registers toast notification services.
    /// </summary>
    public static IServiceCollection AddNotificationsModule([NotNull] this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IToastNotificationsService, ToastNotificationsService>();

        return services;
    }








    /// <summary>
    ///     Registers SentinelCore orchestration, events, and case-flow services.
    /// </summary>
    public static IServiceCollection AddSentinelCoreModule([NotNull] this IServiceCollection services, [NotNull] SentinelCoreSettings settings)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(settings);

        services.AddSentinelCore(settings);

        return services;
    }








    /// <summary>
    ///     Registers all views and their corresponding view-models.
    /// </summary>
    public static IServiceCollection AddViewsAndViewModelsModule([NotNull] this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Shell (main window)
        services.AddTransient<IShellWindow, ShellWindow>();
        services.AddTransient<ShellViewModel>();

        // Chat page — CoreChatViewModel requires the app shutdown token so
        // in-flight orchestrations are cancelled when the application exits.
        services.AddTransient<CoreChatPage>();
        services.AddTransient(sp =>
        {
            App app = (App)App.Current;
            return new CoreChatViewModel(sp.GetRequiredService<IOrchestrationControl>(), sp.GetRequiredService<ISentinelCoreEvents>(), sp.GetRequiredService<ICaseFlowEngine>(), sp.GetRequiredService<ILogger<CoreChatViewModel>>(), app.ShutdownToken);
        });

        // Trace log page
        services.AddTransient<TraceLogViewModel>();
        services.AddTransient<TraceLogPage>();

        // Case management pages
        services.AddTransient<CaseListViewModel>();
        services.AddTransient<CaseListPage>();
        services.AddTransient<CreateCaseViewModel>();
        services.AddTransient<CreateCasePage>();
        services.AddTransient<CaseDetailViewModel>();
        services.AddTransient<CaseDetailPage>();

        // Settings page
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<SettingsPage>();

        // Login dialog
        services.AddTransient<IShellDialogWindow, ShellDialogWindow>();
        services.AddTransient<ShellDialogViewModel>();
        services.AddSingleton<IUserDataService, UserDataService>();
        services.AddTransient<ILogInWindow, LogInWindow>();
        services.AddTransient<LogInViewModel>();

        return services;
    }
}