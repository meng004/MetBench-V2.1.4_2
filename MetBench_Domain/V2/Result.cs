using LiteDB;

namespace MetBench_Domain;

/// <summary>
/// Execution 的数值产物 + 断言判定。
/// 一个 Execution 对应一个 Result（1:1）。
/// </summary>
/// <remarks>
/// SourceMetrics / FollowupMetrics 是 Output Parser 解析的完整字典（含 raw value + std + 其他衍生量）；
/// SourceValue / FollowupValue 是 MR.ValueName 字段提取出的主值，便于查询和报告。
///
/// 索引：ExecutionId 唯一, AssertionPassed。
/// </remarks>
public class Result
{
    [BsonId] public Guid IdResult { get; set; }

    /// <summary>→ Executions.IdExecution。</summary>
    public Guid ExecutionId { get; set; }

    /// <summary>主断言值的 source 值（MR.ValueName 字段）。</summary>
    public double? SourceValue { get; set; }

    /// <summary>主断言值的 source σ（确定性 SUT 为 0）。</summary>
    public double? SourceStd { get; set; }

    /// <summary>主断言值的 followup 值。</summary>
    public double? FollowupValue { get; set; }

    /// <summary>主断言值的 followup σ。</summary>
    public double? FollowupStd { get; set; }

    /// <summary>源案例的完整解析字典（含 raw value + 其他衍生量）。</summary>
    public Dictionary<string, double> SourceMetrics { get; set; } = new();

    /// <summary>后继案例的完整解析字典。</summary>
    public Dictionary<string, double> FollowupMetrics { get; set; } = new();

    /// <summary>断言是否通过。</summary>
    public bool AssertionPassed { get; set; }

    /// <summary>
    /// 人类可读的断言表达式（含具体数值）。
    /// 例："0.50781 < 1.13306 - max(0.0, 0.0)"
    /// </summary>
    public string AssertionExpression { get; set; } = string.Empty;

    /// <summary>观察到的 Δ 值（followup - source）。</summary>
    public double? ObservedDelta { get; set; }

    /// <summary>断言期望的阈值（如 noise floor）。</summary>
    public double? ExpectedThreshold { get; set; }

    /// <summary>失败原因（来自 FluentAssertions AssertionFailedException.Message）。</summary>
    public string? FailureReason { get; set; }

    /// <summary>源案例 SUT 执行耗时。</summary>
    public TimeSpan SourceElapsed { get; set; }

    /// <summary>后继案例 SUT 执行耗时。</summary>
    public TimeSpan FollowupElapsed { get; set; }

    /// <summary>源案例 SUT 退出码。</summary>
    public int SourceExitCode { get; set; }

    /// <summary>后继案例 SUT 退出码。</summary>
    public int FollowupExitCode { get; set; }
}
