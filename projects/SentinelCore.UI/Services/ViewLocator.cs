// Solution: SentinelCore
// Project:   SentinelCore.UI
// File:         ViewLocator.cs
// Author: Kyle L. Crowler
// Build Num:  083003



using System.Windows.Controls;

using Microsoft.Extensions.DependencyInjection;


namespace SentinelCore.UI.Services;


/// <summary>
///     DI-backed implementation of <see cref="IViewLocator" />.
///     Resolves pages from the service provider using a registry of
///     view-model type names to page types, populated during DI configuration.
/// </summary>
public sealed class ViewLocator : IViewLocator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, Type> _pageTypeMap;


    /// <summary>
    ///     Creates a new <see cref="ViewLocator" /> with the given service provider
    ///     and page type registrations.
    /// </summary>
    /// <param name="serviceProvider">The DI service provider.</param>
    /// <param name="pageTypeMap">
    ///     A dictionary mapping view-model type full names to page <see cref="Type" />s.
    /// </param>
    public ViewLocator(IServiceProvider serviceProvider, Dictionary<string, Type> pageTypeMap)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _pageTypeMap = pageTypeMap ?? throw new ArgumentNullException(nameof(pageTypeMap));
    }


    /// <inheritdoc />
    public Page? ResolvePage(string pageKey)
    {
        if (string.IsNullOrWhiteSpace(pageKey) || !_pageTypeMap.TryGetValue(pageKey, out Type? pageType))
        {
            return null;
        }

        object? page = _serviceProvider.GetService(pageType);
        return page as Page;
    }
}
