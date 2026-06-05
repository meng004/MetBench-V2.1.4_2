using MetBench_BLL.SystemMT.Launcher;

namespace MetBench_BLL.SystemMT.Jobs;

/// <summary>
/// 异步 System MT 执行 facade（设计 spec §5）。<see cref="SubmitAsync"/> 立即返回 JobId；
/// 状态只能通过 <see cref="GetStatusAsync"/> polling（读 store 快照，spec §4 §7）。
/// §6 type-leakage：签名只含 primitives / Guid / 本命名空间 DTO / 既有 <see cref="MrRunResult"/>。
/// </summary>
public interface ISystemMtJobService
{
    Task<SystemMtJobHandle> SubmitAsync(SystemMtJobRequest request, CancellationToken cancellationToken = default);
    Task<SystemMtJobHandle> SubmitOperationAsync(SystemMtOperationJobRequest request, CancellationToken cancellationToken = default);
    Task<SystemMtJobStatus?> GetStatusAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task<MrRunResult?> GetResultAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task CancelAsync(Guid jobId, CancellationToken cancellationToken = default);
}
