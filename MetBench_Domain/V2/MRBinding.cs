using LiteDB;

namespace MetBench_Domain;

/// <summary>
/// MR × SUT junction — 把抽象 MR (MetamorphicRelation) 绑定到具体 SUT (Application)。
/// 替代 v1 MetamorphicRelation.ApplicationName 多值字符串反模式。
/// </summary>
/// <remarks>
/// 一个 MetamorphicRelation 可以有 N 个 MRBinding（每个 SUT 一个）。
/// 每个 Binding 携带：
///   • ParameterMappings：把 MR 抽象参数名映射到 SUT 字段路径
///   • DefaultSampleCasePath：默认源案例文件
///   • DefaultTolerance / DefaultHyperparams：默认配置
///
/// 索引：(MRId, ApplicationId, DefaultSampleCasePath) 复合唯一。
/// </remarks>
public class MRBinding
{
    [BsonId] public int IdMRBinding { get; set; }

    /// <summary>→ MetamorphicRelations.IdMR (MR Schema)。</summary>
    public int MRId { get; set; }

    /// <summary>→ Applications.IdApplication (SUT)。</summary>
    public int ApplicationId { get; set; }

    /// <summary>
    /// MR 抽象参数 ↔ SUT 字段路径映射列表（嵌入）。
    /// 例：MR-T 在 OpenMOC 上的 mapping：
    ///   abstract: "fuel.temperature" → concrete: "materials.fuel.temperature_kelvin"
    /// </summary>
    public List<ParameterMapping> ParameterMappings { get; set; } = new();

    /// <summary>默认源案例文件路径（相对 repo root）。</summary>
    public string DefaultSampleCasePath { get; set; } = string.Empty;

    /// <summary>该 Binding 的默认容差配置。可在 MRInstance 上 override。</summary>
    public ToleranceConfig DefaultTolerance { get; set; } = new();

    /// <summary>该 Binding 的默认 SUT 超参。可在 MRInstance 上 override。</summary>
    public SutHyperparams DefaultHyperparams { get; set; } = new();

    /// <summary>是否启用（true = 列在 catalog；false = 历史保留）。</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>绑定时间。</summary>
    public DateTime BoundAt { get; set; } = DateTime.UtcNow;

    /// <summary>绑定者。</summary>
    public string BoundBy { get; set; } = string.Empty;
}
