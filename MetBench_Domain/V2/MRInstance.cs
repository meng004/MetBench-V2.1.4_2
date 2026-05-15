using LiteDB;

namespace MetBench_Domain;

/// <summary>
/// MR 实例 (Level 4 in MR 4 级语义层次) — MRBinding 加具体参数值 + 采样策略 + SUT 超参 override，
/// 得到完整可执行配置。
/// </summary>
/// <remarks>
/// 一个 MRBinding 可以有多个 MRInstance：
///   • factor=1.5 + seed=42 + particles=5000 → 一个 instance
///   • factor=2.0 + seed=43 + particles=5000 → 另一个 instance
///   • Sampling.SampleCount=5 + Distribution=fixed-list → 一次 sweep 一个 instance
///
/// IsReusable=true 表示保存为可复用的实例（出现在 Scenarios 列表）；
/// IsReusable=false 表示一次性 ad-hoc 实例。
///
/// 索引：MRBindingId, (IsReusable, Name)。
/// </remarks>
public class MRInstance
{
    [BsonId] public int IdInstance { get; set; }

    /// <summary>→ MRBindings.IdMRBinding。</summary>
    public int MRBindingId { get; set; }

    /// <summary>
    /// 实际参数值（MR Transformation 的入参）。
    /// 例：{"factor": "1.5"}。
    /// </summary>
    public Dictionary<string, string> ParameterOverrides { get; set; } = new();

    /// <summary>
    /// 采样规格。仅在批量执行 / sweep 时设置；单次 ad-hoc 跑为 null。
    /// </summary>
    public SamplingSpec? Sampling { get; set; }

    /// <summary>SUT 超参 override（覆盖 MRBinding.DefaultHyperparams）。null = 用默认。</summary>
    public SutHyperparams? HyperparamsOverride { get; set; }

    /// <summary>容差 override（覆盖 MRBinding.DefaultTolerance）。null = 用默认。</summary>
    public ToleranceConfig? ToleranceOverride { get; set; }

    /// <summary>样例案例 override（覆盖 MRBinding.DefaultSampleCasePath）。null = 用默认。</summary>
    public string? SampleCaseOverridePath { get; set; }

    /// <summary>是否可复用（保存到 Scenarios 列表）；false = ad-hoc。</summary>
    public bool IsReusable { get; set; }

    /// <summary>可读名（reusable instance 必填）。</summary>
    public string? Name { get; set; }

    /// <summary>创建时间。</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>创建者。</summary>
    public string CreatedBy { get; set; } = string.Empty;
}
