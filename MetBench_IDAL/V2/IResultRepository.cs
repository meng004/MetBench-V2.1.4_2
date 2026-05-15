using MetBench_Domain;

namespace MetBench_IDAL;

/// <summary>Result CRUD 接口（Guid PK；与 Execution 1:1）。</summary>
public interface IResultRepository : IGuidRepository<Result>
{
    /// <summary>按 Execution 查（1:1）。找不到返回 null。</summary>
    Result? GetByExecution(Guid executionId);
}
