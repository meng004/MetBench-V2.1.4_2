namespace MetBench_BLL.SystemMT.Anomaly;

/// <summary>
/// 把一个失败的 <see cref="SystemMtResult"/> 分类成 Anomaly 的 severity / category。
/// 无状态；severity 阈值由调用方注入。
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

        if (deltaPercent < thresholds.NoiseMaxPercent) return "noise";
        if (deltaPercent < thresholds.MinorMaxPercent) return "minor";
        if (deltaPercent < thresholds.MajorMaxPercent) return "major";
        return "critical";
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
}
