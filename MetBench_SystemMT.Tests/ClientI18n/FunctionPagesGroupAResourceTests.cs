using System.Globalization;
using System.Resources;
using Xunit;

namespace MetBench_SystemMT.Tests.ClientI18n;

public sealed class FunctionPagesGroupAResourceTests
{
    [Fact]
    public void Function_page_group_a_resource_keys_exist_in_english_and_chinese()
    {
        var manager = new ResourceManager(
            "MetBench_UI.Localization.Resources.Strings",
            typeof(MetBench_UI.Localization.AppLocalizationService).Assembly);

        var keys = new[]
        {
            "Anomaly_SeverityFilterLabel","Anomaly_StatusFilterLabel","Anomaly_AnalyzeCommonality","Anomaly_SweepOrphans",
            "Anomaly_SweepOrphansTooltip","Anomaly_ColumnId","Anomaly_ColumnSeverity","Anomaly_ColumnStatus",
            "Anomaly_ColumnCategory","Anomaly_ColumnDiscovered","Anomaly_ColumnLinkedBug","Anomaly_ColumnReplayCount",
            "Anomaly_ColumnNotes","Anomaly_TransitionSelectedTo","Anomaly_ReplayThisAnomaly","Anomaly_CommonalityReport",
            "Anomaly_CommonalityTotal","Anomaly_CommonalityDominantSeverity","Anomaly_CommonalityDominantCategory","Anomaly_CommonalityLinkedToKnownBug",
            "AppMgmt_ApplicationName","AppMgmt_Number","AppMgmt_ProgrammingLanguage","AppMgmt_LineOfCode",
            "AppMgmt_SoftwareUnderTest","AppMgmt_SourceTestCase","AppMgmt_DOI","AppMgmt_Url",
            "AppMgmt_IdApplication","AppMgmt_DomainName","AppMgmt_ClearSelection","AppMgmt_Upload",
            "AppMgmt_Unzip","AppMgmt_InputParams","AppMgmt_OutputParams","DomainMgmt_DomainName",
            "DomainMgmt_ColumnNumber","DomainMgmt_IdDomain","MrDisplay_ColumnNumber","MrDisplay_ColumnContext",
            "MrDisplay_ColumnArityOfMR","MrDisplay_ColumnInputPattern","MrDisplay_ColumnOutputPattern","MrDisplay_ColumnApplicationName",
            "MrDisplay_ColumnCodeName","MrDisplay_ColumnDomainName","MrDisplay_ColumnOperation","MrDisplay_FilterArityOfMR",
            "MrDisplay_FilterDimensionOfInputPattern","MrDisplay_FilterDimensionOfOutputPattern","MrDisplay_FilterApplicationName","MrDisplay_FilterDomainName",
            "MrMgmt_ApplicationName","MrMgmt_StatusFilterLabel","MrMgmt_StatusFilterTooltip","MrMgmt_ColNumber",
            "MrMgmt_Context","MrMgmt_ArityOfMR","MrMgmt_InputPattern","MrMgmt_OutputPattern",
            "MrMgmt_DomainName","MrMgmt_Status","MrMgmt_Operation","MrMgmt_ExecuteMT",
            "MrMgmt_IdMR","MrMgmt_Granularity","MrMgmt_Hierarchy","MrMgmt_DimensionOfInputPattern",
            "MrMgmt_DimensionOfOutputPattern","MrMgmt_Operator","MrMgmt_Expression","Report_ReportType",
            "MetaPattern_Title","MetaPattern_ToggleStatus","MetaPattern_SoftDelete","MetaPattern_ColumnCode",
            "MetaPattern_ColumnBlock","MetaPattern_ColumnDefaultAssertion","MetaPattern_ColumnTolRel","MetaPattern_ColumnNoise",
            "MetaPattern_ColumnStatus","MetaPattern_ColumnOutOfScopeReason","MetaPattern_FieldCode","MetaPattern_FieldName",
            "MetaPattern_FieldBlock","MetaPattern_FieldDefaultAssertionType","MetaPattern_FieldDefaultTolerance","MetaPattern_FieldNoiseAware",
            "MetaPattern_FieldStatus","MetaPattern_FieldOutOfScopeReason","MetaPattern_FieldHypothesisTemplate","Replay_FieldId",
            "Replay_FieldSeverity","Replay_FieldStatus","Replay_FieldDiscovered","Replay_Classification",
            "Replay_ColumnField","Replay_ColumnOriginal","Replay_ColumnReplay","Replay_RowMr",
            "Replay_RowSut","Replay_RowTriggerParameters","Replay_RowSourceValue","Replay_RowFollowupValue",
            "Replay_RowAssertion","Replay_RowFinalStatus","Replay_RunRealReplay","Replay_SimulateClassification",
            "Replay_SimulateReplay","History_PageSubtitle","History_FilterByMrName","History_Refresh",
            "History_DeleteSelected","History_Column_RunAt","History_Column_Mr","History_Column_Assertion",
            "History_Column_Value","History_Column_Passed","History_Column_FailureReason","History_PreviousPage",
            "History_NextPage","History_EvidenceForSelectedRow"
        };

        foreach (var key in keys)
        {
            Assert.False(string.IsNullOrWhiteSpace(manager.GetString(key, new CultureInfo("en-US"))), key);
            Assert.False(string.IsNullOrWhiteSpace(manager.GetString(key, new CultureInfo("zh-CN"))), key);
        }
    }
}
