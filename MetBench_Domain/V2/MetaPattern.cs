using LiteDB;

namespace MetBench_Domain;

/// <summary>
/// NOETHER 框架的 MetaPattern —— 蜕变测试 8 个元模式之一（含 4 个当前 SUT 适用 + 4 个 out-of-scope）。
/// </summary>
/// <remarks>
/// 之前以纯 string 形式散落在 <see cref="MetamorphicRelation.MetaPatternCode"/>、
/// <c>tools/noether_candidates.py</c> 内嵌 dict、`.feature` `@metapattern:` 标签里 ——
/// 出现命名漂移（cold-start finding C4）。本实体把 MetaPattern 升格为一等业务对象：
///   • LiteDB 24 个 collection 之一（与 MR/Binding/Anomaly 等并列）
///   • Code 作 string PK，MetamorphicRelation.MetaPatternCode 弱外键
///   • WPF / scaffold 工具 / noether sidecar 三方都读 DB
/// 索引：Code unique，Status，Block。
/// </remarks>
public class MetaPattern
{
    /// <summary>Code（如 "m_inv" / "m_mono" / ... / "m_rel"），string PK。</summary>
    [BsonId] public string Code { get; set; } = string.Empty;

    /// <summary>人类可读名（"Invariance" / "Monotonicity" / ...）。</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>NOETHER 框架的 block 标号（"B1-symmetry" / "B2-order" / ...）。</summary>
    public string Block { get; set; } = string.Empty;

    /// <summary>该 MetaPattern 下 MR 通用的默认断言类型代码（如 "greater" / "approx-invariant" / "bound"）。</summary>
    public string DefaultAssertionTypeCode { get; set; } = string.Empty;

    /// <summary>默认相对容忍度（用于 .feature `@tolerance_rel` tag 的初始值）。</summary>
    public double DefaultToleranceRel { get; set; }

    /// <summary>是否本质上 noise-aware（Monte Carlo 噪声需考虑）。</summary>
    public bool NoiseAware { get; set; }

    /// <summary>假设模板，方便 scaffold 生成 .feature 描述（如 "k_followup ≈ k_source within ε"）。</summary>
    public string HypothesisTemplate { get; set; } = string.Empty;

    /// <summary>典型物理参数路径示例（如 ["geometry.rotation_deg","geometry.mirror_axis"]）。</summary>
    public List<string> ExampleParamHints { get; set; } = new();

    /// <summary>状态：active / deprecated / experimental / out-of-scope。</summary>
    public string Status { get; set; } = "active";

    /// <summary>若 Status="out-of-scope"，给出原因（如 "static eigenvalue SUT has no trajectory"）。</summary>
    public string? OutOfScopeReason { get; set; }

    /// <summary>创建时间。</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>创建者（"seed" / "ci" / 用户名）。</summary>
    public string? CreatedBy { get; set; }
}
