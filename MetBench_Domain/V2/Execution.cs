using LiteDB;

namespace MetBench_Domain;

/// <summary>
/// MT Pipeline 的一次执行 — 状态机记录 + 版本快照。
/// 一个 MRInstance 可被多次 Execution（用于 Replay 或不同时刻重跑）。
/// </summary>
/// <remarks>
/// 状态流转：
///   queued → parsing-source → transforming → writing-followup →
///   running-source → running-followup → parsing-outputs → asserting →
///   ok | anomaly | error | timeout | cancelled
///
/// 版本三元组（启动时锁定，不可后改）：
///   CatalogVersionSha   — Catalog (MR/SUT 定义) 当时的 git commit
///   SutVersionSnapshot  — SUT 当时自报的版本字符串
///   MetbenchVersion     — MetBench 程序集版本
///
/// Guid PK 因为高频生成 + 跨机器唯一。
/// 索引：MRInstanceId, BatchId, (Status, QueuedAt), TriggeredBy, SutVersionSnapshot。
/// </remarks>
public class Execution
{
    [BsonId] public Guid IdExecution { get; set; }

    /// <summary>→ MRInstances.IdInstance。</summary>
    public int MRInstanceId { get; set; }

    /// <summary>→ Batches.IdBatch（可空，单跑则 null）。</summary>
    public Guid? BatchId { get; set; }

    /// <summary>触发者（用户名 / "ci-bot" / "replay-of-{anomaly_id}"）。</summary>
    public string TriggeredBy { get; set; } = string.Empty;

    /// <summary>入队时间。</summary>
    public DateTime QueuedAt { get; set; }

    /// <summary>开始执行时间。</summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>完成时间。</summary>
    public DateTime? FinishedAt { get; set; }

    /// <summary>
    /// 当前状态：queued / parsing-source / transforming / writing-followup /
    ///          running-source / running-followup / parsing-outputs / asserting /
    ///          ok / anomaly / error / timeout / cancelled
    /// </summary>
    public string Status { get; set; } = "queued";

    /// <summary>启动时 catalog 的 git commit SHA。</summary>
    public string CatalogVersionSha { get; set; } = string.Empty;

    /// <summary>启动时 SUT 自报版本字符串副本。</summary>
    public string SutVersionSnapshot { get; set; } = string.Empty;

    /// <summary>启动时 MetBench 程序集版本。</summary>
    public string MetbenchVersion { get; set; } = string.Empty;

    /// <summary>本次 Execution 的 artifacts 文件夹路径（含源/后继输入输出 + stderr / stdout）。</summary>
    public string ArtifactsDirectory { get; set; } = string.Empty;

    /// <summary>失败时的错误消息（若有）。</summary>
    public string? ErrorMessage { get; set; }
}
