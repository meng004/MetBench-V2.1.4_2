namespace MetBench_BLL.RCaseRepro;

/// <summary>
/// F9 R-Case 自动复现的输入 spec。
/// </summary>
/// <remarks>
/// 用法：
/// <code>
/// var spec = new RCaseReproductionSpec(
///     KnownBugCode: "R-Case-4",
///     Context: builtPipelineContext,
///     DetectionThreshold: 0.05,    // 5% k_eff gap → declares reproduced
///     LinkAnomalyOnReproduction: true,
///     Actor: "auto-repro-cli");
/// var report = await service.ReproduceAsync(spec, ct);
/// </code>
/// 调用方负责构造 <see cref="PipelineContext"/>（通常用 m_cmp 配对 binding 跑同案例双 SUT）。
/// 服务负责：lookup 已知 bug + 跑一次 pipeline + 判定 + 落库 + 链接。
/// </remarks>
public sealed record RCaseReproductionSpec(
    string KnownBugCode,
    SystemMT.Pipeline.PipelineContext Context,
    double DetectionThreshold = 0.05,
    bool LinkAnomalyOnReproduction = true,
    string Actor = "auto-repro");
