using LiteDB;

namespace MetBench_Domain;

/// <summary>
/// 异常样本 — Result 中 AssertionPassed=false 的子集，进入异常调查工作流。
/// </summary>
/// <remarks>
/// 一个 Result 至多对应一个 Anomaly（0:1）。
///
/// Severity 分类：
///   noise    — |Δk%| &lt; 1%，MC 噪声底相关；不一定是 bug
///   minor    — |Δk%| 1-10%
///   major    — |Δk%| 10-50%
///   critical — |Δk%| &gt; 50% 或 SUT 崩溃
///
/// Category（自动分类，启发式）：
///   basin            — 单点严重偏离（如 Case 4 / Case 6 OpenMOC convergence basin）
///   mc-floor         — MC 噪声底以下的小偏差
///   cross-program    — 同 MR 在两 SUT 上结果分歧
///   single-point     — 单参数 sliver 触发（进程正常退出、MR 断言被违反）
///   runner-failure   — SUT runner 进程崩溃（非 MR 反例，进程未正常退出）
///   legacy           — 从历史 SystemMtResultRecord 迁入
///
/// Status 状态机：
///   new → investigating → (known | confirmed-bug | false-positive | fixed-upstream)
///
/// 索引：ResultId 唯一, (Status, Severity), LinkedKnownBugId, Category。
/// </remarks>
public class Anomaly
{
    [BsonId] public Guid IdAnomaly { get; set; }

    /// <summary>→ Results.IdResult。</summary>
    public Guid ResultId { get; set; }

    /// <summary>严重度：noise / minor / major / critical。</summary>
    public string Severity { get; set; } = "minor";

    /// <summary>分类：basin / mc-floor / cross-program / single-point / runner-failure / legacy / ...</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>该 Anomaly 被 Replay 的次数。</summary>
    public int ReplayCount { get; set; }

    /// <summary>调查状态（LiteDB 存 int；kebab 映射见 <see cref="AnomalyStatuses"/>）。</summary>
    public AnomalyStatus Status { get; set; } = AnomalyStatus.New;

    /// <summary>研究者分析笔记（Markdown）。</summary>
    public string? Notes { get; set; }

    /// <summary>已链已知 bug → KnownBugs.IdBug。</summary>
    public int? LinkedKnownBugId { get; set; }

    /// <summary>首次发现时间。</summary>
    public DateTime DiscoveredAt { get; set; } = DateTime.UtcNow;

    /// <summary>首次发现者。</summary>
    public string? DiscoveredBy { get; set; }

    /// <summary>最后一次 Replay 的时间。</summary>
    public DateTime? LastReplayedAt { get; set; }
}
