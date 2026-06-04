using MetBench_BLL.SystemMT.Launcher;

namespace MetBench_BLL.SystemMT.Jobs;

/// <summary>
/// v1 async pipeline：委托既有 <c>ISystemMtLauncher.RunAsync</c>（复用验证过的同步路径，
/// 设计 spec §3 原则 1 / §12 兼容）。
/// </summary>
/// <remarks>
/// 实施偏差（接 §12.4 R3，已对照真实 <see cref="MrRunResult"/> 修正计划占位）：
/// <list type="number">
/// <item><see cref="MrRunResult"/> 无 SutName 字段 → SUT 名经
/// <c>ListAvailableAsync()</c> 按 MrId 查 <see cref="MrSummary.SutName"/> 解析；
/// 未知 MR 则留空字符串。</item>
/// <item><see cref="MrRunResult.Passed"/> 是 MR 断言位，**非**基础设施成功位。
/// <c>RunAsync</c> 返回即基础设施成功 → 终止态 <see cref="SystemMtJobState.Succeeded"/>，
/// MR 断言通过/失败由返回的 <see cref="MrRunResult"/> 自身承载（spec §10：断言失败是
/// 结果/异常调查路径，不是基础设施 Failed）。基础设施失败由 <c>RunAsync</c> 抛异常表达，
/// 在 <c>SystemMtJobWorker</c> 的 catch 中归类为 Failed —— 故本类不产生 Failed 分支。</item>
/// <item>v1 经 launcher 路径只可达 <see cref="SystemMtJobState.Succeeded"/>（或抛异常→Failed）。
/// TimedOut / ArtifactMissing 是为后续 <see cref="ISutExecutionBackend"/> 后端预留的状态，
/// v1 launcher 路径不产生。</item>
/// </list>
/// </remarks>
public sealed class SystemMtAsyncPipeline : ISystemMtAsyncPipeline
{
    private readonly ISystemMtLauncher _launcher;

    public SystemMtAsyncPipeline(ISystemMtLauncher launcher) => _launcher = launcher;

    public async Task<JobExecutionOutcome> ExecuteJobAsync(
        Guid jobId, SystemMtJobRequest request,
        IProgress<SystemMtJobProgress>? progress, CancellationToken cancellationToken)
    {
        progress?.Report(new SystemMtJobProgress(SystemMtJobState.Preparing, "preparing", 10));

        var sutName = await ResolveSutNameAsync(request.MrId, cancellationToken);

        progress?.Report(new SystemMtJobProgress(SystemMtJobState.RunningSource, "running-source", 40));

        // RunAsync 不抛即基础设施成功；抛异常（Python 失败 / 缺 SUT 文件等）由 worker catch → Failed。
        MrRunResult result = await _launcher.RunAsync(request.MrId, request.ParameterOverrides, cancellationToken);

        if (!result.Passed
            && result.FailureReason.StartsWith("Runtime preflight failed:", StringComparison.OrdinalIgnoreCase))
        {
            return new JobExecutionOutcome(SystemMtJobState.Failed, sutName, null, result.FailureReason);
        }

        progress?.Report(new SystemMtJobProgress(SystemMtJobState.Asserting, "asserting", 90));

        return new JobExecutionOutcome(SystemMtJobState.Succeeded, sutName, result, null);
    }

    private async Task<string> ResolveSutNameAsync(string mrId, CancellationToken cancellationToken)
    {
        var available = await _launcher.ListAvailableAsync(cancellationToken);
        foreach (var summary in available)
            if (string.Equals(summary.Id, mrId, StringComparison.Ordinal))
                return summary.SutName;
        return string.Empty;
    }
}
