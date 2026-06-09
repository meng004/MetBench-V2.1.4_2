using System.Globalization;
using System.Linq;
using MetBench_Client.ViewModels;
using MetBench_UI.Localization;
using Xunit;

namespace MetBench_Client.Tests.ClientI18n;

public sealed class MainWindowLocalizationTests
{
    [WpfFact]
    public void Navigation_labels_refresh_when_culture_changes()
    {
        var localization = new AppLocalizationService();
        // INavigationService is not used in InitializeViewModel; pass null to avoid
        // requiring a full DI container in the test.
        var vm = new MainWindowViewModel(null!, localization, new LocalizedTextProvider(localization));

        localization.SetCulture(new CultureInfo("zh-CN"));
        vm.RefreshLocalizedText();

        var systemMt = vm.NavigationItems
            .Single(item => item.TargetPageType == typeof(MetBench_Client.Views.Pages.SystemMtExecutionPage));
        var settings = vm.NavigationFooter.Single();

        Assert.Equal("系统级蜕变测试", systemMt.Content);
        Assert.Equal("设置", settings.Content);

        localization.SetCulture(new CultureInfo("en-US"));
        vm.RefreshLocalizedText();

        Assert.Equal("System MT", systemMt.Content);
        Assert.Equal("Settings", settings.Content);
    }
}
