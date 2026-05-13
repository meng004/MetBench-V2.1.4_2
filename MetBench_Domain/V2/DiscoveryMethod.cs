using LiteDB;

namespace MetBench_Domain;

/// <summary>
/// MR 识别方法定义 — IMRDiscoverer 实现的注册条目。
/// </summary>
/// <remarks>
/// Day-1 实例：
///   • Name="MetaPattern-Structural", Version="NOETHER-v1"
///     基于 8 个 MetaPattern 模板 × SUT.InputParameters 枚举式实例化。
///   • Name="LLM-Native", Version="deepseek-v4-pro@2026-05"
///     调用 LLM API，给 SUT 文档/接口 schema，让模型提议 MR。
///
/// 未来扩展：
///   • Symmetry-Static (基于 AST 分析的对称性挖掘)
///   • Test-Suite-Mining (从 SUT 测试套件挖掘)
///   • Domain-Rules (领域专家规则)
///
/// 索引：(Name, Version) 唯一。
/// </remarks>
public class DiscoveryMethod
{
    [BsonId] public int IdMethod { get; set; }

    /// <summary>方法名（如 "MetaPattern-Structural" / "LLM-Native"）。</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>方法版本（如 "NOETHER-v1" / "deepseek-v4-pro@2026-05"）。</summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>方法特定参数 JSON（如 LLM model、prompt template、温度等）。</summary>
    public string ConfigJson { get; set; } = "{}";

    /// <summary>是否启用。</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>方法描述。</summary>
    public string? Description { get; set; }
}
