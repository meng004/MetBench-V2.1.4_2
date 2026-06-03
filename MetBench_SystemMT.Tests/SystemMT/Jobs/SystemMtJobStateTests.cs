using MetBench_BLL.SystemMT.Jobs;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Jobs;

public class SystemMtJobStateTests
{
    [Theory]
    [InlineData(SystemMtJobState.Queued, false)]
    [InlineData(SystemMtJobState.Preparing, false)]
    [InlineData(SystemMtJobState.RunningSource, false)]
    [InlineData(SystemMtJobState.RunningFollowup, false)]
    [InlineData(SystemMtJobState.ParsingOutputs, false)]
    [InlineData(SystemMtJobState.Asserting, false)]
    [InlineData(SystemMtJobState.Succeeded, true)]
    [InlineData(SystemMtJobState.Failed, true)]
    [InlineData(SystemMtJobState.TimedOut, true)]
    [InlineData(SystemMtJobState.Cancelled, true)]
    [InlineData(SystemMtJobState.ArtifactMissing, true)]
    public void IsTerminal_matches_state_model(SystemMtJobState state, bool terminal)
        => Assert.Equal(terminal, state.IsTerminal());
}
