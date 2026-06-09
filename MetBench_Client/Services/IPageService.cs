using System;
using System.Windows;

namespace MetBench_Client.Services;

public interface IPageService
{
    T? GetPage<T>()
        where T : class;

    FrameworkElement? GetPage(Type pageType);
}
