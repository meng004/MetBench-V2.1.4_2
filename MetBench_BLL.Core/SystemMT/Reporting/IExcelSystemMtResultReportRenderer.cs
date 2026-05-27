using MetBench_BLL.SystemMT.Persistence;

namespace MetBench_BLL.SystemMT.Reporting;

/// <summary>
/// Renderer contract for emitting a System-level MT run report as an Excel
/// (<c>.xlsx</c>) byte buffer. Implementations live in <c>MetBench_BLL</c>
/// (ClosedXML dependency); the interface stays in <c>MetBench_BLL.Core</c>
/// so consumers in the Core layer can depend on it without pulling ClosedXML.
///
/// Workbook layout (parity with PDF / Word but tabular instead of paginated):
/// <list type="bullet">
///   <item><c>Summary</c> sheet — header row + one row per record (MrId,
///     AssertionName, ValueName, SourceValue, FollowUpValue, Passed,
///     FailureReason, RunAt). Numeric columns use real number cells so
///     downstream users can sort / pivot.</item>
///   <item><c>TypedVerification</c> sheet — only created when at least one
///     evidence entry carries a <see cref="TypedVerificationEvidence"/> block.
///     One row per evidence entry.</item>
///   <item><c>Charts</c> sheet — one PNG per record anchored to a cell.</item>
/// </list>
/// </summary>
public interface IExcelSystemMtResultReportRenderer
{
    byte[] Render(IEnumerable<SystemMtResultRecord> records, ReportContext? context = null);

    byte[] Render(
        IEnumerable<SystemMtResultRecord> records,
        IReadOnlyDictionary<Guid, ExecutionEvidence>? evidenceByExecutionId,
        ReportContext? context = null);
}
