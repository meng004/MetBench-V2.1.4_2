using MetBench_BLL.SystemMT.Jobs;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Jobs;

public class JobCancellationRegistryTests
{
    [Fact]
    public void Register_returns_uncancelled_token()
    {
        var reg = new JobCancellationRegistry();
        var token = reg.Register(Guid.NewGuid());
        Assert.False(token.IsCancellationRequested);
    }

    [Fact]
    public void Cancel_trips_the_registered_token()
    {
        var reg = new JobCancellationRegistry();
        var id = Guid.NewGuid();
        var token = reg.Register(id);

        reg.Cancel(id);

        Assert.True(token.IsCancellationRequested);
    }

    [Fact]
    public void Cancel_unknown_job_is_noop()
    {
        var reg = new JobCancellationRegistry();
        reg.Cancel(Guid.NewGuid());   // must not throw
    }

    [Fact]
    public void Release_then_Cancel_does_not_throw_and_does_not_trip_a_new_token()
    {
        var reg = new JobCancellationRegistry();
        var id = Guid.NewGuid();
        reg.Register(id);
        reg.Release(id);

        reg.Cancel(id);   // released → no-op, must not throw

        var fresh = reg.Register(id);
        Assert.False(fresh.IsCancellationRequested);
    }
}
