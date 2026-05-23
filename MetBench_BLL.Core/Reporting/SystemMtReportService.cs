using MetBench_BLL.Coverage;
using MetBench_Domain;
using MetBench_IDAL;

namespace MetBench_BLL.Reporting;

/// <summary>
/// 4 Scope 报告生成 —— execution / anomaly / mutation-campaign / coverage。
/// （Weekly scope 已删除，随 Trend 子系统于 next-stage P0 一并下线。）
/// </summary>
/// <remarks>
/// 生成 markdown 文本（不写实际 HTML，由 <c>HtmlSystemMtResultReportRenderer</c> 后处理）+ 写 Report 表。
/// </remarks>
public sealed class SystemMtReportService
{
    public const string ScopeExecution = "single-execution";
    public const string ScopeAnomaly = "anomaly";
    public const string ScopeMutationCampaign = "single-campaign";
    public const string ScopeCoverage = "coverage";

    private readonly IReportRepository _reports;
    private readonly IExecutionRepository _executions;
    private readonly IAnomalyRepository _anomalies;
    private readonly IMutationCampaignRepository _campaigns;
    private readonly IMutationResultRepository _mutationResults;

    public SystemMtReportService(
        IReportRepository reports,
        IExecutionRepository executions,
        IAnomalyRepository anomalies,
        IMutationCampaignRepository campaigns,
        IMutationResultRepository mutationResults)
    {
        _reports = reports;
        _executions = executions;
        _anomalies = anomalies;
        _campaigns = campaigns;
        _mutationResults = mutationResults;
    }

    public ReportRenderResult GenerateExecution(Guid executionId, string contentPath)
    {
        var exec = _executions.Get(executionId)
            ?? throw new InvalidOperationException($"Execution {executionId} not found");
        var content = BuildExecutionMarkdown(exec);
        return Persist(ScopeExecution, executionId.ToString(), contentPath, content);
    }

    public ReportRenderResult GenerateAnomaly(Guid anomalyId, string contentPath)
    {
        var anomaly = _anomalies.Get(anomalyId)
            ?? throw new InvalidOperationException($"Anomaly {anomalyId} not found");
        var content = BuildAnomalyMarkdown(anomaly);
        return Persist(ScopeAnomaly, anomalyId.ToString(), contentPath, content);
    }

    public ReportRenderResult GenerateMutationCampaign(Guid campaignId, string contentPath)
    {
        var campaign = _campaigns.Get(campaignId)
            ?? throw new InvalidOperationException($"Campaign {campaignId} not found");
        var cells = _mutationResults.GetByCampaign(campaignId).ToList();
        var content = BuildMutationMarkdown(campaign, cells);
        return Persist(ScopeMutationCampaign, campaignId.ToString(), contentPath, content);
    }

    public ReportRenderResult GenerateCoverage(CoverageReport report, string contentPath)
    {
        var content = BuildCoverageMarkdown(report);
        return Persist(ScopeCoverage,
            scopeRefId: report.GeneratedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            contentPath: contentPath, content: content);
    }

    private ReportRenderResult Persist(string scope, string? scopeRefId, string contentPath, string content)
    {
        File.WriteAllText(contentPath, content);
        var report = new Report
        {
            IdReport = Guid.NewGuid(),
            GeneratedAt = DateTime.UtcNow,
            Scope = scope,
            ScopeRefId = scopeRefId,
            Format = Path.GetExtension(contentPath).TrimStart('.').ToLowerInvariant(),
            ContentPath = contentPath,
        };
        _reports.Add(report);
        return new ReportRenderResult(report.IdReport, scope, contentPath, content);
    }

    internal static string BuildExecutionMarkdown(Execution e) => $"""
        # Execution Report
        - id: {e.IdExecution}
        - MRInstance: {e.MRInstanceId}
        - Status: {e.Status}
        - Triggered by: {e.TriggeredBy}
        - Queued: {e.QueuedAt:yyyy-MM-dd HH:mm:ss}Z
        - Finished: {e.FinishedAt:yyyy-MM-dd HH:mm:ss}Z
        - SUT version: {e.SutVersionSnapshot}
        - MetBench version: {e.MetbenchVersion}
        """;

    internal static string BuildAnomalyMarkdown(MetBench_Domain.Anomaly a) => $"""
        # Anomaly Report
        - id: {a.IdAnomaly}
        - Severity: {a.Severity}
        - Category: {a.Category}
        - Status: {a.Status}
        - Discovered: {a.DiscoveredAt:yyyy-MM-dd HH:mm:ss}Z
        - Linked KnownBug: {(a.LinkedKnownBugId?.ToString() ?? "none")}
        - Notes: {a.Notes}
        """;

    internal static string BuildMutationMarkdown(MutationCampaign c, IReadOnlyList<MutationResult> cells)
    {
        var detected = cells.Count(x => x.Outcome == "detected");
        var missed = cells.Count(x => x.Outcome == "missed");
        var errors = cells.Count(x => x.Outcome == "error");
        var na = cells.Count(x => x.Outcome == "not-affected");
        var rate = (detected + missed) == 0 ? 0.0 : (double)detected / (detected + missed);
        return $"""
            # Mutation Campaign — {c.Name}
            - id: {c.IdCampaign}
            - Status: {c.Status}
            - Started: {c.StartedAt:yyyy-MM-dd HH:mm:ss}Z
            - Finished: {c.FinishedAt:yyyy-MM-dd HH:mm:ss}Z
            - Total cells: {cells.Count}
            - Detected: {detected}
            - Missed: {missed}
            - Errors: {errors}
            - Not-affected: {na}
            - Detection rate: {rate:P1}
            """;
    }

    internal static string BuildCoverageMarkdown(CoverageReport r) => $"""
        # Coverage Report — {r.GeneratedAt:yyyy-MM-dd HH:mm:ss}Z

        ## MetaPattern coverage
        - {r.MetaPattern.CoveredMetaPatterns}/{r.MetaPattern.TotalMetaPatterns} ({r.MetaPattern.Ratio:P0})
        - Covered: {string.Join(", ", r.MetaPattern.CoveredCodes)}
        - Uncovered: {string.Join(", ", r.MetaPattern.UncoveredCodes)}

        ## SUT × MR coverage
        - {r.SutMr.BoundCells}/{r.SutMr.PossibleCells} ({r.SutMr.Density:P0})
        - Unbound cells: {r.SutMr.UnboundCells.Count}

        ## Known-bug reproduction
        - {r.Bug.ReproducedBugs}/{r.Bug.TotalKnownBugs} ({r.Bug.Ratio:P0})
        - Reproduced: {string.Join(", ", r.Bug.ReproducedCodes)}

        ## Mutation detection
        - {r.Mutation.DetectedMutants}/{r.Mutation.TotalMutants} detected (rate {r.Mutation.DetectionRate:P1})
        - Sampled campaigns: {r.Mutation.CampaignsSampled}
        """;
}

/// <summary>SystemMtReportService 的渲染返回值。</summary>
public sealed record ReportRenderResult(
    Guid ReportId,
    string Scope,
    string ContentPath,
    string Content);
