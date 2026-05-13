using MetBench_Domain;

namespace MetBench_BLL.Discovery.Validators;

/// <summary>
/// EmpiricalValidator —— 在 N 个 baseline result 上看候选 MR 是否一致成立。
/// </summary>
/// <remarks>
/// 算法（粗）：
/// <list type="number">
///   <item>从 <c>Results</c> 表拉历史的 (source, followup) k_eff 对。</item>
///   <item>对每一对按 <c>candidate.SuggestedAssertionTypeCode</c> 判定。</item>
///   <item>通过率 ≥ <c>passThreshold</c>（默认 0.9）→ Passed=true。</item>
/// </list>
/// 取样来源 / assertion 判定逻辑通过 <see cref="EvaluatePair"/> 委托注入，
/// 让上层（VM / e2e 测试）可以替换为真实 pipeline；core 测试可直接传 fake。
/// </remarks>
public sealed class EmpiricalValidator : IMRValidator
{
    public string Name => "empirical";

    private readonly Func<CandidateMR, CancellationToken, Task<IReadOnlyList<EmpiricalSample>>> _sampler;
    private readonly double _passThreshold;

    public EmpiricalValidator(
        Func<CandidateMR, CancellationToken, Task<IReadOnlyList<EmpiricalSample>>> sampler,
        double passThreshold = 0.9)
    {
        _sampler = sampler;
        _passThreshold = passThreshold;
    }

    public async Task<ValidationOutcome> ValidateAsync(CandidateMR candidate, CancellationToken ct = default)
    {
        var samples = await _sampler(candidate, ct);
        if (samples.Count == 0)
            return new ValidationOutcome(false, "{\"reason\":\"no-samples\"}");

        var passed = samples.Count(s => s.AssertionHeld);
        var rate = (double)passed / samples.Count;
        var ok = rate >= _passThreshold;
        var json = $"{{\"samples\":{samples.Count},\"passed\":{passed},\"rate\":{rate:F3},\"threshold\":{_passThreshold:F2}}}";
        return new ValidationOutcome(ok, json);
    }
}

/// <summary>EmpiricalValidator 采样单元 —— 一个 baseline (source, followup) pair 的判定结果。</summary>
public sealed record EmpiricalSample(double SourceValue, double FollowupValue, bool AssertionHeld);
