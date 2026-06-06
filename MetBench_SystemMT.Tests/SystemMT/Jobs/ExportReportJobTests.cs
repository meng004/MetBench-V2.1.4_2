using MetBench_BLL.Core.SystemMT.ImportExport.ExecutionArtifacts;
using MetBench_BLL.SystemMT;
using MetBench_BLL.SystemMT.Jobs;
using MetBench_BLL.SystemMT.Persistence;
using MetBench_BLL.SystemMT.Reporting;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Jobs;

public sealed class ExportReportJobTests
{
    [Fact]
    public async Task SubmitOperationAsync_accepts_export_report_with_execution_and_root()
    {
        var service = new SystemMtJobService(new InMemoryJobStore(), new ChannelJobQueue());

        var handle = await service.SubmitOperationAsync(new SystemMtOperationJobRequest(
            SystemMtJobKind.ExportReport,
            ExportRoot: "/tmp/report",
            ExecutionId: Guid.NewGuid()));

        var status = await service.GetStatusAsync(handle.JobId, default);
        Assert.NotNull(status);
        Assert.Equal(SystemMtJobKind.ExportReport, status!.Kind);
    }

    [Fact]
    public async Task SubmitOperationAsync_rejects_export_report_without_execution_id()
    {
        var service = new SystemMtJobService(new InMemoryJobStore(), new ChannelJobQueue());

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.SubmitOperationAsync(new SystemMtOperationJobRequest(
                SystemMtJobKind.ExportReport,
                ExportRoot: "/tmp/report",
                ExecutionId: null)));

        Assert.Contains("ExecutionId", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExportReport_job_writes_html_report_only_no_result_json()
    {
        using var temp = TempDirectory.Create();
        var executionId = Guid.NewGuid();
        var store = new InMemoryJobStore();
        var queue = new ChannelJobQueue();
        var service = new SystemMtJobService(store, queue);
        var dispatcher = new SystemMtJobOperationDispatcher(new ISystemMtJobOperationHandler[]
        {
            new ExportReportJobOperationHandler(new ExecutionArtifactExporter(
                new FakeResultRepository(MakeRecord(executionId)),
                new FakeEvidenceRepository(),
                new HtmlSystemMtResultReportRenderer())),
        });
        var worker = new SystemMtJobWorker(
            store,
            FakeAsyncPipeline.Succeeds("unused", "sut"),
            operationDispatcher: dispatcher);

        var handle = await service.SubmitOperationAsync(new SystemMtOperationJobRequest(
            SystemMtJobKind.ExportReport,
            ExportRoot: temp.Root,
            ExecutionId: executionId));
        await worker.RunJobAsync(await queue.DequeueAsync(default), default);

        var status = await service.GetStatusAsync(handle.JobId, default);
        Assert.Equal(SystemMtJobState.Succeeded, status!.State);
        Assert.Equal(Path.Combine(temp.Root, "manifest.json"), status.ArtifactPath);
        Assert.True(File.Exists(Path.Combine(temp.Root, "report.html")));
        // Report-only: no data JSON files.
        Assert.False(File.Exists(Path.Combine(temp.Root, "execution-result.json")));
        Assert.False(File.Exists(Path.Combine(temp.Root, "execution-evidence.json")));
    }

    [Fact]
    public async Task ExportReport_job_fails_closed_when_execution_missing()
    {
        using var temp = TempDirectory.Create();
        var store = new InMemoryJobStore();
        var queue = new ChannelJobQueue();
        var service = new SystemMtJobService(store, queue);
        var dispatcher = new SystemMtJobOperationDispatcher(new ISystemMtJobOperationHandler[]
        {
            new ExportReportJobOperationHandler(new ExecutionArtifactExporter(
                new FakeResultRepository(),
                new FakeEvidenceRepository(),
                new HtmlSystemMtResultReportRenderer())),
        });
        var worker = new SystemMtJobWorker(
            store,
            FakeAsyncPipeline.Succeeds("unused", "sut"),
            operationDispatcher: dispatcher);

        var handle = await service.SubmitOperationAsync(new SystemMtOperationJobRequest(
            SystemMtJobKind.ExportReport,
            ExportRoot: temp.Root,
            ExecutionId: Guid.NewGuid()));
        await worker.RunJobAsync(await queue.DequeueAsync(default), default);

        var status = await service.GetStatusAsync(handle.JobId, default);
        Assert.Equal(SystemMtJobState.Failed, status!.State);
    }

    private static SystemMtResultRecord MakeRecord(Guid executionId) => new()
    {
        Id = executionId,
        MrName = "p5-power-response",
        RunAt = DateTimeOffset.UtcNow,
        AssertionName = "greater",
        ValueName = "power_extrema",
        SourceValue = 1,
        FollowUpValue = 2,
        Passed = true,
        SourceCaseName = "source",
        FollowUpCaseName = "follow-up",
    };

    private sealed class FakeResultRepository : ISystemMtResultRepository
    {
        private readonly Dictionary<string, SystemMtResultRecord> _records = new(StringComparer.Ordinal);

        public FakeResultRepository(params SystemMtResultRecord[] records)
        {
            foreach (var record in records)
                _records[record.Id.ToString()] = record;
        }

        public Task<SystemMtResultRecord?> GetAsync(string id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _records.TryGetValue(id, out var record);
            return Task.FromResult<SystemMtResultRecord?>(record);
        }

        public Task<string> SaveAsync(string mrName, SystemMtResult result, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<string> SaveAsync(SystemMtResultRecord record, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<IReadOnlyList<SystemMtResultRecord>> ListRecentAsync(int limit = 100, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<IReadOnlyList<SystemMtResultRecord>> ListByMrNameAsync(string mrName, int limit = 100, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<MetBench_BLL.Paging.PagedResult<SystemMtResultRecord>> ListPagedAsync(MetBench_BLL.Paging.PageRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<MetBench_BLL.Paging.PagedResult<SystemMtResultRecord>> ListPagedByMrNameAsync(string mrName, MetBench_BLL.Paging.PageRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<int> DeleteBatchAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeEvidenceRepository : IExecutionEvidenceRepository
    {
        public Task<ExecutionEvidence?> GetByExecutionAsync(Guid executionId, CancellationToken cancellationToken = default) =>
            Task.FromResult<ExecutionEvidence?>(null);
        public Task SaveAsync(ExecutionEvidence evidence, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<bool> DeleteByExecutionIdAsync(Guid executionId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class TempDirectory : IDisposable
    {
        public string Root { get; }

        private TempDirectory(string root) => Root = root;

        public static TempDirectory Create() =>
            new(Path.Combine(Path.GetTempPath(), "MetBenchExportReportJobTests", Guid.NewGuid().ToString("N")));

        public void Dispose()
        {
            if (Directory.Exists(Root))
                Directory.Delete(Root, recursive: true);
        }
    }
}
