using System.Globalization;
using System.Linq;
using MetBench_UI.Localization;
using MetBench_Client.ViewModels;
using Wpf.Ui;
using Xunit;

namespace MetBench_Client.Tests.ClientI18n;

public sealed class SettingsLanguageTests
{
    [WpfFact]
    public void Settings_exposes_english_and_chinese_options()
    {
        var localization = new AppLocalizationService();
        var vm = new SettingsViewModel(localization, new LocalizedTextProvider(localization));

        vm.OnNavigatedTo();

        Assert.Contains(vm.AvailableCultures, c => c.Culture.Name == "en-US");
        Assert.Contains(vm.AvailableCultures, c => c.Culture.Name == "zh-CN");
    }

    [WpfFact]
    public void Changing_selected_culture_updates_localization_service()
    {
        var localization = new AppLocalizationService();
        var vm = new SettingsViewModel(localization, new LocalizedTextProvider(localization));

        vm.ChangeCultureCommand.Execute("zh-CN");

        Assert.Equal("zh-CN", localization.CurrentCulture.Name);

        vm.ChangeCultureCommand.Execute("fr-FR");

        Assert.Equal("en-US", localization.CurrentCulture.Name);
    }
}
