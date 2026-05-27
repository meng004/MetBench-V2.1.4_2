namespace MetBench_BLL.SystemMT.Pipeline;

/// <summary>
/// MT Pipeline 编排器抽象 — 接受一个 PipelineContext，跑完整流水线，返回 PipelineOutcome。
/// </summary>
/// <remarks>
/// 不直接访问 LiteDB / 文件系统外的全局状态；测试可注入 mock 实现。
/// 默认实现 <see cref="SystemMtPipeline"/> 在 SUT runner / Python parser
/// 上通过 subprocess 调用。
/// </remarks>
public interface ISystemMtPipeline
{
    /// <summary>
    /// 串行跑流水线 9 个阶段。任意阶段失败：状态置 Error/Timeout，ErrorMessage 填充，
    /// 后续阶段跳过。
    /// </summary>
    /// <param name="context">运行时上下文。</param>
    /// <param name="progress">阶段切换回调（pipeline status string）；可空。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task<PipelineOutcome> ExecuteAsync(
        PipelineContext context,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// PR-Bol-2A: 多相 reference-convergence 流水线。串行执行 <c>mp.Phases</c> 中的每个相位
    /// (per-phase parameter overrides), 累积每相位的 metric 字典, 最后用 typed dispatcher
    /// 对 <c>mp.Base.TypedSpec</c> + <c>mp.Base.TypedPredicate</c> (launcher 预构建)
    /// 跑一次 <see cref="MetBench_BLL.SystemMT.Catalog.Typed.Runtime.PredicateDispatcher.Dispatch"/>.
    /// 不做字符串-代码分派 — typed spec 必须由 launcher 注入 <c>mp.Base</c>。
    /// </summary>
    /// <param name="mp">多相执行上下文。</param>
    /// <param name="progress">阶段切换回调；每相位前 emit <c>"running-phase:{role}"</c>。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task<PipelineOutcome> ExecuteMultiPhaseAsync(
        MultiPhaseExecutionContext mp,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);
}
