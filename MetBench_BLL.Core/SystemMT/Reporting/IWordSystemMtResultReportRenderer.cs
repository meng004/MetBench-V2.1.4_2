using System.Collections.Generic;
using MetBench_BLL.SystemMT.Persistence;

namespace MetBench_BLL.SystemMT.Reporting;

/// <summary>
/// Renders <see cref="SystemMtResultRecord"/>s into an OOXML .docx byte buffer.
/// Section layout mirrors the HTML / PDF / Markdown precedents (PR #126 / PR-T2-3a / PR #128).
/// Evidence-aware: an <see cref="ExecutionEvidence"/> row keyed by record
/// <see cref="SystemMtResultRecord.Id"/> surfaces a <c>TypedVerification</c> sub-block.
/// </summary>
public interface IWordSystemMtResultReportRenderer
{
    byte[] Render(
        IEnumerable<SystemMtResultRecord> records,
        IReadOnlyDictionary<System.Guid, ExecutionEvidence>? evidenceByExecutionId = null,
        ReportContext? context = null);
}
