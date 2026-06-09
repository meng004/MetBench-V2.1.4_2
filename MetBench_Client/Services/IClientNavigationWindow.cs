using System;

namespace MetBench_Client.Services;

public interface IClientNavigationWindow : IClientWindow
{
    bool Navigate(Type pageType);
}
