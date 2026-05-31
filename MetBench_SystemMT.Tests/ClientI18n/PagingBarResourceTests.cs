using System.Globalization;
using System.Resources;
using Xunit;

namespace MetBench_SystemMT.Tests.ClientI18n;

public sealed class PagingBarResourceTests
{
    [Fact]
    public void PagingBar_resource_keys_exist_in_english_and_chinese()
    {
        var manager = new ResourceManager(
            "MetBench_UI.Localization.Resources.Strings",
            typeof(MetBench_UI.Localization.AppLocalizationService).Assembly);

        var keys = new[]
        {
            "PagingBar_Refresh",
            "PagingBar_RefreshTooltip",
            "PagingBar_FirstPage",
            "PagingBar_PreviousPage",
            "PagingBar_NextPage",
            "PagingBar_LastPage",
            "PagingBar_PageSize",
            "PagingBar_Page",
            "PagingBar_TotalSuffix",
        };

        foreach (var key in keys)
        {
            Assert.False(string.IsNullOrWhiteSpace(manager.GetString(key, new CultureInfo("en-US"))), key);
            Assert.False(string.IsNullOrWhiteSpace(manager.GetString(key, new CultureInfo("zh-CN"))), key);
        }
    }
}
