using MetBench_Domain;
using System.Collections.ObjectModel;

namespace MetBench_IDAL;

/// <summary>MutationCampaign CRUD 接口（Guid PK）。</summary>
public interface IMutationCampaignRepository : IGuidRepository<MutationCampaign>
{
    /// <summary>按 Status 列出。</summary>
    ObservableCollection<MutationCampaign> GetByStatus(string status);

    /// <summary>列出最近 N 个 Campaign（dashboard 用）。</summary>
    ObservableCollection<MutationCampaign> GetRecent(int limit);
}
