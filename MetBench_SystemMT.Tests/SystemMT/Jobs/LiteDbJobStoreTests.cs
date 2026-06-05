using MetBench_BLL.SystemMT.Jobs;
using MetBench_DAL;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Jobs;

public class LiteDbJobStoreTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"jobstore-{Guid.NewGuid():N}.litedb");
    private string Conn => $"Filename={_dbPath}";

    [Fact]
    public async Task Create_then_Get_roundtrips_all_fields()
    {
        using var store = new LiteDbJobStore(Conn);
        var id = Guid.NewGuid();
        var record = new SystemMtJobRecord
        {
            JobId = id, MrId = "openmc-q-value", SutName = "openmc",
            Kind = SystemMtJobKind.RunBatch,
            ParameterOverrides = new Dictionary<string, string> { ["factor"] = "2" },
            State = SystemMtJobState.RunningFollowup, ProgressPercent = 70,
            CurrentPhase = "running-followup", FailureReason = null,
            PackageRoot = "/tmp/package",
            StagingRoot = "/tmp/stage",
            ExportRoot = "/tmp/export",
            ExecutionId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            ArtifactPath = "/tmp/export/result.json",
            BatchItems = new[]
            {
                new SystemMtBatchJobItem("openmc-q-value", SystemMtBatchItemState.Succeeded, RecordId: "r1"),
                new SystemMtBatchJobItem("openmc-keff", SystemMtBatchItemState.Failed, "assertion failed", "r2"),
            },
            BackendKind = "local", BackendExternalId = "pid-1234",
            CreatedAtUtc = new DateTime(2026, 6, 3, 1, 0, 0, DateTimeKind.Utc),
            UpdatedAtUtc = new DateTime(2026, 6, 3, 1, 2, 0, DateTimeKind.Utc),
        };
        await store.CreateAsync(record, default);

        var got = await store.GetAsync(id, default);
        Assert.NotNull(got);
        Assert.Equal(record.Kind, got!.Kind);
        Assert.Equal(record.MrId, got!.MrId);
        Assert.Equal("2", got.ParameterOverrides!["factor"]);
        Assert.Equal(record.SutName, got.SutName);
        Assert.Equal(record.State, got.State);
        Assert.Equal(record.ProgressPercent, got.ProgressPercent);
        Assert.Equal(record.CurrentPhase, got.CurrentPhase);
        Assert.Equal(record.PackageRoot, got.PackageRoot);
        Assert.Equal(record.StagingRoot, got.StagingRoot);
        Assert.Equal(record.ExportRoot, got.ExportRoot);
        Assert.Equal(record.ExecutionId, got.ExecutionId);
        Assert.Equal(record.ArtifactPath, got.ArtifactPath);
        Assert.Equal(2, got.BatchItems.Count);
        Assert.Equal(SystemMtBatchItemState.Failed, got.BatchItems[1].State);
        Assert.Equal(record.BackendExternalId, got.BackendExternalId);
        Assert.Equal(record.CreatedAtUtc, got.CreatedAtUtc);
    }

    [Fact]
    public async Task UpdateStatus_persists_across_reopen()
    {
        var id = Guid.NewGuid();
        using (var store = new LiteDbJobStore(Conn))
        {
            await store.CreateAsync(new SystemMtJobRecord
            {
                JobId = id, MrId = "mr", SutName = "openmc",
                State = SystemMtJobState.Queued, CurrentPhase = "queued",
                CreatedAtUtc = DateTime.UnixEpoch, UpdatedAtUtc = DateTime.UnixEpoch,
            }, default);
            var rec = await store.GetAsync(id, default);
            await store.UpdateStatusAsync(rec! with { State = SystemMtJobState.Succeeded, ProgressPercent = 100 }, default);
        }

        using var reopened = new LiteDbJobStore(Conn);
        var got = await reopened.GetAsync(id, default);
        Assert.Equal(SystemMtJobState.Succeeded, got!.State);
        Assert.Equal(100, got.ProgressPercent);
    }

    [Fact]
    public async Task SaveResult_then_GetResult_roundtrips()
    {
        var id = Guid.NewGuid();
        using (var store = new LiteDbJobStore(Conn))
        {
            await store.CreateAsync(JobsTestData.Record(id, "mr-pass"), default);
            await store.SaveResultAsync(id, JobsTestData.Result("mr-pass", passed: true), default);
        }

        using var reopened = new LiteDbJobStore(Conn);
        var got = await reopened.GetResultAsync(id, default);
        Assert.NotNull(got);
        Assert.True(got!.Passed);
        Assert.Equal("mr-pass", got.MrId);
    }

    [Fact]
    public async Task UpdateStatus_unknown_id_throws_fail_closed()
    {
        using var store = new LiteDbJobStore(Conn);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => store.UpdateStatusAsync(JobsTestData.Record(Guid.NewGuid()), default));
    }

    [Fact]
    public async Task UpdateStatus_terminal_record_is_immutable()
    {
        using var store = new LiteDbJobStore(Conn);
        var id = Guid.NewGuid();
        await store.CreateAsync(JobsTestData.Record(id), default);
        var rec = await store.GetAsync(id, default);
        await store.UpdateStatusAsync(rec! with { State = SystemMtJobState.Succeeded }, default);

        await store.UpdateStatusAsync(rec! with { State = SystemMtJobState.Cancelled }, default);

        var got = await store.GetAsync(id, default);
        Assert.Equal(SystemMtJobState.Succeeded, got!.State);
    }

    [Fact]
    public async Task Get_unknown_id_returns_null()
    {
        using var store = new LiteDbJobStore(Conn);
        Assert.Null(await store.GetAsync(Guid.NewGuid(), default));
    }

    public void Dispose()
    {
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
    }
}
