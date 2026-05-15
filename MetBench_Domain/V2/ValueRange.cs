using LiteDB;

namespace MetBench_Domain;

/// <summary>
/// 嵌入式数值范围（用于 ParameterMapping 等）。
/// 用于约束 MR 参数在 SUT 上的物理合理取值区间。
/// </summary>
public class ValueRange
{
    public double? Min { get; set; }
    public double? Max { get; set; }
}
