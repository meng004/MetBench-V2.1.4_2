using System.Globalization;
using MetBench_UI.Localization;
using Xunit;

namespace MetBench_Client.Tests.ClientI18n;

/// <summary>
/// Pins the PR-5 explanation / pair-quality resource keys so a missing en-US or
/// zh-CN entry fails the build instead of silently rendering the
/// <c>??key??</c> fallback in the UI.
/// </summary>
public sealed class SystemMtExplanationLocalizationTests
{
    private static readonly string[] RequiredKeys =
    {
        // Shared
        "SystemMt_Explanation_Unavailable",

        // Equation explanation card
        "SystemMt_Equation_ExplanationHeader",
        "SystemMt_Equation_Class",
        "SystemMt_Equation_Family",
        "SystemMt_Equation_PrimaryVariables",
        "SystemMt_Equation_PhysicalMeaning",
        "SystemMt_Equation_BenchmarkRationale",
        "SystemMt_Equation_ExpectedLaws",

        // SUT profile card
        "SystemMt_Sut_ProfileHeader",
        "SystemMt_Sut_ProfileProgramType",
        "SystemMt_Sut_SolverMethod",
        "SystemMt_Sut_RuntimeKey",
        "SystemMt_Sut_InputContract",
        "SystemMt_Sut_OutputContract",
        "SystemMt_Sut_Adapter",
        "SystemMt_Sut_DependencyRisk",

        // MR explanation card
        "SystemMt_Mr_ExplanationHeader",
        "SystemMt_Mr_MetaPatternRationale",
        "SystemMt_Mr_TransformationSemantics",
        "SystemMt_Mr_Observables",
        "SystemMt_Mr_Predicate",
        "SystemMt_Mr_Tolerance",
        "SystemMt_Mr_Applicability",
        "SystemMt_Mr_FailureMeaning",

        // Execution-history pair-quality
        "Evidence_History_PairQualityHeader",
        "Evidence_History_PairQualityCounts_Fmt",
        "Evidence_History_PairQualityRates_Fmt",
        "Evidence_History_PairQualitySkipReasonsHeader",
        "Evidence_History_PairQualityInvalidSpecReasonsHeader",
        "Evidence_History_PairQualityReason_Fmt",
    };

    [Theory]
    [InlineData("en-US")]
    [InlineData("zh-CN")]
    public void All_explanation_keys_resolve_in_culture(string cultureName)
    {
        var localization = new AppLocalizationService();
        localization.SetCulture(new CultureInfo(cultureName));
        var loc = new LocalizedTextProvider(localization);

        foreach (var key in RequiredKeys)
        {
            var value = loc[key];
            Assert.False(
                string.IsNullOrWhiteSpace(value) || value == $"??{key}??",
                $"Missing localized value for '{key}' in culture '{cultureName}'");
        }
    }
}
