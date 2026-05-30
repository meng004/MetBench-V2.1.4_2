using System.Globalization;
using MetBench_UI.Localization;
using Xunit;

namespace MetBench_SystemMT.Tests.ClientI18n;

public sealed class LocalizationAbnormalScenarioTests
{
    [Fact]
    public void Null_or_empty_key_returns_visible_fallback()
    {
        var service = new AppLocalizationService();

        Assert.Equal("??null??", service.GetString(null!));
        Assert.Equal("??empty??", service.GetString(""));
        Assert.Equal("??empty??", service.GetString("   "));
    }

    [Fact]
    public void Neutral_chinese_culture_maps_to_simplified_chinese()
    {
        var service = new AppLocalizationService();

        service.SetCulture(new CultureInfo("zh"));

        Assert.Equal("zh-CN", service.CurrentCulture.Name);
        Assert.Equal("系统级蜕变测试", service.GetString("Nav_SystemMtExecution"));
    }

    [Fact]
    public void Neutral_english_culture_maps_to_english()
    {
        var service = new AppLocalizationService();

        service.SetCulture(new CultureInfo("en"));

        Assert.Equal("en-US", service.CurrentCulture.Name);
        Assert.Equal("System MT", service.GetString("Nav_SystemMtExecution"));
    }
}
