using Wpf.Ui.Appearance;

namespace MetBench_Client.Services;

public enum ClientTheme
{
    Unknown,
    Light,
    Dark,
}

public interface IClientThemeController
{
    ClientTheme GetCurrentTheme();

    void Apply(ClientTheme theme);
}

public sealed class WpfUiClientThemeController : IClientThemeController
{
    public ClientTheme GetCurrentTheme()
    {
        return FromWpfTheme(ApplicationThemeManager.GetAppTheme());
    }

    public void Apply(ClientTheme theme)
    {
        ApplicationThemeManager.Apply(ToWpfTheme(theme));
    }

    private static ClientTheme FromWpfTheme(ApplicationTheme theme)
    {
        return theme switch
        {
            ApplicationTheme.Light => ClientTheme.Light,
            ApplicationTheme.Dark => ClientTheme.Dark,
            _ => ClientTheme.Unknown,
        };
    }

    private static ApplicationTheme ToWpfTheme(ClientTheme theme)
    {
        return theme switch
        {
            ClientTheme.Dark => ApplicationTheme.Dark,
            _ => ApplicationTheme.Light,
        };
    }
}
