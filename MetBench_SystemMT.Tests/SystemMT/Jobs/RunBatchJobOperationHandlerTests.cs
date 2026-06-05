using MetBench_BLL.SystemMT.Jobs;
using MetBench_BLL.SystemMT.Launcher;
using MetBench_BLL.SystemMT.Persistence;
using MetBench_BLL.SystemMT.Runtime;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Jobs;

public sealed class RunBatchJobOperationHandlerTests
{
    [Fact]
    public async Task ExecuteAsync_two_mrs_pass_reaches_succeeded_with_per_mr_summary()
    {
        var launcher = new BatchStubLauncher(new[]
        {
            Result("mr-a", passed: true),
            Result("mr-b", passed: true),
        });
        var handler = new RunBatchJobOperationHandler(launcher);
        var record = Record("mr-a", "mr-b");

        var outcome = await handler.ExecuteAsync(Guid.NewGuid(), record, progress: null, default);

        Assert.Equal(SystemMtJobState.Succeeded, outcome.FinalState);
        Assert.Equal(2, outcome.BatchItems!.Count);
        Assert.All(outcome.BatchItems, item => Assert.Equal(SystemMtBatchItemState.Succeeded, item.State));
        Assert.Equal(new[] { "mr-a", "mr-b" }, launcher.SeenMrIds);
    }

    [Fact]
    public async Task ExecuteAsync_rejects_duplicate_mr_ids_before_dispatch()
    {
        var launcher = new BatchStubLauncher(new[] { Result("mr-a", passed: true) });
        var handler = new RunBatchJobOperationHandler(launcher);
        var record = Record("mr-a", "mr-a");

        var outcome = await handler.ExecuteAsync(Guid.NewGuid(), record, progress: null, default);

        Assert.Equal(SystemMtJobState.Failed, outcome.FinalState);
        Assert.Contains("Duplicate MR id", outcome.FailureReason, StringComparison.Ordinal);
        Assert.Empty(launcher.SeenMrIds);
    }

    [Fact]
    public async Task ExecuteAsync_partial_failed_mr_reaches_succeeded_but_preserves_all_summaries()
    {
        var launcher = new BatchStubLauncher(new[]
        {
            Result("mr-a", passed: true),
            Result("mr-b", passed: false, failure: "assertion failed"),
        });
        var handler = new RunBatchJobOperationHandler(launcher);
        var record = Record("mr-a", "mr-b");

        var outcome = await handler.ExecuteAsync(Guid.NewGuid(), record, progress: null, default);

        Assert.Equal(SystemMtJobState.Succeeded, outcome.FinalState);
        Assert.Null(outcome.FailureReason);
        Assert.Equal(SystemMtBatchItemState.Succeeded, outcome.BatchItems![0].State);
        Assert.Equal(SystemMtBatchItemState.Failed, outcome.BatchItems[1].State);
        Assert.Equal("assertion failed", outcome.BatchItems[1].FailureReason);
    }

    [Fact]
    public async Task ExecuteAsync_synthesized_infrastructure_failure_reaches_failed()
    {
        var launcher = new BatchStubLauncher(new[]
        {
            Result("mr-a", passed: true),
            new MrRunResult(
                RecordId: string.Empty,
                MrId: "mr-b",
                Passed: false,
                FailureReason: "Run threw InvalidOperationException: python missing",
                ValueName: string.Empty,
                SourceValue: 0,
                FollowUpValue: 0,
                SourceElapsed: TimeSpan.Zero,
                FollowUpElapsed: TimeSpan.Zero),
        });
        var handler = new RunBatchJobOperationHandler(launcher);

        var outcome = await handler.ExecuteAsync(Guid.NewGuid(), Record("mr-a", "mr-b"), progress: null, default);

        Assert.Equal(SystemMtJobState.Failed, outcome.FinalState);
        Assert.Equal("one or more batch MR runs failed due to infrastructure errors", outcome.FailureReason);
        Assert.Equal(SystemMtBatchItemState.Failed, outcome.BatchItems![1].State);
    }

    [Fact]
    public async Task ExecuteAsync_runtime_evidence_failure_reaches_failed()
    {
        var executionId = Guid.NewGuid();
        var launcher = new BatchStubLauncher(new[]
        {
            new MrRunResult(
                RecordId: executionId.ToString(),
                MrId: "mr-a",
                Passed: false,
                FailureReason: "dependency missing without legacy prefix",
                ValueName: "value",
                SourceValue: 0,
                FollowUpValue: 0,
                SourceElapsed: TimeSpan.Zero,
                FollowUpElapsed: TimeSpan.Zero),
        });
        var evidence = new StubEvidenceRepository(executionId, RuntimeFailureKind.DependencyMissing.ToString());
        var handler = new RunBatchJobOperationHandler(launcher, evidence);

        var outcome = await handler.ExecuteAsync(Guid.NewGuid(), Record("mr-a"), progress: null, default);

        Assert.Equal(SystemMtJobState.Failed, outcome.FinalState);
        Assert.Equal(RuntimeFailureKind.DependencyMissing.ToString(), outcome.FailureKind);
        Assert.Equal("one or more batch MR runs failed due to infrastructure errors", outcome.FailureReason);
    }

    [Fact]
    public async Task ExecuteAsync_assertion_failure_with_record_id_does_not_mark_job_failed()
    {
        var launcher = new BatchStubLauncher(new[]
        {
            Result("mr-a", passed: false, failure: "assertion failed"),
        });
        var handler = new RunBatchJobOperationHandler(launcher);

        var outcome = await handler.ExecuteAsync(Guid.NewGuid(), Record("mr-a"), progress: null, default);

        Assert.Equal(SystemMtJobState.Succeeded, outcome.FinalState);
        Assert.Equal(SystemMtBatchItemState.Failed, outcome.BatchItems![0].State);
    }

    [Fact]
    public async Task ExecuteAsync_cancellation_before_second_mr_reaches_cancelled_with_second_pending()
    {
        var launcher = new CancellableBatchLauncher(cancelAfterCompleted: 1);
        var handler = new RunBatchJobOperationHandler(launcher);
        var record = Record("mr-a", "mr-b");
        using var cts = new CancellationTokenSource();
        launcher.Cancel = cts.Cancel;

        var outcome = await handler.ExecuteAsync(Guid.NewGuid(), record, progress: null, cts.Token);

        Assert.Equal(SystemMtJobState.Cancelled, outcome.FinalState);
        Assert.Equal(SystemMtBatchItemState.Succeeded, outcome.BatchItems![0].State);
        Assert.Equal(SystemMtBatchItemState.Cancelled, outcome.BatchItems[1].State);
    }

    [Fact]
    public async Task ExecuteAsync_reports_live_per_mr_batch_items_in_progress()
    {
        var launcher = new BatchStubLauncher(new[]
        {
            Result("mr-a", passed: true),
            Result("mr-b", passed: true),
        });
        var handler = new RunBatchJobOperationHandler(launcher);
        var events = new List<SystemMtJobProgress>();
        var progress = new ProgressRecorder(events);

        await handler.ExecuteAsync(Guid.NewGuid(), Record("mr-a", "mr-b"), progress, default);

        Assert.Contains(events, e =>
            e.BatchItems is not null &&
            e.BatchItems[0].State == SystemMtBatchItemState.Succeeded &&
            e.BatchItems[1].State == SystemMtBatchItemState.Pending);
    }

    private static SystemMtJobRecord Record(params string[] ids) => JobsTestData.Record(Guid.NewGuid(), ids[0]) with
    {
        Kind = SystemMtJobKind.RunBatch,
        BatchItems = ids.Select(id => new SystemMtBatchJobItem(id, SystemMtBatchItemState.Pending)).ToList(),
    };

    private static MrRunResult Result(string mrId, bool passed, string failure = "") => new(
        RecordId: Guid.NewGuid().ToString(),
        MrId: mrId,
        Passed: passed,
        FailureReason: passed ? "" : failure,
        ValueName: "value",
        SourceValue: 1,
        FollowUpValue: passed ? 2 : 0,
        SourceElapsed: TimeSpan.Zero,
        FollowUpElapsed: TimeSpan.Zero);

    private static MrSummary Summary(string id) => new(
        id, id, "sut", "transform", "assert", "value",
        new Dictionary<string, string>(), "desc");

    private sealed class BatchStubLauncher : ISystemMtLauncher
    {
        private readonly IReadOnlyList<MrRunResult> _results;
        public string[] SeenMrIds { get; private set; } = Array.Empty<string>();

        public BatchStubLauncher(IReadOnlyList<MrRunResult> results) => _results = results;

        public Task<IReadOnlyList<MrSummary>> ListAvailableAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MrSummary>>(_results.Select(r => Summary(r.MrId)).ToList());

        public Task<MrRunResult> RunAsync(
            string mrId,
            IReadOnlyDictionary<string, string>? parameterOverrides = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_results.Single(r => r.MrId == mrId));

        public Task<IReadOnlyList<MrRunResult>> RunBatchAsync(
            IReadOnlyList<BatchMrRunRequest> requests,
            IProgress<BatchProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            SeenMrIds = requests.Select(r => r.MrId).ToArray();
            var results = new List<MrRunResult>();
            for (var i = 0; i < requests.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var request = requests[i];
                progress?.Report(new BatchProgress(i, requests.Count, request.MrId, null));
                var result = _results.Single(r => r.MrId == request.MrId);
                results.Add(result);
                progress?.Report(new BatchProgress(i + 1, requests.Count, request.MrId, result));
            }

            return Task.FromResult<IReadOnlyList<MrRunResult>>(results);
        }
    }

    private sealed class ProgressRecorder : IProgress<SystemMtJobProgress>
    {
        private readonly List<SystemMtJobProgress> _events;
        public ProgressRecorder(List<SystemMtJobProgress> events) => _events = events;
        public void Report(SystemMtJobProgress value) => _events.Add(value);
    }

    private sealed class StubEvidenceRepository : IExecutionEvidenceRepository
    {
        private readonly Guid _executionId;
        private readonly string _failureKind;

        public StubEvidenceRepository(Guid executionId, string failureKind)
        {
            _executionId = executionId;
            _failureKind = failureKind;
        }

        public Task SaveAsync(ExecutionEvidence evidence, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<ExecutionEvidence?> GetByExecutionAsync(Guid executionId, CancellationToken cancellationToken = default)
        {
            if (executionId != _executionId)
                return Task.FromResult<ExecutionEvidence?>(null);

            return Task.FromResult<ExecutionEvidence?>(new ExecutionEvidence
            {
                ExecutionId = executionId,
                RuntimeEvidence = new RuntimeEvidence
                {
                    Passed = false,
                    FailureKind = _failureKind,
                    FailureDetail = "missing dependency",
                },
            });
        }

        public Task<bool> DeleteByExecutionIdAsync(Guid executionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
    }

    private sealed class CancellableBatchLauncher : ISystemMtLauncher
    {
        private readonly int _cancelAfterCompleted;
        public Action? Cancel { get; set; }

        public CancellableBatchLauncher(int cancelAfterCompleted) =>
            _cancelAfterCompleted = cancelAfterCompleted;

        public Task<IReadOnlyList<MrSummary>> ListAvailableAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MrSummary>>(new[] { Summary("mr-a"), Summary("mr-b") });

        public Task<MrRunResult> RunAsync(
            string mrId,
            IReadOnlyDictionary<string, string>? parameterOverrides = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result(mrId, passed: true));

        public Task<IReadOnlyList<MrRunResult>> RunBatchAsync(
            IReadOnlyList<BatchMrRunRequest> requests,
            IProgress<BatchProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var results = new List<MrRunResult>();
            for (var i = 0; i < requests.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var request = requests[i];
                progress?.Report(new BatchProgress(i, requests.Count, request.MrId, null));
                var result = Result(request.MrId, passed: true);
                results.Add(result);
                progress?.Report(new BatchProgress(i + 1, requests.Count, request.MrId, result));
                if (i + 1 == _cancelAfterCompleted)
                    Cancel?.Invoke();
            }

            return Task.FromResult<IReadOnlyList<MrRunResult>>(results);
        }
    }
}
