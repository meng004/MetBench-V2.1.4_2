namespace MetBench_BLL.SystemMT.Jobs;

/// <summary>
/// 异步提交一个 MR 运行的请求。语义与 <c>ISystemMtLauncher.RunAsync</c> 的
/// <c>(mrId, parameterOverrides)</c> 对齐。
/// </summary>
public sealed record SystemMtJobRequest(
    string MrId,
    IReadOnlyDictionary<string, string>? ParameterOverrides = null,
    string? RuntimeBackendKey = null);
