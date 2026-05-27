using MetBench_BLL.SystemMT.Persistence;

namespace MetBench_BLL.SystemMT.Reporting;

/// <summary>
/// Renderer contract for emitting a System-level MT run report as a PDF byte buffer.
/// Implementations live in <c>MetBench_BLL</c> (iTextSharp dependency); the interface
/// stays in <c>MetBench_BLL.Core</c> so callers in the Core layer (and tests) can
/// depend on it without pulling iTextSharp.
///
/// Section parity with <see cref="ISystemMtResultReportRenderer"/>: title page,
/// per-record block (MR id, pass/fail, source / follow-up values, failure reason,
/// optional <see cref="TypedVerificationEvidence"/> block, embedded chart image).
/// </summary>
public interface IPdfSystemMtResultReportRenderer
{
    byte[] Render(IEnumerable<SystemMtResultRecord> records, ReportContext? context = null);

    /// <summary>
    /// Render the report and, when an <see cref="ExecutionEvidence"/> row is present
    /// for a record's <see cref="SystemMtResultRecord.Id"/> in
    /// <paramref name="evidenceByExecutionId"/>, surface its
    /// <see cref="ExecutionEvidence.TypedVerification"/> block in the record's
    /// detail section. Records without a matching evidence row render exactly as
    /// the legacy two-arg overload would.
    /// </summary>
    byte[] Render(
        IEnumerable<SystemMtResultRecord> records,
        IReadOnlyDictionary<Guid, ExecutionEvidence>? evidenceByExecutionId,
        ReportContext? context = null);
}
