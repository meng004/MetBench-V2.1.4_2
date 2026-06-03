using MetBench_BLL.Paging;
using MetBench_BLL.SystemMT.Persistence;
using MetBench_BLL.SystemMT.Persistence.Editing;
using MetBench_Client.ViewModels;
using MetBench_UI.Localization;
using Xunit;

namespace MetBench_Client.Tests.ClientI18n;

/// <summary>
/// PR-5 execution-history pair-quality surface. The evidence summary must render
/// pair-quality counts/rates when <see cref="ExecutionEvidence.PairQuality"/> is
/// non-empty, and stay quiet (no pair-quality section) for default-empty or
/// missing evidence rows.
/// </summary>
public sealed class SystemMtPairQualityEvidenceTests
{
    private static (SystemMtExecutionHistoryViewModel vm, LocalizedTextProvider loc) BuildVm(ExecutionEvidence? evidence)
    {
        var localization = new AppLocalizationService();
        var loc = new LocalizedTextProvider(localization);
        var vm = new SystemMtExecutionHistoryViewModel(new FakeHistoryEditor(evidence), loc);
        return (vm, loc);
    }

    private static SystemMtResultRecord Record(Guid id) => new()
    {
        Id = id,
        MrName = "heat-equation-amplitude",
        RunAt = DateTimeOffset.UnixEpoch,
    };

    [WpfFact]
    public void Evidence_summary_renders_pair_quality_counts_and_rates()
    {
        var id = Guid.NewGuid();
        var pq = new PairQualitySummary
        {
            PlannedPairs = 4,
            ExecutedPairs = 4,
            ValidPairs = 3,
            PassedPairs = 2,
            FailedPairs = 1,
            SkippedPairs = 1,
            InvalidSpecPairs = 0,
        };
        pq.SkipReasons.Add(PairQualityReasonCount.From("SkippedNotApplicable", "missing observable", 1));
        pq.RecalculateRates();
        var evidence = new ExecutionEvidence { ExecutionId = id, PairQuality = pq };

        var (vm, loc) = BuildVm(evidence);
        vm.SelectedRecord = Record(id);

        var summary = vm.SelectedEvidenceSummary;
        Assert.Contains(loc["Evidence_History_PairQualityHeader"], summary);
        Assert.Contains("Planned pairs 4", summary);
        Assert.Contains("Executed pairs 4", summary);
        Assert.Contains("Valid pairs 3", summary);
        Assert.Contains("Passed pairs 2", summary);
        Assert.Contains("Failed pairs 1", summary);
        Assert.Contains("Skipped pairs 1", summary);
        Assert.Contains("Invalid spec pairs 0", summary);
        // invariant P1 rates: 2/3 -> 66.7 %, 2/4 -> 50.0 % (invariant percent pattern keeps a space)
        Assert.Contains("66.7", summary);
        Assert.Contains("50.0", summary);
        Assert.Contains("%", summary);
        // skip reason distribution surfaced
        Assert.Contains("missing observable", summary);
        Assert.Contains(loc["Evidence_History_PairQualitySkipReasonsHeader"], summary);
    }

    [WpfFact]
    public void Evidence_summary_stays_quiet_for_default_empty_pair_quality()
    {
        var id = Guid.NewGuid();
        var evidence = new ExecutionEvidence { ExecutionId = id }; // default-empty PairQuality

        var (vm, loc) = BuildVm(evidence);
        vm.SelectedRecord = Record(id);

        var summary = vm.SelectedEvidenceSummary;
        // Execution metadata still renders, but no pair-quality section.
        Assert.False(string.IsNullOrWhiteSpace(summary));
        Assert.DoesNotContain(loc["Evidence_History_PairQualityHeader"], summary);
    }

    [WpfFact]
    public async Task Refresh_clears_stale_evidence_when_filter_removes_selected_row()
    {
        var id = Guid.NewGuid();
        var pq = new PairQualitySummary
        {
            PlannedPairs = 1,
            ExecutedPairs = 1,
            ValidPairs = 1,
            PassedPairs = 1,
        };
        pq.RecalculateRates();
        var evidence = new ExecutionEvidence { ExecutionId = id, PairQuality = pq };

        var (vm, loc) = BuildVm(evidence);
        vm.SelectedRecord = Record(id);
        Assert.Contains(loc["Evidence_History_PairQualityHeader"], vm.SelectedEvidenceSummary);

        vm.MrNameFilter = "__missing__";
        await vm.RefreshCommand.ExecuteAsync(null);

        Assert.Null(vm.SelectedRecord);
        Assert.True(string.IsNullOrEmpty(vm.SelectedEvidenceSummary));
        Assert.DoesNotContain(loc["Evidence_History_PairQualityHeader"], vm.SelectedEvidenceSummary);
    }

    [WpfFact]
    public void Evidence_summary_reports_no_evidence_when_row_missing()
    {
        var (vm, loc) = BuildVm(evidence: null);
        vm.SelectedRecord = Record(Guid.NewGuid());

        Assert.Equal(loc["Evidence_History_NoEvidenceRow"], vm.SelectedEvidenceSummary);
    }

    private sealed class FakeHistoryEditor : IExecutionHistoryEditor
    {
        private readonly ExecutionEvidence? _evidence;
        public FakeHistoryEditor(ExecutionEvidence? evidence) => _evidence = evidence;

        public Task<PagedResult<SystemMtResultRecord>> ListPagedAsync(PageRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(PagedResult<SystemMtResultRecord>.Empty(request.PageIndex, request.PageSize));

        public Task<PagedResult<SystemMtResultRecord>> ListPagedByMrNameAsync(string mrName, PageRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(PagedResult<SystemMtResultRecord>.Empty(request.PageIndex, request.PageSize));

        public Task<ExecutionEvidence?> GetEvidenceAsync(Guid executionId, CancellationToken cancellationToken = default)
            => Task.FromResult(_evidence);

        public Task<ExecutionHistoryDeleteResult> DeleteAsync(Guid executionId, CancellationToken cancellationToken = default)
            => Task.FromResult(new ExecutionHistoryDeleteResult(0, 0, 0));

        public Task<ExecutionHistoryDeleteResult> DeleteBatchAsync(IEnumerable<Guid> executionIds, CancellationToken cancellationToken = default)
            => Task.FromResult(new ExecutionHistoryDeleteResult(0, 0, 0));
    }
}
