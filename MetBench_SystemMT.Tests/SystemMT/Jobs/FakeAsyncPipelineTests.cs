using MetBench_BLL.SystemMT.Jobs;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Jobs;

public class FakeAsyncPipelineTests
{
    [Fact]
    public async Task Succeeds_returns_succeeded_outcome_with_result()
    {
        var fake = FakeAsyncPipeline.Succeeds("openmc-q-value", "openmc");
        var phases = new List<SystemMtJobProgress>();

        var outcome = await fake.ExecuteJobAsync(
            Guid.NewGuid(),
            new SystemMtJobRequest("openmc-q-value"),
            new Progress<SystemMtJobProgress>(phases.Add),
            default);

        Assert.Equal(SystemMtJobState.Succeeded, outcome.FinalState);
        Assert.Equal("openmc", outcome.SutName);
        Assert.NotNull(outcome.Result);
        Assert.Equal(1, fake.Invocations);
    }

    [Fact]
    public async Task TimesOut_returns_timedout_with_reason_and_no_result()
    {
        var outcome = await FakeAsyncPipeline.TimesOut("openmc")
            .ExecuteJobAsync(Guid.NewGuid(), new SystemMtJobRequest("mr"), null, default);

        Assert.Equal(SystemMtJobState.TimedOut, outcome.FinalState);
        Assert.Null(outcome.Result);
        Assert.False(string.IsNullOrWhiteSpace(outcome.FailureReason));
    }

    [Fact]
    public async Task Gated_pipeline_blocks_until_gate_released()
    {
        var fake = FakeAsyncPipeline.Succeeds("mr", "openmc", gated: true);
        var run = fake.ExecuteJobAsync(Guid.NewGuid(), new SystemMtJobRequest("mr"), null, default);

        Assert.False(run.IsCompleted);   // 卡在 gate 上
        fake.Gate!.TrySetResult();
        var outcome = await run;
        Assert.Equal(SystemMtJobState.Succeeded, outcome.FinalState);
    }
}
