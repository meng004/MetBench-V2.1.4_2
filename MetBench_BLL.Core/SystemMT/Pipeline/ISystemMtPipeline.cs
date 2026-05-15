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
}
