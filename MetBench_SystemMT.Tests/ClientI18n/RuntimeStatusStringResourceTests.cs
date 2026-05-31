using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Text.RegularExpressions;
using Xunit;

namespace MetBench_SystemMT.Tests.ClientI18n;

/// <summary>
/// Guards the runtime ViewModel status-string localization keys: every key must
/// resolve to a non-blank value in both en-US and zh-CN, and every parameterized
/// key must carry the SAME set of {N} placeholder indices in both cultures
/// (placeholder parity) so that string.Format never throws FormatException.
/// </summary>
public sealed class RuntimeStatusStringResourceTests
{
    // Distinct runtime status-string keys (same key + same en collapsed).
    private static readonly string[] Keys =
    {
        "Status_Anomaly_SweepDone_Fmt",
        "Status_Anomaly_SweepFailed_Fmt",
        "Status_Anomaly_UnknownStatus_Fmt",
        "Status_Anomaly_TransitionReturnedFalse_Fmt",
        "Status_CandidateReview_NoValidatorSelected",
        "Status_CandidateReview_Validating",
        "Status_CandidateReview_ValidatorsPassed_Fmt",
        "Status_CandidateReview_PromotedSuffix_Fmt",
        "Status_CandidateReview_Error",
        "Status_Discovery_Running",
        "Status_Discovery_Done_Fmt",
        "Status_Discovery_Error",
        "Status_MetaPattern_Seeded_Fmt",
        "Status_MetaPattern_BeginAdd",
        "Status_MetaPattern_BeginEdit_Fmt",
        "Error_MetaPattern_CodeRequired",
        "Error_MetaPattern_AlreadyExists_Fmt",
        "Status_MetaPattern_Added_Fmt",
        "Error_MetaPattern_ModifyReturnedFalse_Fmt",
        "Status_MetaPattern_Saved_Fmt",
        "Error_MetaPattern_NotFound_Fmt",
        "Status_MetaPattern_StatusToggled_Fmt",
        "Status_MetaPattern_AlreadyDeprecated_Fmt",
        "Status_MetaPattern_SoftDeleted_Fmt",
        "Status_Mutation_LoadError_Fmt",
        "Status_Mutation_SeedError_Fmt",
        "Status_Mutation_Seeded_Fmt",
        "Status_Mutation_SelectAtLeastOne",
        "Status_Mutation_Starting",
        "Status_Mutation_CellProgress_Fmt",
        "Status_Mutation_Done_Fmt",
        "Status_Mutation_Cancelled",
        "Status_Mutation_Error",
        "Status_Replay_NoAnomalySelected",
        "Status_Replay_NoResultRecord_Fmt",
        "Hint_Replay_MrCodePlaceholder",
        "Status_Replay_PickAnAction",
        "Hint_Replay_NoTriggerParameters",
        "Status_Replay_RealReplayCompleted_Fmt",
        "Status_Replay_RealReplayFailed_Fmt",
        "Status_Replay_SimulatedExpression_Fmt",
        "Status_Replay_SimulatedOnly",
        "Status_Equation_InitialPrompt",
        "Status_Equation_LoadedBuiltInReadOnly_Fmt",
        "Status_Equation_NewDraft",
        "Status_Equation_DraftValid",
        "Status_Equation_ValidationFailed_Fmt",
        "Status_Equation_Saved_Fmt",
        "Status_Equation_SaveBlocked_Fmt",
        "Status_Equation_Deleted_Fmt",
        "Status_Equation_DeleteBlocked_Fmt",
        "Status_Equation_Loaded_Fmt",
        "Status_Equation_ErrorListing_Fmt",
        "Status_Equation_LoadedUser_Fmt",
        "Status_Equation_UserNotFound_Fmt",
        "Status_Equation_ErrorLoading_Fmt",
        "Status_History_Loading",
        "Pagination_History_NoRows",
        "Pagination_History_PageOfTotal_Fmt",
        "Status_History_SelectRowBeforeDelete",
        "Prompt_History_DeleteSingle_Fmt",
        "Prompt_History_DeleteMultiple_Fmt",
        "Prompt_History_ConfirmDeleteTitle",
        "Status_History_NoRecordsMatchFilter",
        "Status_History_LoadedRecords_Fmt",
        "Status_History_ErrorLoadingExecutions_Fmt",
        "Evidence_History_NoEvidenceRow",
        "Evidence_History_ErrorLoadingEvidence_Fmt",
        "Evidence_History_ExecutionId_Fmt",
        "Evidence_History_RecordedAt_Fmt",
        "Evidence_History_TypedVerificationHeader",
        "Evidence_History_Spec_Fmt",
        "Evidence_History_Predicate_Fmt",
        "Evidence_History_Status_Fmt",
        "Evidence_History_Expected_Fmt",
        "Evidence_History_Actual_Fmt",
        "Evidence_History_Residual_Fmt",
        "Evidence_History_Tolerance_Fmt",
        "Evidence_History_Reason_Fmt",
        "Status_History_DeleteDeleted_Fmt",
        "Status_History_DeleteEvidenceOnly_Fmt",
        "Status_History_DeleteFailed_Fmt",
        "Status_History_NothingDeleted",
        "Status_Execution_Idle",
        "Status_Execution_Running_Fmt",
        "Status_Execution_ResultPass_Fmt",
        "Status_Execution_ResultFail_Fmt",
        "Status_Execution_Completed_Fmt",
        "Status_Execution_Error_Fmt",
        "Status_Execution_RunFailed_Title",
        "Status_Execution_Refreshed_Fmt",
        "Status_Execution_ReportExported_Fmt",
        "Status_Catalog_SelectManifest",
        "Status_Catalog_DraftCreated",
        "Status_Catalog_DraftValid",
        "Status_Catalog_ValidationFailed_Fmt",
        "Status_Catalog_DraftSaved",
        "Status_Catalog_SaveBlocked_Fmt",
        "Status_Catalog_NoManifests",
        "Status_Catalog_ManifestsLoaded_Fmt",
        "Status_Catalog_LoadManifestsError_Fmt",
        "Status_Catalog_BindingsLoaded_Fmt",
        "Status_SampleCatalog_SelectSut",
        "Status_SampleCatalog_NoSamplesUnderSut_Fmt",
        "Status_SampleCatalog_LoadedSamples_Fmt",
        "Status_SampleCatalog_ErrorListingSamples_Fmt",
        "Status_SampleCatalog_LoadedSample_Fmt",
        "Status_SampleCatalog_ErrorLoadingSample_Fmt",
        "Status_SampleCatalog_EditThenValidate",
        "Status_SampleCatalog_DraftValid",
        "Status_SampleCatalog_ValidationFailed_Fmt",
        "Status_SampleCatalog_SampleSaved_Fmt",
        "Status_SampleCatalog_SaveBlocked_Fmt",
        "Dialog_SampleCatalog_ConfirmDeleteText_Fmt",
        "Dialog_SampleCatalog_ConfirmDeleteCaption",
        "Status_SampleCatalog_SampleDeleted_Fmt",
        "Status_SampleCatalog_DeleteBlocked_Fmt",
        "Status_SampleCatalog_NoSuts",
        "Status_SampleCatalog_LoadedSuts_Fmt",
        "Status_SampleCatalog_ErrorListingSuts_Fmt",
        "Status_SutCatalog_InitialHint",
        "Status_SutCatalog_DraftLoaded_Fmt",
        "Status_SutCatalog_DraftLoadError_Fmt",
        "Status_SutCatalog_NewDraft",
        "Status_SutCatalog_ValidationFailed_NameRequired",
        "Status_SutCatalog_DraftValid",
        "Status_SutCatalog_ValidationFailed_Fmt",
        "Status_SutCatalog_SaveBlocked_NameRequired",
        "Status_SutCatalog_DraftSaved_Fmt",
        "Status_SutCatalog_SaveBlocked_Fmt",
        "Status_SutCatalog_NoCatalogFiles",
        "Status_SutCatalog_Loaded_Fmt",
        "Status_SutCatalog_ListError_Fmt",
    };

    private static readonly Regex PlaceholderRegex = new(@"\{(\d+)", RegexOptions.Compiled);

    private static ResourceManager Manager() => new(
        "MetBench_UI.Localization.Resources.Strings",
        typeof(MetBench_UI.Localization.AppLocalizationService).Assembly);

    private static SortedSet<int> PlaceholderIndices(string value) =>
        new(PlaceholderRegex.Matches(value).Select(m => int.Parse(m.Groups[1].Value)));

    [Fact]
    public void Every_runtime_status_key_resolves_in_english_and_chinese()
    {
        var manager = Manager();
        var en = new CultureInfo("en-US");
        var zh = new CultureInfo("zh-CN");

        foreach (var key in Keys)
        {
            Assert.False(string.IsNullOrWhiteSpace(manager.GetString(key, en)), $"en-US missing/blank: {key}");
            Assert.False(string.IsNullOrWhiteSpace(manager.GetString(key, zh)), $"zh-CN missing/blank: {key}");
        }
    }

    [Fact]
    public void Parameterized_keys_have_matching_placeholder_indices_across_cultures()
    {
        var manager = Manager();
        var en = new CultureInfo("en-US");
        var zh = new CultureInfo("zh-CN");

        foreach (var key in Keys)
        {
            var enValue = manager.GetString(key, en);
            var zhValue = manager.GetString(key, zh);
            Assert.NotNull(enValue);
            Assert.NotNull(zhValue);

            var enIndices = PlaceholderIndices(enValue!);
            var zhIndices = PlaceholderIndices(zhValue!);

            Assert.True(
                enIndices.SetEquals(zhIndices),
                $"Placeholder parity mismatch for '{key}': en={{{string.Join(",", enIndices)}}} zh={{{string.Join(",", zhIndices)}}}");
        }
    }
}
