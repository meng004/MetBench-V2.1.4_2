using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MetBench_BLL.Paging;
using MetBench_BLL.SystemMT;
using MetBench_BLL.SystemMT.Assertions;
using MetBench_BLL.SystemMT.Persistence;
using MetBench_BLL.SystemMT.Pipeline;
using MetBench_SystemMT.Tests.V2Pipeline;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Pipeline;

/// <summary>
/// PR-4 (T1 non-MR CRUD) acceptance: SystemMtExecutionRecorder mirrors v2 schema
/// executions into the legacy SystemMtResults collection that
/// IExecutionHistoryEditor.ListPagedAsync reads, so the WPF Execution History
/// page sees live runs end-to-end.
///
/// Sequencing fact: Id == ExecutionId so DeleteAsync(executionId.ToString())
/// from the History editor can drop the legacy record by joining through
/// v2 Execution.IdExecution.
/// </summary>
public sealed class LegacyResultMirrorTests
{
    private static PipelineContext CtxFor(string mrCode, string sutName = "openmoc") =>
        new(
            MrCode: mrCode,
            TransformationName: "ScaleNuSigmaF",
            AssertionTypeCode: "greater",
            ValueName: "k_eff",
            TargetFieldPath: "/materials/fuel/nu_sigma_f",
            PathSyntax: "json-pointer",
            Parameters: new Dictionary<string, string> { ["factor"] = "1.5" },
            Tolerance: new AssertionTolerance(),
            ExtraAssertionValues: null,
            SutName: sutName,
            SourceCasePath: "/tmp/pincell.json",
            WorkingDirectory: "/tmp",
            InputParserInvocation: new ProcessInvocation("", Array.Empty<string>()),
            OutputParserInvocation: new ProcessInvocation("", Array.Empty<string>()),
            RunnerInvocation: new ProcessInvocation("", Array.Empty<string>()),
            TimeoutSeconds: 60,
            CatalogVersionSha: "sha",
            SutVersionSnapshot: "snap",
            MetbenchVersion: "v2.2-dev",
            TriggeredBy: "test");

    private static PipelineOutcome OkOutcome() => new(
        FinalStatus: "ok",
        ErrorMessage: null,
        StartedAt: DateTime.UtcNow,
        FinishedAt: DateTime.UtcNow,
        ArtifactsDirectory: "/tmp",
        SourceInputPath: "",
        FollowupInputPath: "/tmp/pincell-followup.json",
        SourceOutputPath: "",
        FollowupOutputPath: "",
        SourceMetrics: new Dictionary<string, double> { ["k_eff"] = 1.0 },
        FollowupMetrics: new Dictionary<string, double> { ["k_eff"] = 1.5 },
        AssertionResult: new SystemMtAssertionResultV2(
            AssertionTypeCode: "greater",
            Passed: true,
            SourceValue: 1.0,
            FollowupValue: 1.5,
            ObservedDelta: 0.5,
            ExpectedThreshold: 0.0,
            Expression: "follow > src",
            FailureReason: null),
        SourceElapsed: TimeSpan.FromSeconds(2),
        FollowupElapsed: TimeSpan.FromSeconds(3),
        SourceExitCode: 0,
        FollowupExitCode: 0);

    [Fact]
    public async Task Record_when_assertion_present_mirrors_to_legacy_result_repo_with_executionId_as_id()
    {
        var execRepo = new FakeExecRepo();
        var resRepo = new FakeResultRepo();
        var legacy = new InMemoryLegacyResultRepo();

        var recorder = new SystemMtExecutionRecorder(execRepo, resRepo, legacyResults: legacy);
        var recorded = await recorder.RecordAsync(CtxFor("openmoc-pincell-nu-sigma-f"), OkOutcome(), mrInstanceId: 1);

        var mirrored = Assert.Single(legacy.Records);
        Assert.Equal(recorded.ExecutionId, mirrored.Id);
        Assert.Equal("openmoc-pincell-nu-sigma-f", mirrored.MrName);
        Assert.Equal("greater", mirrored.AssertionName);
        Assert.Equal("k_eff", mirrored.ValueName);
        Assert.Equal(1.0, mirrored.SourceValue);
        Assert.Equal(1.5, mirrored.FollowUpValue);
        Assert.True(mirrored.Passed);
        Assert.Equal("pincell.json", mirrored.SourceCaseName);
        Assert.Equal("pincell-followup.json", mirrored.FollowUpCaseName);
        Assert.Equal(0, mirrored.SourceExitCode);
        Assert.Equal("ScaleNuSigmaF", mirrored.TransformationName);
        Assert.NotNull(mirrored.TransformationParameters);
        Assert.Equal("1.5", mirrored.TransformationParameters!["factor"]);
        Assert.Equal(1.0, mirrored.SourceMetrics["k_eff"]);
        Assert.Equal(1.5, mirrored.FollowUpMetrics["k_eff"]);
    }

    [Fact]
    public async Task Record_when_legacy_repo_is_null_preserves_pre_mirror_behavior()
    {
        // Backward-compat: every pre-existing recorder construction site passes
        // null legacy repo. Recorder must still write V2 schema and skip the
        // mirror cleanly without throwing.
        var execRepo = new FakeExecRepo();
        var resRepo = new FakeResultRepo();

        var recorder = new SystemMtExecutionRecorder(execRepo, resRepo);
        var recorded = await recorder.RecordAsync(CtxFor("openmoc-pincell-nu-sigma-f"), OkOutcome(), mrInstanceId: 1);

        Assert.NotEqual(Guid.Empty, recorded.ExecutionId);
        Assert.NotNull(recorded.ResultId);
        Assert.Single(execRepo.Data);
        Assert.Single(resRepo.Data);
    }

    [Fact]
    public async Task Record_when_outcome_has_no_AssertionResult_skips_legacy_mirror()
    {
        // For error / timeout / cancelled outcomes the V2 Result row is skipped;
        // legacy mirror must follow the same gate to keep the two collections
        // consistent.
        var execRepo = new FakeExecRepo();
        var resRepo = new FakeResultRepo();
        var legacy = new InMemoryLegacyResultRepo();

        var recorder = new SystemMtExecutionRecorder(execRepo, resRepo, legacyResults: legacy);
        var errored = OkOutcome() with { FinalStatus = "error", AssertionResult = null };
        var recorded = await recorder.RecordAsync(CtxFor("openmoc-pincell-nu-sigma-f"), errored, mrInstanceId: 1);

        Assert.Null(recorded.ResultId);
        Assert.Empty(legacy.Records);
    }

    [Fact]
    public async Task Record_when_assertion_fails_mirrors_failure_reason()
    {
        var execRepo = new FakeExecRepo();
        var resRepo = new FakeResultRepo();
        var legacy = new InMemoryLegacyResultRepo();
        var failedOutcome = OkOutcome() with
        {
            AssertionResult = new SystemMtAssertionResultV2(
                AssertionTypeCode: "greater",
                Passed: false,
                SourceValue: 1.0,
                FollowupValue: 0.8,
                ObservedDelta: -0.2,
                ExpectedThreshold: 0.0,
                Expression: "follow > src",
                FailureReason: "follow-up did not strictly exceed source"),
        };

        var recorder = new SystemMtExecutionRecorder(execRepo, resRepo, legacyResults: legacy);
        await recorder.RecordAsync(CtxFor("openmoc-pincell-nu-sigma-f"), failedOutcome, mrInstanceId: 1);

        var mirrored = Assert.Single(legacy.Records);
        Assert.False(mirrored.Passed);
        Assert.Equal("follow-up did not strictly exceed source", mirrored.FailureReason);
    }

    private sealed class InMemoryLegacyResultRepo : ISystemMtResultRepository
    {
        public List<SystemMtResultRecord> Records { get; } = new();

        public Task<string> SaveAsync(SystemMtResultRecord record, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (record.Id == Guid.Empty) record.Id = Guid.NewGuid();
            if (record.RunAt == default) record.RunAt = DateTimeOffset.UtcNow;
            Records.Add(record);
            return Task.FromResult(record.Id.ToString());
        }

        // Unused interface members
        public Task<string> SaveAsync(string mrName, SystemMtResult result, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<SystemMtResultRecord?> GetAsync(string id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Records.FirstOrDefault(r => r.Id.ToString() == id));
        public Task<IReadOnlyList<SystemMtResultRecord>> ListRecentAsync(int limit = 100, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SystemMtResultRecord>>(Records.Take(limit).ToList());
        public Task<IReadOnlyList<SystemMtResultRecord>> ListByMrNameAsync(string mrName, int limit = 100, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SystemMtResultRecord>>(Records.Where(r => r.MrName == mrName).Take(limit).ToList());
        public Task<PagedResult<SystemMtResultRecord>> ListPagedAsync(PageRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PagedResult<SystemMtResultRecord>(Records, Records.Count, request.PageIndex, request.PageSize));
        public Task<PagedResult<SystemMtResultRecord>> ListPagedByMrNameAsync(string mrName, PageRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PagedResult<SystemMtResultRecord>(Records.Where(r => r.MrName == mrName).ToList(), Records.Count(r => r.MrName == mrName), request.PageIndex, request.PageSize));
        public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            if (!Guid.TryParse(id, out var guid)) return Task.FromResult(false);
            var idx = Records.FindIndex(r => r.Id == guid);
            if (idx < 0) return Task.FromResult(false);
            Records.RemoveAt(idx);
            return Task.FromResult(true);
        }
        public Task<int> DeleteBatchAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default)
        {
            int removed = 0;
            foreach (var id in ids)
            {
                if (Guid.TryParse(id, out var guid))
                {
                    var idx = Records.FindIndex(r => r.Id == guid);
                    if (idx >= 0) { Records.RemoveAt(idx); removed++; }
                }
            }
            return Task.FromResult(removed);
        }
    }
}
