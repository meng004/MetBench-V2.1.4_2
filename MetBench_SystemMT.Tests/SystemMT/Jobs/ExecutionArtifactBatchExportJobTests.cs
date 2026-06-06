using System.Text.Json;
using MetBench_BLL.Core.SystemMT.ImportExport.ExecutionArtifacts;
using MetBench_BLL.SystemMT;
using MetBench_BLL.SystemMT.Jobs;
using MetBench_BLL.SystemMT.Persistence;
using MetBench_BLL.SystemMT.Reporting;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Jobs;

public sealed class ExecutionArtifactBatchExportJobTests
{
    [Fact]
    public async Task SubmitOperationAsync_accepts_execution_export_batch()
    {
        var service = new SystemMtJobService(new InMemoryJobStore(), new ChannelJobQueue());

        var handle = await service.SubmitOperationAsync(new SystemMtOperationJobRequest(
            SystemMtJobKind.ExportExecutionArtifacts,
            ExportRoot: "/tmp/batch",
            ExecutionIds: new[] { Guid.NewGuid(), Guid.NewGuid() }));

        var status = await service.GetStatusAsync(handle.JobId, default);
        Assert.NotNull(status);
        Assert.Equal(2, status!.ExecutionIds!.Count);
    }

    [Fact]
    public async Task SubmitOperationAsync_rejects_export_with_neither_execution_id_nor_ids()
    {
        var service = new SystemMtJobService(new InMemoryJobStore(), new ChannelJobQueue());

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.SubmitOperationAsync(new SystemMtOperationJobRequest(
                SystemMtJobKind.ExportExecutionArtifacts,
                ExportRoot: "/tmp/batch")));

        Assert.Contains("ExecutionId", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SubmitOperationAsync_rejects_empty_guid_in_execution_ids()
    {
        var service = new SystemMtJobService(new InMemoryJobStore(), new ChannelJobQueue());

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.SubmitOperationAsync(new SystemMtOperationJobRequest(
                SystemMtJobKind.ExportExecutionArtifacts,
                ExportRoot: "/tmp/batch",
                ExecutionIds: new[] { Guid.NewGuid(), Guid.Empty })));

        Assert.Contains("ExecutionIds[1]", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Batch_export_writes_per_execution_subfolders_and_batch_manifest()
    {
        using var temp = TempDirectory.Create();
        var idA = Guid.NewGuid();
        var idB = Guid.NewGuid();
        var (service, worker, queue) = BuildPipeline(MakeRecord(idA), MakeRecord(idB));

        var handle = await service.SubmitOperationAsync(new SystemMtOperationJobRequest(
            SystemMtJobKind.ExportExecutionArtifacts,
            ExportRoot: temp.Root,
            ExecutionIds: new[] { idA, idB }));
        await worker.RunJobAsync(await queue.DequeueAsync(default), default);

        var status = await service.GetStatusAsync(handle.JobId, default);
        Assert.Equal(SystemMtJobState.Succeeded, status!.State);
        Assert.Equal(Path.Combine(temp.Root, "batch-manifest.json"), status.ArtifactPath);

        Assert.True(File.Exists(Path.Combine(temp.Root, idA.ToString("N"), "manifest.json")));
        Assert.True(File.Exists(Path.Combine(temp.Root, idA.ToString("N"), "report.html")));
        Assert.True(File.Exists(Path.Combine(temp.Root, idB.ToString("N"), "manifest.json")));

        var manifest = JsonSerializer.Deserialize<ExecutionArtifactBatchManifest>(
            File.ReadAllText(status.ArtifactPath!),
            ExecutionArtifactExporter.JsonOptions);
        Assert.NotNull(manifest);
        Assert.True(manifest!.AllSucceeded);
        Assert.Equal(2, manifest.Items.Count);
        Assert.All(manifest.Items, i => Assert.True(i.Succeeded));
    }

    [Fact]
    public async Task Batch_export_with_missing_execution_fails_but_exports_the_rest()
    {
        using var temp = TempDirectory.Create();
        var idOk = Guid.NewGuid();
        var idMissing = Guid.NewGuid();
        // Only idOk is in the repo; idMissing is not.
        var (service, worker, queue) = BuildPipeline(MakeRecord(idOk));

        var handle = await service.SubmitOperationAsync(new SystemMtOperationJobRequest(
            SystemMtJobKind.ExportExecutionArtifacts,
            ExportRoot: temp.Root,
            ExecutionIds: new[] { idOk, idMissing }));
        await worker.RunJobAsync(await queue.DequeueAsync(default), default);

        var status = await service.GetStatusAsync(handle.JobId, default);
        Assert.Equal(SystemMtJobState.Failed, status!.State);
        Assert.Contains("1 of 2", status.FailureReason!, StringComparison.Ordinal);

        // The good execution still exported.
        Assert.True(File.Exists(Path.Combine(temp.Root, idOk.ToString("N"), "manifest.json")));

        var manifest = JsonSerializer.Deserialize<ExecutionArtifactBatchManifest>(
            File.ReadAllText(Path.Combine(temp.Root, "batch-manifest.json")),
            ExecutionArtifactExporter.JsonOptions);
        Assert.False(manifest!.AllSucceeded);
        Assert.Equal(1, manifest.FailureCount);
        Assert.True(manifest.Items.Single(i => i.ExecutionId == idOk).Succeeded);
        var failed = manifest.Items.Single(i => i.ExecutionId == idMissing);
        Assert.False(failed.Succeeded);
        Assert.NotNull(failed.Error);
    }

    private static (SystemMtJobService, SystemMtJobWorker, ChannelJobQueue) BuildPipeline(
        params SystemMtResultRecord[] records)
    {
        var store = new InMemoryJobStore();
        var queue = new ChannelJobQueue();
        var service = new SystemMtJobService(store, queue);
        var dispatcher = new SystemMtJobOperationDispatcher(new ISystemMtJobOperationHandler[]
        {
            new ExportExecutionArtifactsJobOperationHandler(new ExecutionArtifactExporter(
                new FakeResultRepository(records),
                new FakeEvidenceRepository(),
                new HtmlSystemMtResultReportRenderer())),
        });
        var worker = new SystemMtJobWorker(
            store,
            FakeAsyncPipeline.Succeeds("unused", "sut"),
            operationDispatcher: dispatcher);
        return (service, worker, queue);
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
            new(Path.Combine(Path.GetTempPath(), "MetBenchBatchExportJobTests", Guid.NewGuid().ToString("N")));

        public void Dispose()
        {
            if (Directory.Exists(Root))
                Directory.Delete(Root, recursive: true);
        }
    }
}
