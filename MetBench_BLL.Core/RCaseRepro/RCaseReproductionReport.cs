namespace MetBench_BLL.RCaseRepro;

/// <summary>
/// F9 R-Case 自动复现的输出报告。
/// </summary>
public sealed record RCaseReproductionReport(
    string KnownBugCode,
    int? KnownBugId,
    bool Reproduced,
    Guid? AnomalyId,
    Guid? ExecutionId,
    Guid? ResultId,
    double? SourceValue,
    double? FollowupValue,
    double? ObservedGap,
    double ThresholdUsed,
    string FinalStatus,
    string? FailureReason,
    string Summary);
