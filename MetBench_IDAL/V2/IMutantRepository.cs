using MetBench_Domain;
using System.Collections.ObjectModel;

namespace MetBench_IDAL;

/// <summary>Mutant CRUD 接口。</summary>
public interface IMutantRepository : IRepository<Mutant>
{
    /// <summary>列出指定 Operator 的全部 Mutant。</summary>
    ObservableCollection<Mutant> GetByOperator(int operatorId);

    /// <summary>列出指定 SUT 上的 Mutant（ApplicationId 匹配）。</summary>
    ObservableCollection<Mutant> GetByApplication(int applicationId);

    /// <summary>列出 active 状态的 Mutant。</summary>
    ObservableCollection<Mutant> GetActive();
}
