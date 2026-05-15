using MetBench_Domain;
using System.Collections.ObjectModel;

namespace MetBench_IDAL;

/// <summary>Anomaly CRUD 接口（Guid PK；与 Result 0:1）。</summary>
public interface IAnomalyRepository : IGuidRepository<Anomaly>
{
    /// <summary>按 Result 查（0:1）。找不到返回 null。</summary>
    Anomaly? GetByResult(Guid resultId);

    /// <summary>按 Status 过滤 (new / investigating / known / confirmed-bug / false-positive)。</summary>
    ObservableCollection<Anomaly> GetByStatus(string status);

    /// <summary>列出关联到指定 KnownBug 的 Anomaly（看 R-Case 复现历史）。</summary>
    ObservableCollection<Anomaly> GetByLinkedBug(int knownBugId);
}
