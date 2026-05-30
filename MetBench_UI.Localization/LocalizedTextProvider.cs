using System.ComponentModel;

namespace MetBench_UI.Localization;

public sealed class LocalizedTextProvider : INotifyPropertyChanged
{
    private readonly IAppLocalizationService _localization;

    public LocalizedTextProvider(IAppLocalizationService localization)
    {
        _localization = localization;
        _localization.CultureChanged += (_, _) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string this[string key] => _localization.GetString(key);
}
