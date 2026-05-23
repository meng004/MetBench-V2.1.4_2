using MetBench_Domain;
using System.Collections.ObjectModel;

namespace MetBench_IDAL;

/// <summary>Report CRUD 接口（Guid PK）。</summary>
public interface IReportRepository : IGuidRepository<Report>
{
    /// <summary>按 Scope 列出 (single-execution / single-campaign / anomaly / coverage / ad-hoc / paper-package；weekly/monthly 已下线)。</summary>
    ObservableCollection<Report> GetByScope(string scope);

    /// <summary>列出指定时间窗的报告。</summary>
    ObservableCollection<Report> GetByDateRange(DateTime from, DateTime to);
}
