using MetBench_BLL.SystemMT.Persistence;

namespace MetBench_BLL.SystemMT.Reporting;

public interface ISystemMtResultReportRenderer
{
    string Render(IEnumerable<SystemMtResultRecord> records, ReportContext? context = null);

    /// <summary>
    /// Render report HTML and, when an <see cref="ExecutionEvidence"/> row is present
    /// for a <see cref="SystemMtResultRecord.Id"/> in <paramref name="evidenceByExecutionId"/>,
    /// surface the row's <see cref="ExecutionEvidence.TypedVerification"/> block in the
    /// per-row "Details" section. Records without a matching evidence row render exactly
    /// as the legacy <see cref="Render(IEnumerable{SystemMtResultRecord},ReportContext?)"/>
    /// overload.
    /// </summary>
    string Render(
        IEnumerable<SystemMtResultRecord> records,
        IReadOnlyDictionary<Guid, ExecutionEvidence>? evidenceByExecutionId,
        ReportContext? context = null);
}

public sealed record ReportContext(
    string Title = "MetBench System-Level MT Run Report",
    DateTimeOffset? GeneratedAt = null);
