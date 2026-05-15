namespace MetBench_BLL.Discovery;

/// <summary>
/// MR Discovery 引擎 —— 给定（可选）目标 SUT，产出一组候选 MR proposal。
/// </summary>
/// <remarks>
/// 两个内建实现：
/// <list type="bullet">
///   <item><c>MetaPatternDiscoverer</c> — 进程外调 Python <c>tools/noether_candidates.py</c>。</item>
///   <item><c>LlmNativeDiscoverer</c> — 依赖 <see cref="ILlmGateway"/>；prompt-engineering 由 Gateway 决定。</item>
/// </list>
/// 落库（DiscoveryRun + CandidateMR 行）由 <see cref="DiscoveryService"/> 统一负责，
/// 让 Discoverer 自身保持无状态、便于 unit-test。
/// </remarks>
public interface IMRDiscoverer
{
    /// <summary>Discoverer 唯一名（与 <c>DiscoveryMethods.Name</c> 对齐）。</summary>
    string Name { get; }

    /// <summary>触发一次发现。<paramref name="targetApplicationId"/> 为 null 表示全局/未限定 SUT。</summary>
    Task<DiscoveryRunOutcome> DiscoverAsync(int? targetApplicationId, CancellationToken ct = default);
}
