using System.Globalization;
using System.Resources;
using Xunit;

namespace MetBench_SystemMT.Tests.ClientI18n;

public sealed class SystemMtPageFullI18nResourceTests
{
    [Fact]
    public void Full_page_system_mt_resource_keys_exist_in_english_and_chinese()
    {
        var manager = new ResourceManager(
            "MetBench_UI.Localization.Resources.Strings",
            typeof(MetBench_UI.Localization.AppLocalizationService).Assembly);

        var keys = new[]
        {
            // Execution page (SystemMt_*)
            "SystemMt_PageTitle",
            "SystemMt_PageSubtitle",
            "SystemMt_Scenario",
            "SystemMt_FactorParameter",
            "SystemMt_FactorParameterPlaceholder",
            "SystemMt_RefreshRecent",
            "SystemMt_LastResult",
            "SystemMt_Status",
            "SystemMt_RunAt",
            "SystemMt_Assertion",
            "SystemMt_Value",
            "SystemMt_Passed",
            "SystemMt_ExportHtmlReport",

            // Cross-page shared catalog keys
            "Catalog_Reload",
            "Catalog_Validate",
            "Catalog_EquationKey",

            // MR Catalog (Catalog_*)
            "Catalog_MrCatalogTitle",
            "Catalog_MrCatalogSubtitle",
            "Catalog_Manifest",
            "Catalog_NewMrDraft",
            "Catalog_MrId",
            "Catalog_Assertion",
            "Catalog_Value",
            "Catalog_DisplayName",
            "Catalog_Transformation",
            "Catalog_AssertionName",
            "Catalog_ValueName",
            "Catalog_MetaPattern",
            "Catalog_SampleCase",
            "Catalog_WorkRoot",
            "Catalog_TimeoutSeconds",
            "Catalog_Factor",
            "Catalog_TransformStep",
            "Catalog_TargetFieldPath",

            // SUT Catalog
            "Catalog_SutCatalogTitle",
            "Catalog_SutCatalogSubtitle",
            "Catalog_NewSutDraft",
            "Catalog_SutId",
            "Catalog_Equation",
            "Catalog_Program",
            "Catalog_SutName",
            "Catalog_ProgramName",
            "Catalog_ProgramType",
            "Catalog_PythonRuntimeKind",
            "Catalog_RunnerScript",
            "Catalog_InputParserScript",
            "Catalog_OutputParserScript",
            "Catalog_InputAdapterScript",
            "Catalog_OutputAdapterScript",

            // Equation Catalog
            "Catalog_EquationCatalogTitle",
            "Catalog_EquationCatalogSubtitle",
            "Catalog_NewEquation",
            "Catalog_Key",
            "Catalog_CanonicalForm",
            "Catalog_Source",
            "Catalog_SymbolSystem",

            // Sample Case Catalog
            "Catalog_SampleCatalogTitle",
            "Catalog_SampleCatalogSubtitle",
            "Catalog_SutLabel",
            "Catalog_NewSample",
            "Catalog_FileName",
        };

        foreach (var key in keys)
        {
            Assert.False(string.IsNullOrWhiteSpace(manager.GetString(key, new CultureInfo("en-US"))), key);
            Assert.False(string.IsNullOrWhiteSpace(manager.GetString(key, new CultureInfo("zh-CN"))), key);
        }
    }
}
