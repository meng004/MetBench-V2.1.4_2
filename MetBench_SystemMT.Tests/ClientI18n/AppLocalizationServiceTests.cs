using System.Globalization;
using MetBench_UI.Localization;
using Xunit;

namespace MetBench_SystemMT.Tests.ClientI18n;

public sealed class AppLocalizationServiceTests
{
    [Fact]
    public void SetCulture_changes_lookup_language()
    {
        var service = new AppLocalizationService();

        service.SetCulture(new CultureInfo("zh-CN"));
        Assert.Equal("系统级蜕变测试", service.GetString("Nav_SystemMtExecution"));

        service.SetCulture(new CultureInfo("en-US"));
        Assert.Equal("System MT", service.GetString("Nav_SystemMtExecution"));
    }

    [Fact]
    public void Unsupported_culture_falls_back_to_english()
    {
        var service = new AppLocalizationService();

        service.SetCulture(new CultureInfo("fr-FR"));

        Assert.Equal("System MT", service.GetString("Nav_SystemMtExecution"));
        Assert.Equal("en-US", service.CurrentCulture.Name);
    }

    [Fact]
    public void Missing_key_returns_visible_fallback()
    {
        var service = new AppLocalizationService();

        Assert.Equal("??Missing_Key??", service.GetString("Missing_Key"));
    }
}
