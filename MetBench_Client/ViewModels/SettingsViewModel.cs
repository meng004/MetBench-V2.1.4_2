using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wpf.Ui.Controls;

namespace MetBench_Client.ViewModels
{
    public partial class SettingsViewModel : ObservableObject, INavigationAware
    {
        private bool _isInitialized = false;

        [ObservableProperty]
        private string _appVersion = "1.0.0.0";

        [ObservableProperty]
        private Wpf.Ui.Appearance.ApplicationTheme _currentApplicationTheme = Wpf.Ui
            .Appearance
            .ApplicationTheme
            .Unknown;

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
            CurrentApplicationTheme = Wpf.Ui.Appearance.ApplicationThemeManager.GetAppTheme();
            //AppVersion = $"Numerical Expression Metamorphic Relations Repository - {GetAssemblyVersion()}";
            AppVersion = $"MetBench: A Numerical Expression Metamorphic Relations Benchmark Dataset - {GetAssemblyVersion()}";


            _isInitialized = true;
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
                    if (CurrentApplicationTheme == Wpf.Ui.Appearance.ApplicationTheme.Light)
                    {
                        break;
                    }

                    Wpf.Ui.Appearance.ApplicationThemeManager.Apply(Wpf.Ui.Appearance.ApplicationTheme.Light);
                    CurrentApplicationTheme = Wpf.Ui.Appearance.ApplicationTheme.Light;

                    break;

                default:
                    if (CurrentApplicationTheme == Wpf.Ui.Appearance.ApplicationTheme.Dark)
                    {
                        break;
                    }

                    Wpf.Ui.Appearance.ApplicationThemeManager.Apply(Wpf.Ui.Appearance.ApplicationTheme.Dark);
                    CurrentApplicationTheme = Wpf.Ui.Appearance.ApplicationTheme.Dark;

                    break;
            }
        }
        //private bool _isInitialized = false;

        //[ObservableProperty]
        //private string _appVersion = String.Empty;

        //[ObservableProperty]
        //private Wpf.Ui.Appearance.ThemeType _currentTheme = Wpf.Ui.Appearance.ThemeType.Unknown;

        //public void OnNavigatedTo()
        //{
        //    if (!_isInitialized)
        //        InitializeViewModel();
        //}

        //public void OnNavigatedFrom()
        //{
        //}

        //private void InitializeViewModel()
        //{
        //    CurrentTheme = Wpf.Ui.Appearance.Theme.GetAppTheme();
        //    AppVersion = $"MR - {GetAssemblyVersion()}";

        //    _isInitialized = true;
        //}

        //private string GetAssemblyVersion()
        //{
        //    return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? String.Empty;
        //}

        //[RelayCommand]
        //private void OnChangeTheme(string parameter)
        //{
        //    switch (parameter)
        //    {
        //        case "theme_dark":
        //            if (CurrentTheme == Wpf.Ui.Appearance.ThemeType.Dark)
        //                break;

        //            Wpf.Ui.Appearance.Theme.Apply(Wpf.Ui.Appearance.ThemeType.Dark);
        //            CurrentTheme = Wpf.Ui.Appearance.ThemeType.Dark;

        //            break;
        //        default:
        //            if (CurrentTheme == Wpf.Ui.Appearance.ThemeType.Light)
        //                break;

        //            Wpf.Ui.Appearance.Theme.Apply(Wpf.Ui.Appearance.ThemeType.Light);
        //            CurrentTheme = Wpf.Ui.Appearance.ThemeType.Light;

        //            break;
        //    }
        //}
    }
}
