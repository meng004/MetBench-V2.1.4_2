using MetBench_BLL.Equations;
using MetBench_BLL.SystemMT.Assertions;

namespace MetBench_BLL.SystemMT.Pipeline;

/// <summary>
/// Pipeline 单次执行的运行时上下文 — 把 MR/SUT/Parser/参数等聚合在一处，
/// 避免 SystemMtPipeline 方法签名爆炸。
/// </summary>
/// <remarks>
/// Pipeline 实现 NOT 应该直接访问 LiteDB；这些字段由调用方（Service 层）
/// 预先准备并传入。这样 Pipeline 可在测试中用 mock 数据直接驱动。
/// </remarks>
public sealed record PipelineContext(
    // === MR 信息 ===
    string MrCode,                                      // "MR-T"
    string TransformationName,                          // "ScaleField"
    string AssertionTypeCode,                           // "less-noise-aware"
    string ValueName,                                   // "k_eff"
    string TargetFieldPath,                             // 已由 ParameterMapping 解析的路径
    string PathSyntax,                                  // "json-pointer" / "mcnp-card" / ...
    IReadOnlyDictionary<string, string> Parameters,     // {"factor": "1.5"}
    AssertionTolerance Tolerance,
    IReadOnlyDictionary<string, double>? ExtraAssertionValues, // 用于 variance-ratio / cross-program-agree

    // === SUT 信息 ===
    string SutName,                                     // "openmoc"
    string SourceCasePath,                              // 输入文件路径
    string WorkingDirectory,                            // pipeline 临时工作目录
    string InputParserCommand,                          // 用于 subprocess 调用 Python parser
    string OutputParserCommand,
    string RunnerCommand,                               // 用于调用 SUT runner
    int TimeoutSeconds,

    // === 版本快照（启动时锁定）===
    string CatalogVersionSha,
    string SutVersionSnapshot,
    string MetbenchVersion,
    string TriggeredBy
)
{
    /// <summary>
    /// 所属方程的稳定业务键（如 <c>bateman</c>）；空字符串表示无关联方程。
    /// 供 TransformationResolver 决策 B 查找方程命名空间函数（P4 L1 Recipe 支持）。
    /// </summary>
    public string EquationKey { get; init; } = string.Empty;

    /// <summary>
    /// 方程函数注册表（可选）；若提供，pipeline 使用 TransformationResolver 解析变换，
    /// 可命中 L1 Recipe。未提供时退回全局 TransformationRegistry。
    /// </summary>
    public EquationFunctionRegistry? EquationFunctionRegistry { get; init; }
}
