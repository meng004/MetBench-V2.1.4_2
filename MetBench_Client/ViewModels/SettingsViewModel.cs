using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MetBench_Client.Services;
using MetBench_UI.Localization;

namespace MetBench_Client.ViewModels
{
    public partial class SettingsViewModel : ObservableObject, INavigationAware
    {
        private readonly IAppLocalizationService _localization;
        private readonly IClientThemeController _themeController;

        public LocalizedTextProvider Localization { get; }

        [ObservableProperty]
        private IReadOnlyList<AppCultureOption> _availableCultures = Array.Empty<AppCultureOption>();

        [ObservableProperty]
        private AppCultureOption? _selectedCulture;

        private bool _isInitialized = false;

        [ObservableProperty]
        private string _appVersion = "1.0.0.0";

        [ObservableProperty]
        private ClientTheme _currentApplicationTheme = ClientTheme.Unknown;

        public SettingsViewModel(
            IAppLocalizationService localization,
            LocalizedTextProvider localizedText,
            IClientThemeController themeController)
        {
            _localization = localization;
            Localization = localizedText;
            _themeController = themeController;
        }

        //public String AppVersion { get; set; } = "1.0.0.0";
        public void OnNavigatedTo()
        {
            if (!_isInitialized)
            {
                InitializeViewModel();
            }
        }

        public void OnNavigatedFrom() { }

        private void InitializeViewModel()
        {
            CurrentApplicationTheme = _themeController.GetCurrentTheme();
            //AppVersion = $"Numerical Expression Metamorphic Relations Repository - {GetAssemblyVersion()}";
            AppVersion = $"MetBench: A Numerical Expression Metamorphic Relations Benchmark Dataset - {GetAssemblyVersion()}";

            AvailableCultures = _localization.AvailableCultures;
            SelectedCulture = AvailableCultures.FirstOrDefault(c => c.Culture.Name == _localization.CurrentCulture.Name);

            _isInitialized = true;
        }

        [RelayCommand]
        private void OnChangeCulture(string cultureName)
        {
            if (string.IsNullOrWhiteSpace(cultureName)) return;
            _localization.SetCulture(new CultureInfo(cultureName));
            SelectedCulture = AvailableCultures.FirstOrDefault(c => c.Culture.Name == _localization.CurrentCulture.Name);
        }

        private static string GetAssemblyVersion()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString()
                ?? string.Empty;
        }

        [RelayCommand]
        private void OnChangeTheme(string parameter)
        {
            switch (parameter)
            {
                case "theme_light":
                    if (CurrentApplicationTheme == ClientTheme.Light)
                    {
                        break;
                    }

                    _themeController.Apply(ClientTheme.Light);
                    CurrentApplicationTheme = ClientTheme.Light;

                    break;

                default:
                    if (CurrentApplicationTheme == ClientTheme.Dark)
                    {
                        break;
                    }

                    _themeController.Apply(ClientTheme.Dark);
                    CurrentApplicationTheme = ClientTheme.Dark;

                    break;
            }
        }
    }
}
