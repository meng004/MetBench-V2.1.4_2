namespace MetBench_BLL.SystemMT.Anomaly;

using MetBench_BLL.SystemMT.Pipeline;

/// <summary>
/// 把一个失败的 <see cref="SystemMtResult"/>(W1) 或 <see cref="PipelineOutcome"/>(W2)
/// 分类成 Anomaly 的 severity / category。无状态；severity 阈值由调用方注入。
/// </summary>
public static class AnomalyClassifier
{
    // SourceValue 低于此绝对值视作 0，无法计算相对变化。
    private const double ZeroGuard = 1e-12;

    /// <summary>
    /// severity：runner 进程崩溃 → critical；否则按 |Δk%| 落 <see cref="AnomalySeverityThresholds"/>
    /// 定义的半开区间。SourceValue≈0 无法算相对变化 → 同样判 critical。
    /// </summary>
    public static string ClassifySeverity(SystemMtResult result, AnomalySeverityThresholds thresholds)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(thresholds);

        if (!result.SourceRun.Succeeded || !result.FollowUpRun.Succeeded)
        {
            return "critical";
        }

        var source = result.Assertion.SourceValue;
        if (Math.Abs(source) < ZeroGuard)
        {
            return "critical";
        }

        var deltaPercent = Math.Abs((result.Assertion.FollowUpValue - source) / source) * 100.0;
        return BucketByDeltaPercent(deltaPercent, thresholds);
    }

    /// <summary>
    /// category：runner 进程崩溃（source / follow-up 任一未正常退出）→ runner-failure；
    /// 否则（进程正常退出、是 MR 断言被违反）→ single-point。
    /// </summary>
    public static string ClassifyCategory(SystemMtResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.SourceRun.Succeeded && result.FollowUpRun.Succeeded
            ? "single-point"
            : "runner-failure";
    }

    /// <summary>
    /// W2 overload：分类一个 <see cref="PipelineOutcome"/>(由 launcher 经 pipeline 跑出)
    /// 的 severity。语义与 W1 重载等价 —— runner 进程退出非零 → critical;
    /// SourceValue≈0 → critical;否则按 |Δ%| 落桶。
    /// </summary>
    public static string ClassifySeverity(PipelineOutcome outcome, AnomalySeverityThresholds thresholds)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        ArgumentNullException.ThrowIfNull(thresholds);

        if (outcome.SourceExitCode != 0 || outcome.FollowupExitCode != 0)
        {
            return "critical";
        }

        var source = outcome.AssertionResult?.SourceValue ?? 0.0;
        if (Math.Abs(source) < ZeroGuard)
        {
            return "critical";
        }

        var followup = outcome.AssertionResult?.FollowupValue ?? 0.0;
        var deltaPercent = Math.Abs((followup - source) / source) * 100.0;
        return BucketByDeltaPercent(deltaPercent, thresholds);
    }

    /// <summary>
    /// W2 overload:runner 进程退出非零 → runner-failure;否则 single-point。
    /// </summary>
    public static string ClassifyCategory(PipelineOutcome outcome)
    {
        ArgumentNullException.ThrowIfNull(outcome);

        return outcome.SourceExitCode == 0 && outcome.FollowupExitCode == 0
            ? "single-point"
            : "runner-failure";
    }

    private static string BucketByDeltaPercent(double deltaPercent, AnomalySeverityThresholds thresholds)
    {
        if (deltaPercent < thresholds.NoiseMaxPercent) return "noise";
        if (deltaPercent < thresholds.MinorMaxPercent) return "minor";
        if (deltaPercent < thresholds.MajorMaxPercent) return "major";
        return "critical";
    }
}
