using MetBench_Domain;
using MetBench_IDAL;

namespace MetBench_BLL.Discovery;

/// <summary>
/// 把 NOETHER 框架 8 个 MetaPattern 的"权威定义"灌入 LiteDB。
/// </summary>
/// <remarks>
/// 跟 <c>tools/noether_candidates.py</c> 中内嵌的 METAPATTERN_TABLE 字典等价。
/// 启动时调用一次 <see cref="EnsureSeeded"/>：若 collection 为空 → 全量灌入；
/// 若已存在 → 不动（避免覆盖用户在 UI 中调整过的字段）。
///
/// 修改方式：
///   • 加新 MetaPattern: 在下面 <see cref="CanonicalSet"/> 加一条。
///   • 改既有: 不要改这里，从 WPF / Repository.Modify 来改 —— 这里是初始化。
/// </remarks>
public static class MetaPatternSeed
{
    /// <summary>NOETHER 框架 8 个 MetaPattern 权威定义（含 4 active + 4 out-of-scope）。</summary>
    public static readonly IReadOnlyList<MetaPattern> CanonicalSet = new[]
    {
        new MetaPattern
        {
            Code = "m_inv", Name = "Invariance", Block = "B1-symmetry",
            DefaultAssertionTypeCode = "approx-invariant", DefaultToleranceRel = 0.0001,
            NoiseAware = false,
            HypothesisTemplate = "k_followup ≈ k_source within ε (group-action invariance)",
            ExampleParamHints = new() { "geometry.rotation_deg", "geometry.mirror_axis", "physics.energy_group_permutation" },
            Status = "active",
            CreatedBy = "seed",
        },
        new MetaPattern
        {
            Code = "m_mono", Name = "Monotonicity", Block = "B2-order",
            DefaultAssertionTypeCode = "greater", DefaultToleranceRel = 0.0,
            NoiseAware = false,
            HypothesisTemplate = "k_followup > k_source (parameter increase → k monotone direction)",
            ExampleParamHints = new() { "physics.fuel.nu_sigma_f", "physics.fuel.sigma_a", "physics.fuel.temperature" },
            Status = "active",
            CreatedBy = "seed",
        },
        new MetaPattern
        {
            Code = "m_conv", Name = "Convergence", Block = "B5-limit",
            DefaultAssertionTypeCode = "bound", DefaultToleranceRel = 0.005,
            NoiseAware = true,
            HypothesisTemplate = "|k_finer - k_coarser| → 0 as discretisation refines",
            ExampleParamHints = new() { "numerics.particles", "numerics.num_azim", "numerics.batches" },
            Status = "active",
            CreatedBy = "seed",
        },
        new MetaPattern
        {
            Code = "m_cmp", Name = "Method-Comparison", Block = "B7-cross-program",
            DefaultAssertionTypeCode = "bound", DefaultToleranceRel = 0.005,
            NoiseAware = true,
            HypothesisTemplate = "|k_A - k_B| < 3·σ_MC + ε_method (two solvers agree on same case)",
            ExampleParamHints = new() { "solver.OpenMOC_vs_OpenMC", "solver.scattering_order" },
            Status = "active",
            CreatedBy = "seed",
        },
        new MetaPattern
        {
            Code = "m_adj", Name = "Self-adjoint", Block = "B3-self-adjoint",
            DefaultAssertionTypeCode = "approx-invariant", DefaultToleranceRel = 0.0001,
            NoiseAware = false,
            HypothesisTemplate = "k_adjoint == k_forward within ε (operator self-adjointness)",
            ExampleParamHints = new() { "solver.adjoint_mode" },
            Status = "out-of-scope",
            OutOfScopeReason = "Adjoint solve not wired into current SUT runners; will reopen once exposed.",
            CreatedBy = "seed",
        },
        new MetaPattern
        {
            Code = "m_rev", Name = "Time-reversal", Block = "B4-time-reversal",
            DefaultAssertionTypeCode = "approx-invariant", DefaultToleranceRel = 0.0001,
            NoiseAware = false,
            HypothesisTemplate = "(state evolution → reversal → original) within ε",
            ExampleParamHints = new(),
            Status = "out-of-scope",
            OutOfScopeReason = "Steady-state static eigenvalue SUT has no time evolution; empty.",
            CreatedBy = "seed",
        },
        new MetaPattern
        {
            Code = "m_dyn", Name = "Qualitative-dynamics", Block = "B6-qualitative-dyn",
            DefaultAssertionTypeCode = "bound", DefaultToleranceRel = 0.0,
            NoiseAware = false,
            HypothesisTemplate = "Qualitative trajectory shape preserved under transform",
            ExampleParamHints = new(),
            Status = "out-of-scope",
            OutOfScopeReason = "Static eigenvalue SUT has no trajectory; no dynamics MetaPattern realizable.",
            CreatedBy = "seed",
        },
        new MetaPattern
        {
            Code = "m_rel", Name = "Idempotent-rewriting", Block = "B8-idempotent",
            DefaultAssertionTypeCode = "approx-invariant", DefaultToleranceRel = 0.0,
            NoiseAware = false,
            HypothesisTemplate = "f(f(x)) == f(x) (idempotent semantic equality)",
            ExampleParamHints = new(),
            Status = "out-of-scope",
            OutOfScopeReason = "Pin-cell physics has no idempotent-semiring rewriting structure.",
            CreatedBy = "seed",
        },
    };

    /// <summary>
    /// 若 LiteDB 中 MetaPatterns collection 为空 → 灌入全部 <see cref="CanonicalSet"/>；
    /// 否则 no-op（不覆盖既有数据）。
    /// </summary>
    /// <returns>本次新插入的行数（0 = 已存在）。</returns>
    public static int EnsureSeeded(IMetaPatternRepository repo)
    {
        if (repo.Count() > 0) return 0;
        var inserted = 0;
        foreach (var mp in CanonicalSet)
        {
            repo.Add(mp);
            inserted++;
        }
        return inserted;
    }
}
