using MetBench_BLL.Core.SystemMT.ImportExport.ExecutionArtifacts;
using MetBench_BLL.SystemMT;
using MetBench_BLL.SystemMT.Jobs;
using MetBench_BLL.SystemMT.Persistence;
using MetBench_BLL.SystemMT.Reporting;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Jobs;

public sealed class ExecutionArtifactExportJobTests
{
    [Fact]
    public void Execution_artifact_export_operation_kind_matches_plan_contract()
    {
        Assert.True(
            Enum.TryParse<SystemMtJobKind>("ExportExecutionArtifacts", out var kind),
            "The T0-T2 async import/export plan and VM prompt require SystemMtJobKind.ExportExecutionArtifacts.");

        Assert.Equal(
            "ExportExecutionArtifacts",
            new ExportExecutionArtifactsJobOperationHandler(new ExecutionArtifactExporter(
                new FakeResultRepository(),
                new FakeEvidenceRepository(),
                new HtmlSystemMtResultReportRenderer())).Kind.ToString());
        Assert.Equal("ExportExecutionArtifacts", kind.ToString());
    }

    [Fact]
    public async Task ExportExecutionArtifacts_job_writes_manifest_artifact_path()
    {
        using var temp = TempDirectory.Create();
        var executionId = Guid.NewGuid();
        var store = new InMemoryJobStore();
        var queue = new ChannelJobQueue();
        var service = new SystemMtJobService(store, queue);
        var dispatcher = new SystemMtJobOperationDispatcher(new ISystemMtJobOperationHandler[]
        {
            new ExportExecutionArtifactsJobOperationHandler(new ExecutionArtifactExporter(
                new FakeResultRepository(MakeRecord(executionId)),
                new FakeEvidenceRepository(),
                new HtmlSystemMtResultReportRenderer())),
        });
        var worker = new SystemMtJobWorker(
            store,
            FakeAsyncPipeline.Succeeds("unused", "sut"),
            operationDispatcher: dispatcher);

        var handle = await service.SubmitOperationAsync(new SystemMtOperationJobRequest(
            SystemMtJobKind.ExportExecutionArtifacts,
            ExportRoot: temp.Root,
            ExecutionId: executionId));
        await worker.RunJobAsync(await queue.DequeueAsync(default), default);

        var status = await service.GetStatusAsync(handle.JobId, default);
        Assert.Equal(SystemMtJobState.Succeeded, status!.State);
        Assert.Equal(Path.Combine(temp.Root, "manifest.json"), status.ArtifactPath);
        Assert.True(File.Exists(status.ArtifactPath));
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
            new(Path.Combine(Path.GetTempPath(), "MetBenchExecutionArtifactJobTests", Guid.NewGuid().ToString("N")));

        public void Dispose()
        {
            if (Directory.Exists(Root))
                Directory.Delete(Root, recursive: true);
        }
    }
}
