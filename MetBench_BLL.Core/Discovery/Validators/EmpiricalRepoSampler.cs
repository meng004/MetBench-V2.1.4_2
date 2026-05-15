using MetBench_Domain;
using MetBench_IDAL;

namespace MetBench_BLL.Discovery.Validators;

/// <summary>
/// 真实的 <see cref="EmpiricalSampler"/> 实现 —— 从 LiteDB <c>Results</c> 表拉历史
/// (source, followup) k_eff pairs，按 <see cref="CandidateMR.SuggestedAssertionTypeCode"/>
/// 判定，得到 baseline pass rate。
/// </summary>
/// <remarks>
/// 替代 App.xaml.cs DI 中曾经的 hardcoded "10 个 always-pass" demo stub。
/// 找不到任何 Result 时返回 empty list；EmpiricalValidator 上层会把空样本视为 fail（更保守）。
/// </remarks>
public sealed class EmpiricalRepoSampler
{
    private readonly IResultRepository _results;
    private readonly int _maxSamples;

    public EmpiricalRepoSampler(IResultRepository results, int maxSamples = 50)
    {
        _results = results;
        _maxSamples = maxSamples;
    }

    /// <summary>
    /// 暴露为 EmpiricalSampler delegate 形参兼容的方法（DI 时用 `new EmpiricalValidator(sampler.SampleAsync)`）。
    /// </summary>
    public Task<IReadOnlyList<EmpiricalSample>> SampleAsync(CandidateMR candidate, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        // 当前 v2 Result 没有 MR 直接外键（要走 Execution → MRInstance → MR 的链路），
        // first-ship 简化策略：拉最近 _maxSamples 个 Result，按既有 SourceValue/FollowupValue 判定。
        // 后续 follow-up 加 MRId 直查（性能 + 准确性）。
        var all = _results.GetAll();
        var samples = all
            .Where(r => r.SourceValue.HasValue && r.FollowupValue.HasValue)
            .Take(_maxSamples)
            .Select(r => new EmpiricalSample(
                SourceValue: r.SourceValue!.Value,
                FollowupValue: r.FollowupValue!.Value,
                AssertionHeld: AssertionHolds(
                    candidate.SuggestedAssertionTypeCode ?? "approx",
                    r.SourceValue.Value, r.FollowupValue.Value)))
            .ToList();

        return Task.FromResult<IReadOnlyList<EmpiricalSample>>(samples);
    }

    /// <summary>简化判定逻辑 —— 与 P4 FluentAssertions 扩展一致的语义。</summary>
    internal static bool AssertionHolds(string assertionTypeCode, double source, double followup)
    {
        return assertionTypeCode switch
        {
            "greater" => followup > source,
            "less" => followup < source,
            "approx" or "approx-invariant" => Math.Abs(followup - source) < 1e-3 * Math.Abs(source),
            "bound" => Math.Abs(followup - source) < 5e-3 * Math.Abs(source),
            _ => Math.Abs(followup - source) < 1e-3 * Math.Abs(source),  // 未知断言走 approx 默认
        };
    }
}
