using MetBench_Domain;

namespace MetBench_IDAL;

/// <summary>Runtime CRUD 接口。</summary>
public interface IRuntimeRepository : IRepository<Runtime>
{
    /// <summary>按 Name 唯一索引查询。找不到返回 null。</summary>
    Runtime? GetByName(string name);
}
