using LiteDB;

namespace MetBench_Domain;

/// <summary>
/// 一组待执行的 MRInstance（手动 batch 或来自 BatchPlan）。
/// </summary>
/// <remarks>
/// 一个 Execution 通过 BatchId 引用所属 Batch（可空，单跑则 null）。
///
/// Status：queued / running / ok / failed / cancelled。
///
/// 索引：PlanId, (Status, CreatedAt)。
/// </remarks>
public class Batch
{
    [BsonId] public Guid IdBatch { get; set; }

    /// <summary>批次名（如 "nightly-regression-2026-05-13"）。</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>所属 BatchPlan → BatchPlans.IdPlan（可空，ad-hoc batch 则 null）。</summary>
    public int? PlanId { get; set; }

    /// <summary>创建时间。</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>创建者。</summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>状态：queued / running / ok / failed / cancelled。</summary>
    public string Status { get; set; } = "queued";
}
