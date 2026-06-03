using MetBench_BLL.SystemMT.Launcher;

namespace MetBench_BLL.SystemMT.Jobs;

/// <summary>
/// worker 调用它执行一个 job。v1 实现（<c>SystemMtAsyncPipeline</c>）委托既有
/// <c>ISystemMtLauncher.RunAsync</c>，复用验证过的同步路径（设计 spec §3 原则 1 / §12 兼容）。
/// </summary>
public interface ISystemMtAsyncPipeline
{
    Task<JobExecutionOutcome> ExecuteJobAsync(
        Guid jobId,
        SystemMtJobRequest request,
        IProgress<SystemMtJobProgress>? progress,
        CancellationToken cancellationToken);
}

/// <summary>worker 据此把状态机写入 store 的进度事件。</summary>
public sealed record SystemMtJobProgress(SystemMtJobState State, string Phase, int ProgressPercent);

/// <summary>
/// async pipeline 的最终产物。<see cref="FinalState"/> 必属终止态；
/// <see cref="SystemMtJobState.Succeeded"/> 时 <see cref="Result"/> 非空。
/// </summary>
public sealed record JobExecutionOutcome(
    SystemMtJobState FinalState,
    string SutName,
    MrRunResult? Result,
    string? FailureReason);
