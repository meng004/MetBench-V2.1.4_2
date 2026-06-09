using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace MetBench_Client.Models;

public sealed partial class NavigationItem : ObservableObject
{
    public NavigationItem(string key, string content, Type targetPageType)
    {
        Key = key;
        Content = content;
        TargetPageType = targetPageType;
    }

    public string Key
    {
        get;
    }

    [ObservableProperty]
    private string _content = string.Empty;

    public Type TargetPageType
    {
        get;
    }
}
