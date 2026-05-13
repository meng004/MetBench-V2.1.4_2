namespace MetBench_Domain;

/// <summary>
/// 嵌入式参数映射：把 MR 抽象参数名映射到 SUT 输入 dict 的具体字段路径。
/// 嵌入 MRBinding.ParameterMappings 列表。
/// </summary>
/// <remarks>
/// 例子：MR-T (RaiseFuelTemperature) 抽象参数 "fuel.temperature"
///   在 OpenMOC pin-cell 上：ConcreteFieldPath = "materials.fuel.temperature_kelvin"
///   在 MCNP 上：             ConcreteFieldPath = "card:m1::tmp"
///   在 SCALE 上：            ConcreteFieldPath = "block:materials:fuel::TMP"
/// PathSyntax 字段指定路径表达式的语法，由 Input Parser 解释。
/// </remarks>
public class ParameterMapping
{
    /// <summary>MR Schema 视角的抽象参数名，如 "fuel.temperature"。</summary>
    public string AbstractParamName { get; set; } = string.Empty;

    /// <summary>SUT input dict 中的具体字段路径。</summary>
    public string ConcreteFieldPath { get; set; } = string.Empty;

    /// <summary>字段类型："number" / "array" / "string"。</summary>
    public string FieldType { get; set; } = "number";

    /// <summary>物理合理取值范围。</summary>
    public ValueRange? ValueRange { get; set; }

    /// <summary>单位（"K" / "barn" / "cm" / "-"）。</summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>路径语法标识："json-pointer" / "mcnp-card" / "namelist-key" / ...</summary>
    public string PathSyntax { get; set; } = "json-pointer";

    /// <summary>映射说明（中文备注）。</summary>
    public string? Notes { get; set; }
}
