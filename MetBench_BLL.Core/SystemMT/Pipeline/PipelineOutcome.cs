using MetBench_BLL.SystemMT.Assertions;

namespace MetBench_BLL.SystemMT.Pipeline;

/// <summary>
/// Pipeline 跑完一次的结果摘要（Service 层用这个写 LiteDB Execution + Result + Anomaly）。
/// </summary>
public sealed record PipelineOutcome(
    string FinalStatus,                              // Ok / Anomaly / Error / Timeout
    string? ErrorMessage,
    DateTime StartedAt,
    DateTime FinishedAt,
    string ArtifactsDirectory,                       // 路径，含源/后继输入输出
    string SourceInputPath,
    string FollowupInputPath,
    string SourceOutputPath,
    string FollowupOutputPath,
    IReadOnlyDictionary<string, double>? SourceMetrics,
    IReadOnlyDictionary<string, double>? FollowupMetrics,
    SystemMtAssertionResultV2? AssertionResult,
    TimeSpan SourceElapsed,
    TimeSpan FollowupElapsed,
    int SourceExitCode,
    int FollowupExitCode);
