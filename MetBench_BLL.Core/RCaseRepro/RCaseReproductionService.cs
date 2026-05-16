using MetBench_BLL.SystemMT.Pipeline;
using MetBench_Domain;
using MetBench_IDAL;

namespace MetBench_BLL.RCaseRepro;

/// <summary>
/// F9 — 自动复现 R-Case (已知 bug) 的端到端编排器。
/// </summary>
/// <remarks>
/// 论文核心 reproducibility 工具：用户给一份 <see cref="RCaseReproductionSpec"/>
/// → 服务跑一次 pipeline → 自动判定是否复现 → 若复现，写 Execution + Result + Anomaly
/// 并 link 到 KnownBug + 写 AuditLog。
///
/// 典型用法（R-Case-4 OpenMOC narrow basin @ ModSigmaA factor=1.5）：
/// <list type="number">
///   <item>调用方用 MR14-cmp 的 primary binding (openmoc) 构造 PipelineContext。</item>
///   <item>调 <see cref="ReproduceAsync"/>。</item>
///   <item>Pipeline 跑 openmoc + ModSigmaA(1.5)，得 k_eff ≈ 0.4764。</item>
///   <item>对比 partner SUT 已知值 0.9683（来自 ExtraAssertionValues 或 spec），gap ≈ 0.51。</item>
///   <item>0.51 > 0.05 阈值 → reproduced=true → 落 anomaly 链接 KnownBug R-Case-4。</item>
/// </list>
///
/// 设计取舍：服务**不**主动构造 PipelineContext（避免依赖 7-table join 逻辑）。
/// 调用方用 <see cref="ReplayContextBuilder"/> 或手工 build，给定 context；本服务专注编排 + 判定。
/// </remarks>
public sealed class RCaseReproductionService
{
    private readonly ISystemMtPipeline _pipeline;
    private readonly IKnownBugRepository _knownBugs;
    private readonly IExecutionRepository _executions;
    private readonly IResultRepository _results;
    private readonly IAnomalyRepository _anomalies;
    private readonly IAuditLogRepository _audit;

    public RCaseReproductionService(
        ISystemMtPipeline pipeline,
        IKnownBugRepository knownBugs,
        IExecutionRepository executions,
        IResultRepository results,
        IAnomalyRepository anomalies,
        IAuditLogRepository audit)
    {
        _pipeline = pipeline;
        _knownBugs = knownBugs;
        _executions = executions;
        _results = results;
        _anomalies = anomalies;
        _audit = audit;
    }

    /// <summary>
    /// 跑一次复现尝试。返回 <see cref="RCaseReproductionReport"/>。
    /// </summary>
    /// <exception cref="InvalidOperationException"><see cref="RCaseReproductionSpec.KnownBugCode"/> 不存在。</exception>
    public async Task<RCaseReproductionReport> ReproduceAsync(
        RCaseReproductionSpec spec,
        CancellationToken ct = default)
    {
        // ===== 1. lookup KnownBug =====
        var bug = _knownBugs.GetByCode(spec.KnownBugCode)
            ?? throw new InvalidOperationException(
                $"KnownBug with code '{spec.KnownBugCode}' not found.");

        // ===== 2. run pipeline =====
        PipelineOutcome outcome;
        try
        {
            outcome = await _pipeline.ExecuteAsync(spec.Context, progress: null, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            WriteAudit(spec.Actor, "r-case.error",
                bugCode: spec.KnownBugCode, bugId: bug.IdBug,
                detailsJson: $"{{\"reason\":\"pipeline-threw\",\"message\":\"{Escape(ex.Message)}\"}}");
            return new RCaseReproductionReport(
                KnownBugCode: spec.KnownBugCode, KnownBugId: bug.IdBug,
                Reproduced: false, AnomalyId: null, ExecutionId: null, ResultId: null,
                SourceValue: null, FollowupValue: null, ObservedGap: null,
                ThresholdUsed: spec.DetectionThreshold,
                FinalStatus: "error",
                FailureReason: ex.Message,
                Summary: $"Pipeline threw: {ex.Message}");
        }

        // ===== 3. extract values + compute gap =====
        var src = outcome.AssertionResult?.SourceValue
                  ?? FirstValueOrNull(outcome.SourceMetrics, spec.Context.ValueName);
        var flw = outcome.AssertionResult?.FollowupValue
                  ?? FirstValueOrNull(outcome.FollowupMetrics, spec.Context.ValueName);
        double? gap = (src.HasValue && flw.HasValue && Math.Abs(src.Value) > 1e-12)
            ? Math.Abs(flw.Value - src.Value) / Math.Abs(src.Value)
            : null;

        // ===== 4. detection =====
        var isAnomaly = outcome.FinalStatus == PipelineStatus.Anomaly
                        || (outcome.AssertionResult is { } ar && !ar.Passed)
                        || (gap.HasValue && gap.Value >= spec.DetectionThreshold);

        if (!isAnomaly)
        {
            WriteAudit(spec.Actor, "r-case.not-reproduced",
                bugCode: spec.KnownBugCode, bugId: bug.IdBug,
                detailsJson: $"{{\"gap\":{gap?.ToString("0.######") ?? "null"},\"threshold\":{spec.DetectionThreshold}}}");
            return new RCaseReproductionReport(
                KnownBugCode: spec.KnownBugCode, KnownBugId: bug.IdBug,
                Reproduced: false, AnomalyId: null, ExecutionId: null, ResultId: null,
                SourceValue: src, FollowupValue: flw, ObservedGap: gap,
                ThresholdUsed: spec.DetectionThreshold,
                FinalStatus: outcome.FinalStatus,
                FailureReason: null,
                Summary: $"Not reproduced. gap={gap?.ToString("P2") ?? "n/a"} < threshold {spec.DetectionThreshold:P0}.");
        }

        // ===== 5. persist Execution + Result =====
        var executionId = Guid.NewGuid();
        var resultId = Guid.NewGuid();

        _executions.Add(new Execution
        {
            IdExecution = executionId,
            QueuedAt = outcome.StartedAt,
            StartedAt = outcome.StartedAt,
            FinishedAt = outcome.FinishedAt,
            Status = outcome.FinalStatus,
            TriggeredBy = spec.Context.TriggeredBy,
            CatalogVersionSha = spec.Context.CatalogVersionSha,
            SutVersionSnapshot = spec.Context.SutVersionSnapshot,
            MetbenchVersion = spec.Context.MetbenchVersion,
        });

        _results.Add(new Result
        {
            IdResult = resultId,
            ExecutionId = executionId,
            SourceValue = src,
            FollowupValue = flw,
            SourceMetrics = outcome.SourceMetrics is not null
                ? new Dictionary<string, double>(outcome.SourceMetrics) : new(),
            FollowupMetrics = outcome.FollowupMetrics is not null
                ? new Dictionary<string, double>(outcome.FollowupMetrics) : new(),
            AssertionPassed = outcome.AssertionResult?.Passed ?? false,
            AssertionExpression = outcome.AssertionResult?.Expression ?? string.Empty,
            ObservedDelta = outcome.AssertionResult?.ObservedDelta,
            ExpectedThreshold = outcome.AssertionResult?.ExpectedThreshold,
            FailureReason = outcome.AssertionResult?.FailureReason ?? outcome.ErrorMessage,
            SourceElapsed = outcome.SourceElapsed,
            FollowupElapsed = outcome.FollowupElapsed,
            SourceExitCode = outcome.SourceExitCode,
            FollowupExitCode = outcome.FollowupExitCode,
        });

        // ===== 6. Anomaly + link to KnownBug =====
        Guid? anomalyId = null;
        if (spec.LinkAnomalyOnReproduction)
        {
            var anomaly = new MetBench_Domain.Anomaly
            {
                IdAnomaly = Guid.NewGuid(),
                ResultId = resultId,
                DiscoveredAt = DateTime.UtcNow,
                Severity = "major",
                Status = "confirmed-bug",
                Category = $"r-case-{spec.KnownBugCode}",
                LinkedKnownBugId = bug.IdBug,
                Notes = $"Auto-reproduced via RCaseReproductionService. gap={gap?.ToString("P2") ?? "n/a"}",
            };
            _anomalies.Add(anomaly);
            anomalyId = anomaly.IdAnomaly;
        }

        WriteAudit(spec.Actor, "r-case.reproduced",
            bugCode: spec.KnownBugCode, bugId: bug.IdBug,
            detailsJson: $"{{\"gap\":{gap?.ToString("0.######") ?? "null"},\"threshold\":{spec.DetectionThreshold},\"executionId\":\"{executionId}\",\"anomalyId\":\"{anomalyId}\"}}");

        return new RCaseReproductionReport(
            KnownBugCode: spec.KnownBugCode, KnownBugId: bug.IdBug,
            Reproduced: true,
            AnomalyId: anomalyId,
            ExecutionId: executionId,
            ResultId: resultId,
            SourceValue: src, FollowupValue: flw, ObservedGap: gap,
            ThresholdUsed: spec.DetectionThreshold,
            FinalStatus: outcome.FinalStatus,
            FailureReason: outcome.AssertionResult?.FailureReason ?? outcome.ErrorMessage,
            Summary: $"Reproduced {spec.KnownBugCode}. gap={gap?.ToString("P2") ?? "n/a"} ≥ threshold {spec.DetectionThreshold:P0}. " +
                     $"Anomaly={anomalyId} → linked to KnownBug.Id={bug.IdBug}.");
    }

    private static double? FirstValueOrNull(
        IReadOnlyDictionary<string, double>? metrics, string valueName)
        => metrics is not null && metrics.TryGetValue(valueName, out var v) ? v : null;

    private void WriteAudit(string actor, string action, string bugCode, int bugId, string detailsJson)
    {
        _audit.Add(new AuditLog
        {
            IdLog = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            Actor = actor,
            Action = action,
            TargetEntityType = "KnownBug",
            TargetEntityId = bugCode,
            DetailsJson = detailsJson,
        });
    }

    private static string Escape(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
