using System.Collections.ObjectModel;
using System.Globalization;
using MetBench_UI.Localization.Resources;

namespace MetBench_UI.Localization;

public sealed class AppLocalizationService : IAppLocalizationService
{
    private static readonly CultureInfo English = new("en-US");
    private static readonly CultureInfo Chinese = new("zh-CN");

    private static readonly ReadOnlyCollection<AppCultureOption> Cultures = new(
        new[]
        {
            new AppCultureOption("English", English),
            new AppCultureOption("中文", Chinese),
        });

    public AppLocalizationService()
    {
        CultureInfo.CurrentUICulture = English;
    }

    public CultureInfo CurrentCulture { get; private set; } = English;

    public event EventHandler? CultureChanged;

    public ReadOnlyCollection<AppCultureOption> AvailableCultures => Cultures;

    public void SetCulture(CultureInfo culture)
    {
        var selected = ResolveCulture(culture);
        if (selected.Name == CurrentCulture.Name)
        {
            return;
        }

        CurrentCulture = selected;
        CultureInfo.CurrentUICulture = selected;
        CultureChanged?.Invoke(this, EventArgs.Empty);
    }

    public string GetString(string key)
    {
        var value = Strings.ResourceManager.GetString(key, CurrentCulture);
        return string.IsNullOrWhiteSpace(value) ? $"??{key}??" : value;
    }

    private static CultureInfo ResolveCulture(CultureInfo culture)
    {
        var exact = Cultures.FirstOrDefault(c => c.Culture.Name == culture.Name)?.Culture;
        if (exact is not null)
        {
            return exact;
        }

        var neutral = Cultures.FirstOrDefault(c => c.Culture.TwoLetterISOLanguageName == culture.TwoLetterISOLanguageName)?.Culture;
        return neutral ?? English;
    }
}
