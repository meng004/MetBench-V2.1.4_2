using MetBench_Domain;
using System.Collections.ObjectModel;

namespace MetBench_IDAL;

/// <summary>KnownBug (R-Case 库) CRUD 接口。</summary>
public interface IKnownBugRepository : IRepository<KnownBug>
{
    /// <summary>按 Code 唯一索引查询。找不到返回 null。</summary>
    KnownBug? GetByCode(string code);

    /// <summary>按 Status 列出。</summary>
    ObservableCollection<KnownBug> GetByStatus(string status);
}
