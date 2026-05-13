using LiteDB;

namespace MetBench_Domain;

/// <summary>
/// MR 识别方法的一次具体执行。
/// </summary>
/// <remarks>
/// 一次 DiscoveryRun 产出 0 或多个 CandidateMR；每个候选再进入 Validation 流程。
///
/// 索引：(MethodId, StartedAt), TargetApplicationId。
/// </remarks>
public class DiscoveryRun
{
    [BsonId] public Guid IdRun { get; set; }

    /// <summary>→ DiscoveryMethods.IdMethod。</summary>
    public int MethodId { get; set; }

    /// <summary>目标 SUT → Applications.IdApplication；null = 全局识别（未指定 SUT）。</summary>
    public int? TargetApplicationId { get; set; }

    /// <summary>开始时间。</summary>
    public DateTime StartedAt { get; set; }

    /// <summary>完成时间。</summary>
    public DateTime? FinishedAt { get; set; }

    /// <summary>状态：running / ok / error / cancelled。</summary>
    public string Status { get; set; } = "running";

    /// <summary>调用细节（命令行参数 / LLM prompt SHA / 等）。</summary>
    public string? InvocationDetails { get; set; }

    /// <summary>本次 Run 产出的候选数量（CandidateMRs 表里的行数缓存）。</summary>
    public int CandidatesProduced { get; set; }
}
