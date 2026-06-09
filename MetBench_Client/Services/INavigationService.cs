using System;
using System.Windows.Controls;

namespace MetBench_Client.Services;

public interface INavigationService
{
    void SetNavigationFrame(Frame frame);

    bool Navigate(Type pageType);
}
