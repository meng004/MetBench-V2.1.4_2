using MetBench_BLL.SystemMT.Jobs;

namespace MetBench_SystemMT.Tests.SystemMT.Jobs;

/// <summary>
/// 确定性 fake async pipeline：按预设走 Succeeded / TimedOut / ArtifactMissing / Failed，
/// 可选 gate 卡住执行（用于 submit-不阻塞 / 取消 测试）。
/// </summary>
public sealed class FakeAsyncPipeline : ISystemMtAsyncPipeline
{
    private readonly SystemMtJobState _final;
    private readonly string _sut;
    private readonly string _mrId;
    private readonly bool _withResult;
    private readonly string? _failure;

    public TaskCompletionSource? Gate { get; }
    public int Invocations { get; private set; }

    private FakeAsyncPipeline(SystemMtJobState final, string sut, string mrId, bool withResult, string? failure, bool gated)
    {
        _final = final; _sut = sut; _mrId = mrId; _withResult = withResult; _failure = failure;
        Gate = gated ? new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously) : null;
    }

    public static FakeAsyncPipeline Succeeds(string mrId, string sut, bool gated = false)
        => new(SystemMtJobState.Succeeded, sut, mrId, withResult: true, null, gated);

    public static FakeAsyncPipeline TimesOut(string sut, string mrId = "mr")
        => new(SystemMtJobState.TimedOut, sut, mrId, withResult: false, "fake timeout", false);

    public static FakeAsyncPipeline ArtifactMissing(string sut, string mrId = "mr")
        => new(SystemMtJobState.ArtifactMissing, sut, mrId, withResult: false, "fake missing artifact", false);

    public static FakeAsyncPipeline Fails(string sut, string reason, string mrId = "mr")
        => new(SystemMtJobState.Failed, sut, mrId, withResult: false, reason, false);

    public async Task<JobExecutionOutcome> ExecuteJobAsync(
        Guid jobId, SystemMtJobRequest request,
        IProgress<SystemMtJobProgress>? progress, CancellationToken cancellationToken)
    {
        Invocations++;
        progress?.Report(new SystemMtJobProgress(SystemMtJobState.Preparing, "preparing", 10));
        if (Gate is not null) await Gate.Task.WaitAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        progress?.Report(new SystemMtJobProgress(SystemMtJobState.RunningSource, "running-source", 50));

        var result = _withResult ? JobsTestData.Result(_mrId, passed: true) : null;
        return new JobExecutionOutcome(_final, _sut, result, _failure);
    }
}
