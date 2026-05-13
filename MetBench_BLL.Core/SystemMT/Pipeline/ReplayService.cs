namespace MetBench_BLL.SystemMT.Pipeline;

/// <summary>
/// 异常重放服务 — 给定一个历史 PipelineOutcome（来自 Anomaly），用相同 context
/// 重跑 Pipeline，比较新旧结果，分类为：reproduced / fixed / flaky / mismatched。
/// </summary>
/// <remarks>
/// 调用方约定：
///   • 传入原 PipelineContext（含 catalog SHA + SUT 版本 — 服务层负责锁定）
///   • 传入原 PipelineOutcome（含原 assertion 判定 + 主值）
///   • 返回 ReplayResult，调用方据此更新 Anomaly.Status + ReplayCount
/// </remarks>
public sealed class ReplayService
{
    private readonly ISystemMtPipeline _pipeline;

    public ReplayService(ISystemMtPipeline pipeline)
    {
        _pipeline = pipeline;
    }

    public async Task<ReplayResult> ReplayAsync(
        PipelineContext context,
        PipelineOutcome originalOutcome,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var newOutcome = await _pipeline.ExecuteAsync(context, progress, cancellationToken)
            .ConfigureAwait(false);

        var classification = Classify(originalOutcome, newOutcome, context);
        return new ReplayResult(originalOutcome, newOutcome, classification);
    }

    private static ReplayClassification Classify(
        PipelineOutcome original, PipelineOutcome replay, PipelineContext ctx)
    {
        // 1. 跑挂了：用 NotComparable
        if (replay.FinalStatus is PipelineStatus.Error or PipelineStatus.Timeout
                                or PipelineStatus.Cancelled)
            return ReplayClassification.NotComparable;

        // 2. 假设 original 是 anomaly。若 replay 仍 anomaly + 主值一致 → reproduced；
        //    若 replay 通过 → 可能 fixed 或 flaky（依赖版本是否变）。
        var originalPassed = original.FinalStatus == PipelineStatus.Ok;
        var replayPassed = replay.FinalStatus == PipelineStatus.Ok;

        if (!originalPassed && !replayPassed)
        {
            // 两次都 anomaly — 比对主值
            if (NearEqual(original.FollowupMetrics, replay.FollowupMetrics, ctx.ValueName))
                return ReplayClassification.Reproduced;
            return ReplayClassification.MismatchedFailure;
        }

        if (!originalPassed && replayPassed)
        {
            // 原本失败，现在通过 — 是 fixed 还是 flaky
            return ReplayClassification.FixedOrFlaky;
        }

        if (originalPassed && !replayPassed)
        {
            // 原本通过，现在失败 — 倒退（regression-on-replay）
            return ReplayClassification.RegressionOnReplay;
        }

        // 两次都通过
        return ReplayClassification.StillPassing;
    }

    private static bool NearEqual(
        IReadOnlyDictionary<string, double>? a,
        IReadOnlyDictionary<string, double>? b,
        string valueName,
        double tol = 1e-6)
    {
        if (a is null || b is null) return false;
        if (!a.TryGetValue(valueName, out var av)) return false;
        if (!b.TryGetValue(valueName, out var bv)) return false;
        return Math.Abs(av - bv) <= tol;
    }
}

/// <summary>重放分类结果。</summary>
public enum ReplayClassification
{
    /// <summary>新旧都 anomaly + 主值一致 → 异常重现。</summary>
    Reproduced,

    /// <summary>原本失败，重放通过 — 上游 SUT 可能已修复或 flaky 单次抖动。</summary>
    FixedOrFlaky,

    /// <summary>原本通过，重放失败 — 检测到倒退。</summary>
    RegressionOnReplay,

    /// <summary>两次都通过（异常已自愈，应清掉 anomaly status）。</summary>
    StillPassing,

    /// <summary>新旧都失败但主值不同 — 现象不同。</summary>
    MismatchedFailure,

    /// <summary>重放跑挂了，无法对比。</summary>
    NotComparable,
}

public sealed record ReplayResult(
    PipelineOutcome OriginalOutcome,
    PipelineOutcome ReplayOutcome,
    ReplayClassification Classification);
