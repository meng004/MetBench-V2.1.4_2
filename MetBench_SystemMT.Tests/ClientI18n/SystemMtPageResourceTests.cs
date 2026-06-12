using System.Globalization;
using System.Resources;
using Xunit;

namespace MetBench_SystemMT.Tests.ClientI18n;

public sealed class SystemMtPageResourceTests
{
    [Fact]
    public void System_mt_page_resource_keys_exist_in_english_and_chinese()
    {
        var manager = new ResourceManager(
            "MetBench_UI.Localization.Resources.Strings",
            typeof(MetBench_UI.Localization.AppLocalizationService).Assembly);

        var keys = new[]
        {
            "SystemMt_Run","SystemMt_SelectedMr","SystemMt_Source","SystemMt_FollowUp","SystemMt_Result",
            "Catalog_LoadedManifests","Catalog_LoadedSuts","Catalog_LoadedEquations","Catalog_LoadedSampleCases",
            "History_ExecutionHistory","Anomaly_Title","Anomaly_ApplyTransition","Replay_Title",
            "ReportGenerator_Title","ReportGenerator_Export",
            "Nav_SystemMtRuntimeEnvironments","RuntimeEnv_Title","RuntimeEnv_Subtitle",
            "RuntimeEnv_RuntimeKey","RuntimeEnv_Endpoint","RuntimeEnv_Image","RuntimeEnv_Python",
            "RuntimeEnv_AuthTokenEnv","RuntimeEnv_Status_Saved_Fmt","RuntimeEnv_Status_HealthFailed_Fmt"
        };

        foreach (var key in keys)
        {
            Assert.False(string.IsNullOrWhiteSpace(manager.GetString(key, new CultureInfo("en-US"))), key);
            Assert.False(string.IsNullOrWhiteSpace(manager.GetString(key, new CultureInfo("zh-CN"))), key);
        }
    }
}
