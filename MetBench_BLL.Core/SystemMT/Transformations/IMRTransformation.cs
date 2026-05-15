namespace MetBench_BLL.SystemMT.Transformations;

/// <summary>
/// MR 输入变换抽象 — 在内存 dict 上把 source 变成 followup，不接触文件 IO。
/// </summary>
/// <remarks>
/// 设计原则（按 docs/design/v2-system-mt-architecture.md §3.2）：
/// <list type="bullet">
///   <item>不知道 SUT 原生文件格式（Parser 负责）。</item>
///   <item>不知道字段如何映射（ParameterMapping 负责，由 Pipeline 解析后传入 targetFieldPath）。</item>
///   <item>仅做"按路径拿值 → 应用变换 → 按路径写回"的纯数据操作。</item>
/// </list>
///
/// 实现类应该无状态（线程安全），通过 Name 字符串从 <see cref="ITransformationRegistry"/> 解析。
/// </remarks>
public interface IMRTransformation
{
    /// <summary>变换名（如 "ScaleField"）；对应 MetamorphicRelation.TransformationName 字段。</summary>
    string Name { get; }

    /// <summary>
    /// 参数 JSON Schema（描述 Apply 接受的参数；用于 UI 表单 / catalog 校验）。
    /// 例：{"factor": "positive number"} 或 {"axis": "x|y", "fields": "string array"}。
    /// </summary>
    string ParametersSchema { get; }

    /// <summary>
    /// 在内存 dict 上应用变换。
    /// </summary>
    /// <param name="sourceData">源案例的结构化数据（已由 Input Parser 解析）。</param>
    /// <param name="targetFieldPath">已由 ParameterMapping 解析的具体字段路径。</param>
    /// <param name="parameters">变换参数（如 {"factor": "1.5"}）。</param>
    /// <returns>变换后的字典（应返回新对象，不要 mutate sourceData）。</returns>
    Dictionary<string, object?> Apply(
        IReadOnlyDictionary<string, object?> sourceData,
        string targetFieldPath,
        IReadOnlyDictionary<string, string> parameters);
}
