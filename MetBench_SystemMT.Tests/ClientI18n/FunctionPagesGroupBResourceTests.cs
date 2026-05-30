using System.Globalization;
using System.Resources;
using Xunit;

namespace MetBench_SystemMT.Tests.ClientI18n;

public sealed class FunctionPagesGroupBResourceTests
{
    [Fact]
    public void Function_page_group_b_resource_keys_exist_in_english_and_chinese()
    {
        var manager = new ResourceManager(
            "MetBench_UI.Localization.Resources.Strings",
            typeof(MetBench_UI.Localization.AppLocalizationService).Assembly);

        var keys = new[]
        {
            "Dashboard_ClickMe",
            "Discovery_DiscovererLabel","Discovery_TargetSutLabel","Discovery_RunButton","Discovery_ColCode",
            "Discovery_ColAssertion","Discovery_ColValue","Discovery_ColConfidence","Discovery_ColStatus",
            "Discovery_ColCreated",
            "Candidate_SelectCandidate","Candidate_Confidence","Candidate_Assertion","Candidate_Rationale",
            "Candidate_Validators","Candidate_Empirical","Candidate_TheoreticalLlm","Candidate_ValidateSelected",
            "Candidate_ColValidator","Candidate_ColPassed","Candidate_ColRunAt","Candidate_ColDetails",
            "Coverage_Title","Coverage_MetaPatternCoverage","Coverage_SutMrCoverage","Coverage_BugCoverage",
            "Coverage_MutationCoverage",
            "Mutation_CampaignName","Mutation_SeedDemoData","Mutation_SeedDemoDataToolTip","Mutation_Mutants",
            "Mutation_MRBindings","Mutation_StartCampaign","Mutation_CampaignSummary","Mutation_SummaryTotal",
            "Mutation_SummaryDetected","Mutation_SummaryMissed","Mutation_SummaryErrors","Mutation_SummaryDetectionRate",
            "Mutation_ColMutant","Mutation_ColBinding","Mutation_ColOutcome","Mutation_ColObservedDelta",
            "Mutation_ColExpectedDelta","Mutation_ColNotes",
            "Detect_ApplicationName","Detect_InputPattern","Detect_OutputPattern","Detect_DimensionOfInputPattern",
            "Detect_TrueRatio","Detect_Program","Detect_WithdrawUpload","Detect_Upload","Detect_InputParamCount",
            "Detect_Reset","Detect_InputParamRanges","Detect_InputDatatypes","Detect_DetectMR","Detect_StoreMR",
            "Detect_ExportCsv",
            "Recommend_Application","Recommend_Similarity","Recommend_InputPattern","Recommend_OutputPattern",
            "Recommend_MrInformation","Recommend_View","Recommend_ApplicationInformation","Recommend_SupportRate",
            "Recommend_RecommendType","Recommend_Recommend","Recommend_Clear","Recommend_Upload",
            "Recommend_InputParamCount","Recommend_Reset","Recommend_InputParamRanges","Recommend_InputDatatypes",
            "MtExec_InputPattern","MtExec_OutputPattern","MtExec_CodeName","MtExec_ExecuteMt","MtExec_MinNumber",
            "MtExec_Reset","MtExec_MaxNumber","MtExec_ExecuteNumb","MtExec_Threshold","MtExec_MtReport",
            "MtExec_Pass","MtExec_Fail","MtExec_PlotType"
        };

        foreach (var key in keys)
        {
            Assert.False(string.IsNullOrWhiteSpace(manager.GetString(key, new CultureInfo("en-US"))), key);
            Assert.False(string.IsNullOrWhiteSpace(manager.GetString(key, new CultureInfo("zh-CN"))), key);
        }
    }
}
