namespace MetBench_BLL.MethodMT;

/// <summary>
/// 方法级 MT 单次执行请求。
/// </summary>
/// <remarks>
/// 与系统级 <c>PipelineContext</c>（BLL.Core）对称但更轻量：method MT SUT 是 C# 委托
/// （<see cref="SutFunction"/>），不走进程 / CLI / 文件 IO；变换在内存 dict 上完成。
/// </remarks>
public sealed record MethodMtRunRequest(
    string MrCode,
    string EquationKey,
    string TransformationName,
    string TargetFieldPath,
    IReadOnlyDictionary<string, string> Parameters,
    string AssertionTypeCode,
    string ValueName,
    IReadOnlyDictionary<string, object?> SourceInput,
    Func<IReadOnlyDictionary<string, object?>, IReadOnlyDictionary<string, object?>> SutFunction,
    double Tolerance = 1e-9);
