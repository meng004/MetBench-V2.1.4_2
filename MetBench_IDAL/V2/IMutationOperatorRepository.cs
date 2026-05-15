using MetBench_Domain;
using System.Collections.ObjectModel;

namespace MetBench_IDAL;

/// <summary>MutationOperator CRUD 接口。</summary>
public interface IMutationOperatorRepository : IRepository<MutationOperator>
{
    /// <summary>按 Code 唯一索引查询。找不到返回 null。</summary>
    MutationOperator? GetByCode(string code);

    /// <summary>按 Category 列出（runner-level / input-adapter-level / ...）。</summary>
    ObservableCollection<MutationOperator> GetByCategory(string category);
}
