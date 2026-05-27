using MetBench_BLL.SystemMT.Persistence;

namespace MetBench_BLL.SystemMT.Reporting;

/// <summary>
/// Renderer contract for emitting a System-level MT run report as a Word
/// (<c>.docx</c>) byte buffer. Implementations live in <c>MetBench_BLL</c>
/// (DocumentFormat.OpenXml dependency); the interface stays in
/// <c>MetBench_BLL.Core</c> so consumers in the Core layer can depend on it
/// without pulling OpenXml.
///
/// Section parity with <see cref="ISystemMtResultReportRenderer"/> +
/// <see cref="IPdfSystemMtResultReportRenderer"/>: title, per-record block
/// (MR id, pass/fail, source / follow-up values, failure reason, optional
/// <see cref="TypedVerificationEvidence"/> block, embedded chart image).
/// </summary>
public interface IWordSystemMtResultReportRenderer
{
    byte[] Render(IEnumerable<SystemMtResultRecord> records, ReportContext? context = null);

    /// <summary>
    /// Render the report and, when an <see cref="ExecutionEvidence"/> row is present
    /// for a record's <see cref="SystemMtResultRecord.Id"/> in
    /// <paramref name="evidenceByExecutionId"/>, surface its
    /// <see cref="ExecutionEvidence.TypedVerification"/> block in the record's
    /// detail paragraphs.
    /// </summary>
    byte[] Render(
        IEnumerable<SystemMtResultRecord> records,
        IReadOnlyDictionary<Guid, ExecutionEvidence>? evidenceByExecutionId,
        ReportContext? context = null);
}
