using LiteDB;

namespace MetBench_Domain;

/// <summary>
/// Batch 模板 — 定义"跑哪些 MR × SUT × params 组合"的可复用规格。
/// 可配 cron 定时触发（如 nightly regression）。
/// </summary>
/// <remarks>
/// DefinitionJson 例：
///   {
///     "mrBindings": "all" | [ids...] | { metaPattern: "m_mono" } | { application: "openmoc" },
///     "samplingSpec": { distribution: "fixed-list", values: [1.1, 1.25, 1.5] },
///     "hyperparams": { particles: 5000 }
///   }
///
/// 索引：Name 唯一。
/// </remarks>
public class BatchPlan
{
    [BsonId] public int IdPlan { get; set; }

    /// <summary>Plan 名（如 "nightly-regression"）。</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Plan 定义 JSON。</summary>
    public string DefinitionJson { get; set; } = "{}";

    /// <summary>cron 表达式（可空 = 仅手动触发）。</summary>
    public string? Schedule { get; set; }

    /// <summary>是否启用。</summary>
    public bool Enabled { get; set; } = true;
}
