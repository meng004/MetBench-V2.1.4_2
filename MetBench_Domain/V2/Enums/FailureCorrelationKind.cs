namespace MetBench_Domain.V2.Enums;

/// <summary>
/// 失败相关性分类，V3 MR 5D tag 第 5 维（What failure modes does this MR detect?）。
/// </summary>
public enum FailureCorrelationKind
{
    /// <summary>未指定（v2 默认值）。</summary>
    Unspecified,
    /// <summary>未发现失败 / 未跑变异 campaign。</summary>
    None,
    /// <summary>检出真实缺陷（已封存入 KnownBug 表）。</summary>
    RealBug,
    /// <summary>检出变异体（mutation 杀死率高）。</summary>
    MutationKill,
    /// <summary>vacuous 假阳性（assertion 太松，注变异不死）。</summary>
    Vacuous,
}
