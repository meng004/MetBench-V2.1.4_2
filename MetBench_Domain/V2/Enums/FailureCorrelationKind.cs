namespace MetBench_Domain.V2.Enums;

/// <summary>
/// 失败相关性分类，V3 MR 5D tag 第 5 维（What failure modes does this MR detect?）。
/// </summary>
/// <remarks>**APPEND-ONLY**：LiteDB BsonMapper 默认按底层 int 序列化；重排或插值会让历史行映射错误成员（Stage 8 P5 review）。新成员只追加末尾，并配 <c>V3EnumStabilityPinningTests</c> 守护。</remarks>
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
