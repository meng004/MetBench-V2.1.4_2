using MetBench_BLL.SystemMT.Persistence;

namespace MetBench_BLL.SystemMT.Reporting;

public interface ISystemMtResultReportRenderer
{
    string Render(IEnumerable<SystemMtResultRecord> records, ReportContext? context = null);
}

public sealed record ReportContext(
    string Title = "MetBench System-Level MT Run Report",
    DateTimeOffset? GeneratedAt = null);
