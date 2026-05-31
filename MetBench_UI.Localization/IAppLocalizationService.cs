using System.Collections.ObjectModel;
using System.Globalization;

namespace MetBench_UI.Localization;

public interface IAppLocalizationService
{
    event EventHandler? CultureChanged;

    CultureInfo CurrentCulture { get; }

    ReadOnlyCollection<AppCultureOption> AvailableCultures { get; }

    void SetCulture(CultureInfo culture);

    string GetString(string key);
}
