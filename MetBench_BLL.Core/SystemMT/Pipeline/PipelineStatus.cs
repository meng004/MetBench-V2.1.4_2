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

    /// <summary>
    /// PR-Bol-2A: 多相管线 <c>ExecuteMultiPhaseAsync</c> 在每个相位前 emit
    /// <c>"running-phase:{role}"</c>（如 <c>"running-phase:coarse"</c>）；此处常量是不带
    /// role 后缀的"裸"前缀，用于 dashboard / 日志按 <c>StartsWith</c> 分类。<see cref="All"/>
    /// 集合不包含 role-suffixed 字符串以保持有限性；多相进度字符串只用于显示，不参与状态机校验。
    /// </summary>
    public const string RunningPhase     = "running-phase";

    /// <summary>所有合法状态（校验用）。多相 <c>running-phase:&lt;role&gt;</c> 是显示级字符串、不入此集合。</summary>
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
