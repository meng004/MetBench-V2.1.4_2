using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace MetBench_Client.Services;

public sealed class ClientNavigationService : INavigationService
{
    private readonly IPageService _pageService;
    private Frame? _frame;
    private FrameworkElement? _currentPage;

    public ClientNavigationService(IPageService pageService)
    {
        _pageService = pageService;
    }

    public void SetNavigationFrame(Frame frame)
    {
        _frame = frame;
    }

    public bool Navigate(Type pageType)
    {
        if (_frame is null)
        {
            return false;
        }

        var page = _pageService.GetPage(pageType);
        if (page is null)
        {
            return false;
        }

        NotifyNavigatedFrom(_currentPage);
        _frame.Content = page;
        _currentPage = page;
        NotifyNavigatedTo(page);
        return true;
    }

    private static void NotifyNavigatedTo(FrameworkElement? element)
    {
        GetNavigationAware(element)?.OnNavigatedTo();
    }

    private static void NotifyNavigatedFrom(FrameworkElement? element)
    {
        GetNavigationAware(element)?.OnNavigatedFrom();
    }

    private static INavigationAware? GetNavigationAware(FrameworkElement? element)
    {
        if (element is null)
        {
            return null;
        }

        if (element is INavigationAware directAware)
        {
            return directAware;
        }

        if (element.DataContext is INavigationAware dataContextAware)
        {
            return dataContextAware;
        }

        var viewModelProperty = element.GetType().GetProperty(
            "ViewModel",
            BindingFlags.Instance | BindingFlags.Public);
        return viewModelProperty?.GetValue(element) as INavigationAware;
    }
}
