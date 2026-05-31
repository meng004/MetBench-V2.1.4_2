using System.Globalization;
using System.Resources;
using Xunit;

namespace MetBench_SystemMT.Tests.ClientI18n;

public sealed class LegacyPageResourceTests
{
    [Fact]
    public void Legacy_page_resource_keys_exist_in_english_and_chinese()
    {
        var manager = new ResourceManager(
            "MetBench_UI.Localization.Resources.Strings",
            typeof(MetBench_UI.Localization.AppLocalizationService).Assembly);

        var keys = new[]
        {
            "Legacy_Add","Legacy_Delete","Legacy_Modify","Legacy_Query","Legacy_Save","Legacy_Cancel",
            "Legacy_Name","Legacy_Description","Legacy_Domain","Legacy_Application",
            "Legacy_Recommendation","Legacy_Detection","Legacy_Coverage"
        };

        foreach (var key in keys)
        {
            Assert.False(string.IsNullOrWhiteSpace(manager.GetString(key, new CultureInfo("en-US"))), key);
            Assert.False(string.IsNullOrWhiteSpace(manager.GetString(key, new CultureInfo("zh-CN"))), key);
        }
    }
}
