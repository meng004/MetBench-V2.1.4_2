namespace MetBench_BLL.SystemMT.Pipeline;

/// <summary>
/// MT Pipeline 状态枚举（与 LiteDB Execution.Status 字段字符串值对应）。
/// </summary>
/// <remarks>
/// 设计成字符串常量是为了与 LiteDB Execution.Status (string) 字段直接对接、
/// 且方便 dashboard / 日志显示。
///
/// 状态转移图：
///   Queued → ParsingSource → Transforming → WritingFollowup →
///   RunningSource → RunningFollowup → ParsingOutputs → Asserting →
///   Ok / Anomaly / Error / Timeout / Cancelled
/// </remarks>
public static class PipelineStatus
{
    public const string Queued           = "queued";
    public const string ParsingSource    = "parsing-source";
    public const string Transforming     = "transforming";
    public const string WritingFollowup  = "writing-followup";
    public const string RunningSource    = "running-source";
    public const string RunningFollowup  = "running-followup";
    public const string ParsingOutputs   = "parsing-outputs";
    public const string Asserting        = "asserting";
    public const string Ok               = "ok";
    public const string Anomaly          = "anomaly";
    public const string Error            = "error";
    public const string Timeout          = "timeout";
    public const string Cancelled        = "cancelled";

    /// <summary>所有合法状态（校验用）。</summary>
    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        Queued, ParsingSource, Transforming, WritingFollowup,
        RunningSource, RunningFollowup, ParsingOutputs, Asserting,
        Ok, Anomaly, Error, Timeout, Cancelled,
    };

    /// <summary>判断状态是否为终态。</summary>
    public static bool IsTerminal(string status) => status switch
    {
        Ok or Anomaly or Error or Timeout or Cancelled => true,
        _ => false
    };
}
