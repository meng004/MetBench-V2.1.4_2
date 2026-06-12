using System.Text;

namespace MetBench_BLL.Core.SystemMT.ImportExport.Put;

public static class ExternalMrAcceptanceEvidenceProjector
{
    public static ExternalMrAcceptanceEvidenceReport Project(SutImportUnit unit)
    {
        ArgumentNullException.ThrowIfNull(unit);

        var candidates = unit.Detections
            .Where(d => d.Result == DetectionResult.Detected)
            .Select(d => new ExternalMrAcceptanceAnomalyCandidate(
                d.MrId,
                d.MutationId,
                d.DetectionId,
                "Imported research evidence: candidate is bounded by the source repository commit and is not a fresh MetBench runtime failure."))
            .ToList();

        var limitations = new List<string>();
        if (unit.Sut.SutId.Contains("sciml-domain-validity", StringComparison.Ordinal))
        {
            limitations.Add("SciML seeded-fault evidence is limited to one-SUT / one-checkpoint claims.");
        }
        if (unit.Compatibility.OverallReadiness == RuntimeReadiness.ImportedOnly)
        {
            limitations.Add("Runtime readiness is ImportedOnly unless a MetBench launcher/runtime adapter is bound.");
        }
        limitations.AddRange(unit.Compatibility.Findings);

        var deferredMrs = unit.Mrs
            .Where(m => IsDeferred(m))
            .Select(m => m.MrId)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();

        return new ExternalMrAcceptanceEvidenceReport(
            unit.Sut.SutId,
            unit.Provenance.RepositoryUrl,
            unit.Provenance.Commit,
            unit.Detections.Count,
            unit.Detections.Count(d => d.Result == DetectionResult.Detected),
            unit.Detections.Count(d => d.Result == DetectionResult.Survived),
            unit.Detections.Count(d => d.Result == DetectionResult.Inconclusive),
            candidates,
            deferredMrs,
            limitations);
    }

    public static string RenderMarkdown(ExternalMrAcceptanceEvidenceReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var sb = new StringBuilder();
        sb.AppendLine("# External MR Acceptance Evidence");
        sb.AppendLine();
        sb.AppendLine($"- SUT: {report.SutId}");
        sb.AppendLine($"- Repository: {report.RepositoryUrl}");
        sb.AppendLine($"- Commit: {report.Commit}");
        sb.AppendLine($"- Detection records: {report.TotalDetectionRecords}");
        sb.AppendLine($"- Detected: {report.DetectedRecords}");
        sb.AppendLine($"- Survived: {report.SurvivedRecords}");
        sb.AppendLine($"- Inconclusive: {report.InconclusiveRecords}");
        sb.AppendLine();

        if (report.AnomalyCandidates.Count > 0)
        {
            sb.AppendLine("## Anomaly candidates");
            foreach (var candidate in report.AnomalyCandidates)
            {
                sb.AppendLine($"- {candidate.MrId} / {candidate.MutationId}: {candidate.Limitation}");
            }
            sb.AppendLine();
        }

        if (report.DeferredMrIds.Count > 0)
        {
            sb.AppendLine("## Deferred / diagnostic MRs");
            foreach (var mrId in report.DeferredMrIds)
            {
                sb.AppendLine($"- {mrId}: deferred diagnostic evidence; not an absolute pass/fail runtime verdict.");
            }
            sb.AppendLine();
        }

        if (report.Limitations.Count > 0)
        {
            sb.AppendLine("## Limitations");
            foreach (var limitation in report.Limitations)
            {
                sb.AppendLine($"- {limitation}");
            }
        }

        return sb.ToString();
    }

    private static bool IsDeferred(MrAsset mr)
    {
        return mr.TransformBinding.Status == CompatibilityStatus.ImportedOnly
            || mr.AssertionBinding.Status == CompatibilityStatus.ImportedOnly
            || (mr.Metadata.TryGetValue("status", out var status)
                && status.Contains("deferred", StringComparison.OrdinalIgnoreCase));
    }
}

public sealed record ExternalMrAcceptanceEvidenceReport(
    string SutId,
    string RepositoryUrl,
    string Commit,
    int TotalDetectionRecords,
    int DetectedRecords,
    int SurvivedRecords,
    int InconclusiveRecords,
    IReadOnlyList<ExternalMrAcceptanceAnomalyCandidate> AnomalyCandidates,
    IReadOnlyList<string> DeferredMrIds,
    IReadOnlyList<string> Limitations);

public sealed record ExternalMrAcceptanceAnomalyCandidate(
    string MrId,
    string MutationId,
    string DetectionId,
    string Limitation);
