using MetBench_Domain;

namespace MetBench_BLL.Discovery.Validators;

/// <summary>
/// AdversarialMutmutValidator —— 注入一组 mutation，看候选 MR 能否检出。
/// </summary>
/// <remarks>
/// 防 "vacuous" MR：一个永远成立的 MR 在所有 mutant 上都不会失败，detection-rate=0
/// → 没有捕获能力 → 不应 promote。
///
/// 算法：
/// <list type="number">
///   <item>用 <see cref="_sampler"/> 委托拉一组 (mutant, assertionHeldUnderMutation) 元组。</item>
///   <item><c>detection_rate = #{not-held} / #{samples}</c>。</item>
///   <item>≥ <c>minDetectionRate</c>（默认 0.2）→ Passed=true。</item>
/// </list>
/// </remarks>
public sealed class AdversarialMutmutValidator : IMRValidator
{
    public string Name => "adversarial-mutmut";

    private readonly MutantProbeSampler _sampler;
    private readonly double _minDetectionRate;

    public AdversarialMutmutValidator(MutantProbeSampler sampler, double minDetectionRate = 0.2)
    {
        _sampler = sampler;
        _minDetectionRate = minDetectionRate;
    }

    public async Task<ValidationOutcome> ValidateAsync(CandidateMR candidate, CancellationToken ct = default)
    {
        var samples = await _sampler(candidate, ct);
        if (samples.Count == 0)
            return new ValidationOutcome(false, "{\"reason\":\"no-mutants\"}");

        var detected = samples.Count(s => !s.AssertionStillHolds);
        var rate = (double)detected / samples.Count;
        var ok = rate >= _minDetectionRate;
        var json = $"{{\"mutants\":{samples.Count},\"detected\":{detected},\"detectionRate\":{rate:F3},\"minRequired\":{_minDetectionRate:F2}}}";
        return new ValidationOutcome(ok, json);
    }
}

/// <summary>AdversarialMutmutValidator 采样单元 —— 一个 mutant 应用后该候选 MR 的成立状态。</summary>
public sealed record MutantProbe(int MutantId, bool AssertionStillHolds);

/// <summary>
/// AdversarialMutmutValidator 采样委托 —— 拉一组 (mutant, assertion-still-holds) 元组。
/// </summary>
public delegate Task<IReadOnlyList<MutantProbe>> MutantProbeSampler(
    CandidateMR candidate, CancellationToken ct);
