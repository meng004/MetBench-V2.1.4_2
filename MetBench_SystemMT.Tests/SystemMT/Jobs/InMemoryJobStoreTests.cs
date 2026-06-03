using MetBench_BLL.SystemMT.Jobs;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Jobs;

public class InMemoryJobStoreTests
{
    [Fact]
    public async Task Create_then_Get_returns_same_record()
    {
        var store = new InMemoryJobStore();
        var id = Guid.NewGuid();
        await store.CreateAsync(JobsTestData.Record(id, "mr-a", "openmc"), default);

        var got = await store.GetAsync(id, default);

        Assert.NotNull(got);
        Assert.Equal("openmc", got!.SutName);
        Assert.Equal(SystemMtJobState.Queued, got.State);
    }

    [Fact]
    public async Task UpdateStatus_then_Get_reflects_new_state()
    {
        var store = new InMemoryJobStore();
        var id = Guid.NewGuid();
        await store.CreateAsync(JobsTestData.Record(id), default);

        await store.UpdateStatusAsync(JobsTestData.Record(id) with
        {
            State = SystemMtJobState.RunningSource,
            ProgressPercent = 40,
            CurrentPhase = "running-source",
        }, default);

        var got = await store.GetAsync(id, default);
        Assert.Equal(SystemMtJobState.RunningSource, got!.State);
        Assert.Equal(40, got.ProgressPercent);
    }

    [Fact]
    public async Task SaveResult_then_GetResult_roundtrips()
    {
        var store = new InMemoryJobStore();
        var id = Guid.NewGuid();
        await store.CreateAsync(JobsTestData.Record(id, "mr-pass"), default);

        await store.SaveResultAsync(id, JobsTestData.Result("mr-pass", passed: true), default);

        var got = await store.GetResultAsync(id, default);
        Assert.NotNull(got);
        Assert.True(got!.Passed);
        Assert.Equal("mr-pass", got.MrId);
    }

    [Fact]
    public async Task UpdateStatus_unknown_id_throws_does_not_ghost_insert()
    {
        var store = new InMemoryJobStore();
        var unknown = Guid.NewGuid();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => store.UpdateStatusAsync(JobsTestData.Record(unknown), default));

        // fail-closed: no ghost record was inserted
        Assert.Null(await store.GetAsync(unknown, default));
    }

    [Fact]
    public async Task UpdateStatus_terminal_record_is_immutable()
    {
        var store = new InMemoryJobStore();
        var id = Guid.NewGuid();
        await store.CreateAsync(JobsTestData.Record(id), default);
        var rec = await store.GetAsync(id, default);
        await store.UpdateStatusAsync(rec! with { State = SystemMtJobState.Succeeded }, default);

        // racing write (e.g. a late CancelAsync) must not overwrite the terminal state
        await store.UpdateStatusAsync(rec! with { State = SystemMtJobState.Cancelled }, default);

        var got = await store.GetAsync(id, default);
        Assert.Equal(SystemMtJobState.Succeeded, got!.State);
    }

    [Fact]
    public async Task Get_unknown_id_returns_null()
        => Assert.Null(await new InMemoryJobStore().GetAsync(Guid.NewGuid(), default));

    [Fact]
    public async Task GetResult_unknown_id_returns_null()
        => Assert.Null(await new InMemoryJobStore().GetResultAsync(Guid.NewGuid(), default));
}
