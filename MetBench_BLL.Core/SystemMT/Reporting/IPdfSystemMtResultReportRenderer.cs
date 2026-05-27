using System.Collections.Generic;
using MetBench_BLL.SystemMT.Persistence;

namespace MetBench_BLL.SystemMT.Reporting;

/// <summary>
/// Renders one or more <see cref="SystemMtResultRecord"/>s into a PDF byte
/// buffer, mirroring the section layout of
/// <see cref="HtmlSystemMtResultReportRenderer"/> and the markdown report
/// produced by <c>SystemMtReportService.GenerateExecution</c>. Includes an
/// embedded PNG chart per record (rendered by an <see cref="ISystemMtChartRenderer"/>
/// implementation) so the reader gets a visual alongside the numeric block.
///
/// Evidence-aware: when an <see cref="ExecutionEvidence"/> row is supplied
/// for a record's <see cref="SystemMtResultRecord.Id"/>, the per-record block
/// also surfaces <see cref="ExecutionEvidence.TypedVerification"/>.
/// </summary>
public interface IPdfSystemMtResultReportRenderer
{
    byte[] Render(
        IEnumerable<SystemMtResultRecord> records,
        IReadOnlyDictionary<System.Guid, ExecutionEvidence>? evidenceByExecutionId = null,
        ReportContext? context = null);
}
