using System.Windows;
using System.Windows.Media;

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

public sealed class NativeClientThemeController : IClientThemeController
{
    private const string ThemeKey = "MetBenchClientTheme";

    public ClientTheme GetCurrentTheme()
    {
        return (Application.Current?.Resources[ThemeKey] as string) switch
        {
            "Dark" => ClientTheme.Dark,
            "Light" => ClientTheme.Light,
            _ => ClientTheme.Light,
        };
    }

    public void Apply(ClientTheme theme)
    {
        var normalized = theme == ClientTheme.Dark ? ClientTheme.Dark : ClientTheme.Light;
        var resources = Application.Current?.Resources;
        if (resources is null)
        {
            return;
        }

        resources[ThemeKey] = normalized.ToString();
        if (normalized == ClientTheme.Dark)
        {
            resources["ApplicationBackgroundBrush"] = Brush("#111827");
            resources["TextFillColorPrimaryBrush"] = Brush("#F9FAFB");
            resources["TextFillColorSecondaryBrush"] = Brush("#CBD5E1");
            resources["TextFillColorDisabledBrush"] = Brush("#64748B");
            resources["ControlStrokeColorDefaultBrush"] = Brush("#334155");
            resources["SubtleFillColorSecondaryBrush"] = Brush("#1F2937");
            resources["SubtleFillColorTertiaryBrush"] = Brush("#273244");
            resources["SystemFillColorCriticalBrush"] = Brush("#F97316");
            return;
        }

        resources["ApplicationBackgroundBrush"] = Brush("#F7F9FC");
        resources["TextFillColorPrimaryBrush"] = Brush("#1F2937");
        resources["TextFillColorSecondaryBrush"] = Brush("#5F6B7A");
        resources["TextFillColorDisabledBrush"] = Brush("#9AA4B2");
        resources["ControlStrokeColorDefaultBrush"] = Brush("#D7DEE8");
        resources["SubtleFillColorSecondaryBrush"] = Brush("#EEF2F7");
        resources["SubtleFillColorTertiaryBrush"] = Brush("#E4EAF2");
        resources["SystemFillColorCriticalBrush"] = Brush("#C2410C");
    }

    private static SolidColorBrush Brush(string color) => new((Color)ColorConverter.ConvertFromString(color));
}
