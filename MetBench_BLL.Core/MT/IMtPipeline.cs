namespace MetBench_BLL.MT;

/// <summary>
/// MT 管道的共享接口（method-level / system-level 共用）。
/// </summary>
/// <remarks>
/// 设计：mr-architecture.md §2 表 — 语义层 ✅ 共享、协议层 ✅ 共享、执行层分离。
/// 本接口是协议层契约，具体步骤由 method / system 各自实现，不强行共享步骤代码
/// （避免空架子模板方法）。
/// </remarks>
/// <typeparam name="TRequest">单次执行的请求形态（含 MR 信息 + SUT 句柄 + 源输入）。</typeparam>
/// <typeparam name="TOutcome">单次执行的结果形态（含断言 pass/fail + 两次输出 + 错误信息）。</typeparam>
public interface IMtPipeline<TRequest, TOutcome>
    where TRequest : class
    where TOutcome : class
{
    /// <summary>异步执行一次完整 MT 流程。</summary>
    Task<TOutcome> ExecuteAsync(TRequest request, CancellationToken cancellationToken = default);
}
