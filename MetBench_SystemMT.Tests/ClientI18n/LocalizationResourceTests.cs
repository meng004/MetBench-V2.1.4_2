using System.Globalization;
using System.Linq;
using System.Resources;
using MetBench_UI.Localization;
using Xunit;

namespace MetBench_SystemMT.Tests.ClientI18n;

public sealed class LocalizationResourceTests
{
    [Fact]
    public void English_and_chinese_resources_contain_required_shell_keys()
    {
        var manager = new ResourceManager(
            "MetBench_UI.Localization.Resources.Strings",
            typeof(AppLocalizationService).Assembly);

        var keys = new[]
        {
            "App_Title",
            "Nav_SystemMtExecution",
            "Nav_Settings",
            "Settings_Personalization",
            "Settings_Language",
            "Settings_Language_English",
            "Settings_Language_Chinese",
            "Common_Search",
            "Common_NotAvailable"
        };

        foreach (var key in keys)
        {
            Assert.False(string.IsNullOrWhiteSpace(manager.GetString(key, new CultureInfo("en-US"))));
            Assert.False(string.IsNullOrWhiteSpace(manager.GetString(key, new CultureInfo("zh-CN"))));
        }
    }

    [Fact]
    public void Localization_core_has_no_ui_framework_references()
    {
        var referenced = typeof(AppLocalizationService).Assembly
            .GetReferencedAssemblies()
            .Select(assembly => assembly.Name)
            .ToArray();

        Assert.DoesNotContain("PresentationFramework", referenced);
        Assert.DoesNotContain("WindowsBase", referenced);
        Assert.DoesNotContain("Wpf.Ui", referenced);
        Assert.DoesNotContain("Avalonia", referenced);
    }
}
