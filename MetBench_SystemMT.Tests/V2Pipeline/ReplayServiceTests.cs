using MetBench_BLL.SystemMT.Assertions;
using MetBench_BLL.SystemMT.Pipeline;
using Xunit;

namespace MetBench_SystemMT.Tests.V2Pipeline;

/// <summary>
/// TDD 验证 P4.4 ReplayService —�?�?Pipeline 抽象�?fake�?
/// 仅验证分类逻辑（Reproduced / FixedOrFlaky / RegressionOnReplay / 等）�?
/// </summary>
public sealed class ReplayServiceTests
{
    [Fact]
    public async Task Replay_reproduces_when_both_anomaly_same_value()
    {
        var (svc, ctx, original) = MakeOriginalAndService(
            originalStatus: PipelineStatus.Anomaly,
            replayStatus: PipelineStatus.Anomaly,
            originalFollowup: 0.51,
            replayFollowup: 0.51);
        var result = await svc.ReplayAsync(ctx, original);
        Assert.Equal(ReplayClassification.Reproduced, result.Classification);
    }

    [Fact]
    public async Task Replay_mismatched_when_both_anomaly_different_value()
    {
        var (svc, ctx, original) = MakeOriginalAndService(
            originalStatus: PipelineStatus.Anomaly,
            replayStatus: PipelineStatus.Anomaly,
            originalFollowup: 0.51,
            replayFollowup: 0.72);
        var result = await svc.ReplayAsync(ctx, original);
        Assert.Equal(ReplayClassification.MismatchedFailure, result.Classification);
    }

    [Fact]
    public async Task Replay_fixed_or_flaky_when_was_anomaly_now_ok()
    {
        var (svc, ctx, original) = MakeOriginalAndService(
            originalStatus: PipelineStatus.Anomaly,
            replayStatus: PipelineStatus.Ok,
            originalFollowup: 0.51,
            replayFollowup: 1.11);
        var result = await svc.ReplayAsync(ctx, original);
        Assert.Equal(ReplayClassification.FixedOrFlaky, result.Classification);
    }

    [Fact]
    public async Task Replay_regression_when_was_ok_now_anomaly()
    {
        var (svc, ctx, original) = MakeOriginalAndService(
            originalStatus: PipelineStatus.Ok,
            replayStatus: PipelineStatus.Anomaly,
            originalFollowup: 1.11,
            replayFollowup: 0.51);
        var result = await svc.ReplayAsync(ctx, original);
        Assert.Equal(ReplayClassification.RegressionOnReplay, result.Classification);
    }

    [Fact]
    public async Task Replay_still_passing_when_both_ok()
    {
        var (svc, ctx, original) = MakeOriginalAndService(
            originalStatus: PipelineStatus.Ok,
            replayStatus: PipelineStatus.Ok,
            originalFollowup: 1.11,
            replayFollowup: 1.11);
        var result = await svc.ReplayAsync(ctx, original);
        Assert.Equal(ReplayClassification.StillPassing, result.Classification);
    }

    [Fact]
    public async Task Replay_not_comparable_when_replay_errors()
    {
        var (svc, ctx, original) = MakeOriginalAndService(
            originalStatus: PipelineStatus.Anomaly,
            replayStatus: PipelineStatus.Error,
            originalFollowup: 0.51,
            replayFollowup: 0.0);
        var result = await svc.ReplayAsync(ctx, original);
        Assert.Equal(ReplayClassification.NotComparable, result.Classification);
    }

    private static (ReplayService, PipelineContext, PipelineOutcome) MakeOriginalAndService(
        string originalStatus, string replayStatus,
        double originalFollowup, double replayFollowup)
    {
        var replayPipeline = new FakePipeline(replayStatus, replayFollowup);
        var svc = new ReplayService(replayPipeline);

        var ctx = new PipelineContext(
            MrCode: "MR-T",
            TransformationName: "ScaleField",
            AssertionTypeCode: "less",
            ValueName: "k_eff",
            TargetFieldPath: "x", PathSyntax: "json-pointer",
            Parameters: new Dictionary<string, string>(),
            Tolerance: new AssertionTolerance(),
            ExtraAssertionValues: null,
            SutName: "test", SourceCasePath: "/tmp/x", WorkingDirectory: "/tmp",
            InputParserInvocation: new ProcessInvocation("x", Array.Empty<string>()), OutputParserInvocation: new ProcessInvocation("x", Array.Empty<string>()), RunnerInvocation: new ProcessInvocation("x", Array.Empty<string>()),
            TimeoutSeconds: 60,
            CatalogVersionSha: "sha", SutVersionSnapshot: "v1",
            MetbenchVersion: "v2", TriggeredBy: "test");

        var original = new PipelineOutcome(
            FinalStatus: originalStatus,
            ErrorMessage: null,
            StartedAt: DateTime.UtcNow.AddMinutes(-10),
            FinishedAt: DateTime.UtcNow.AddMinutes(-9),
            ArtifactsDirectory: "/tmp",
            SourceInputPath: "/tmp/s.in", FollowupInputPath: "/tmp/f.in",
            SourceOutputPath: "/tmp/s.out", FollowupOutputPath: "/tmp/f.out",
            SourceMetrics: new Dictionary<string, double> { ["k_eff"] = 1.13 },
            FollowupMetrics: new Dictionary<string, double> { ["k_eff"] = originalFollowup },
            AssertionResult: null,
            SourceElapsed: TimeSpan.Zero, FollowupElapsed: TimeSpan.Zero,
            SourceExitCode: 0, FollowupExitCode: 0);

        return (svc, ctx, original);
    }
}

internal sealed class FakePipeline : ISystemMtPipeline
{
    private readonly string _replayStatus;
    private readonly double _replayFollowupValue;

    public FakePipeline(string replayStatus, double replayFollowupValue)
    {
        _replayStatus = replayStatus;
        _replayFollowupValue = replayFollowupValue;
    }

    public Task<PipelineOutcome> ExecuteAsync(
        PipelineContext context, IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new PipelineOutcome(
            FinalStatus: _replayStatus,
            ErrorMessage: null,
            StartedAt: DateTime.UtcNow,
            FinishedAt: DateTime.UtcNow,
            ArtifactsDirectory: "/tmp",
            SourceInputPath: "/tmp/s", FollowupInputPath: "/tmp/f",
            SourceOutputPath: "/tmp/so", FollowupOutputPath: "/tmp/fo",
            SourceMetrics: new Dictionary<string, double> { ["k_eff"] = 1.13 },
            FollowupMetrics: new Dictionary<string, double> { [context.ValueName] = _replayFollowupValue },
            AssertionResult: null,
            SourceElapsed: TimeSpan.Zero, FollowupElapsed: TimeSpan.Zero,
            SourceExitCode: 0, FollowupExitCode: 0));
    }

    // PR-Bol-2A: ISystemMtPipeline interface added ExecuteMultiPhaseAsync; replay tests do not
    // exercise the multi-phase path, so throw NotImplementedException �?any test that hits this
    // surface accidentally fails loudly.
    public Task<PipelineOutcome> ExecuteMultiPhaseAsync(
        MultiPhaseExecutionContext mp,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
        => throw new NotImplementedException("FakePipeline does not implement ExecuteMultiPhaseAsync (replay tests are 2-side only).");
}
