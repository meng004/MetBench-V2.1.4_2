using System.Globalization;
using System.Linq;
using System.Windows;
using MetBench_Client.Services;
using MetBench_UI.Localization;
using MetBench_Client.ViewModels;
using MetBench_Client.Views.Pages;
using Xunit;

namespace MetBench_Client.Tests.ClientI18n;

public sealed class SettingsLanguageTests
{
    [WpfFact]
    public void Settings_exposes_english_and_chinese_options()
    {
        var localization = new AppLocalizationService();
        var vm = CreateViewModel(localization);

        vm.OnNavigatedTo();

        Assert.Contains(vm.AvailableCultures, c => c.Culture.Name == "en-US");
        Assert.Contains(vm.AvailableCultures, c => c.Culture.Name == "zh-CN");
    }

    [WpfFact]
    public void Changing_selected_culture_updates_localization_service()
    {
        var localization = new AppLocalizationService();
        var vm = CreateViewModel(localization);

        vm.ChangeCultureCommand.Execute("zh-CN");

        Assert.Equal("zh-CN", localization.CurrentCulture.Name);

        vm.ChangeCultureCommand.Execute("fr-FR");

        Assert.Equal("en-US", localization.CurrentCulture.Name);
    }

    [WpfFact]
    public void ChangeCulture_with_null_or_blank_does_not_throw()
    {
        var localization = new AppLocalizationService();
        var vm = CreateViewModel(localization);
        vm.ChangeCultureCommand.Execute(null);
        vm.ChangeCultureCommand.Execute("");
        Assert.Equal("en-US", localization.CurrentCulture.Name); // unchanged
    }

    [WpfFact]
    public void ChangeTheme_uses_client_theme_controller()
    {
        var localization = new AppLocalizationService();
        var themeController = new FakeThemeController(ClientTheme.Light);
        var vm = CreateViewModel(localization, themeController);

        vm.OnNavigatedTo();
        Assert.Equal(ClientTheme.Light, vm.CurrentApplicationTheme);

        vm.ChangeThemeCommand.Execute("theme_dark");

        Assert.Equal(ClientTheme.Dark, vm.CurrentApplicationTheme);
        Assert.Equal(ClientTheme.Dark, themeController.LastAppliedTheme);
    }

    [WpfFact]
    public void Settings_page_loaded_initializes_view_model_without_wpf_ui_navigable_view()
    {
        var localization = new AppLocalizationService();
        var themeController = new FakeThemeController(ClientTheme.Light);
        var vm = CreateViewModel(localization, themeController);
        var page = new SettingsPage(vm);

        Assert.Same(page, page.DataContext);
        Assert.Empty(vm.AvailableCultures);
        Assert.Equal(ClientTheme.Unknown, vm.CurrentApplicationTheme);

        page.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

        Assert.Contains(vm.AvailableCultures, c => c.Culture.Name == "en-US");
        Assert.Contains(vm.AvailableCultures, c => c.Culture.Name == "zh-CN");
        Assert.Equal(ClientTheme.Light, vm.CurrentApplicationTheme);
        Assert.StartsWith("MetBench: A Numerical Expression Metamorphic Relations Benchmark Dataset - ", vm.AppVersion);
    }

    private static SettingsViewModel CreateViewModel(
        AppLocalizationService localization,
        IClientThemeController? themeController = null)
    {
        return new SettingsViewModel(
            localization,
            new LocalizedTextProvider(localization),
            themeController ?? new FakeThemeController(ClientTheme.Light));
    }

    private sealed class FakeThemeController : IClientThemeController
    {
        private readonly ClientTheme _currentTheme;

        public FakeThemeController(ClientTheme currentTheme)
        {
            _currentTheme = currentTheme;
        }

        public ClientTheme? LastAppliedTheme { get; private set; }

        public ClientTheme GetCurrentTheme()
        {
            return _currentTheme;
        }

        public void Apply(ClientTheme theme)
        {
            LastAppliedTheme = theme;
        }
    }
}
