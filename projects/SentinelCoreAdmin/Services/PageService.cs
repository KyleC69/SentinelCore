// Solution: SentinelCore
// Project:   SentinelCoreAdmin
// File:         PageService.cs
// Author: Kyle L. Crowder
// Build Num:  082808



using System.Windows.Controls;

using CommunityToolkit.Mvvm.ComponentModel;

using JetBrains.Annotations;

using SentinelCoreAdmin.Contracts.Services;
using SentinelCoreAdmin.ViewModels;
using SentinelCoreAdmin.Views;




namespace SentinelCoreAdmin.Services;





public class PageService : IPageService
{
    private readonly Dictionary<string, Type> _pages = new Dictionary<string, Type>();
    private readonly IServiceProvider _serviceProvider;








    public PageService([CanBeNull] IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        Configure<CoreChatViewModel, CoreChatPage>();
        Configure<TraceLogViewModel, TraceLogPage>();
        Configure<CaseListViewModel, CaseListPage>();
        Configure<CreateCaseViewModel, CreateCasePage>();
        Configure<CaseDetailViewModel, CaseDetailPage>();
        Configure<SettingsViewModel, SettingsPage>();
    }








    [CanBeNull]
    public Page? GetPage([NotNull] string key)
    {
        Type pageType = GetPageType(key);
        return _serviceProvider.GetService(pageType) as Page;
    }








    [CanBeNull]
    public Type GetPageType([NotNull] string key)
    {
        Type? pageType;
        lock (_pages)
        {
            if (!_pages.TryGetValue(key, out pageType))
            {
                throw new ArgumentException($"Page not found: {key}. Did you forget to call PageService.Configure?");
            }
        }

        return pageType;
    }








    private void Configure<VM, V>() where VM : ObservableObject where V : Page
    {
        lock (_pages)
        {
            string key = typeof(VM).FullName!;
            if (_pages.ContainsKey(key))
            {
                throw new ArgumentException($"The key {key} is already configured in PageService");
            }

            Type type = typeof(V);
            if (_pages.Any(p => p.Value == type))
            {
                throw new ArgumentException($"This type is already configured with key {_pages.First(p => p.Value == type).Key}");
            }

            _pages.Add(key, type);
        }
    }
}