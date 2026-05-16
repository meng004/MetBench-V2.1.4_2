using System.Collections.ObjectModel;
using MetBench_Domain;
using MetBench_IDAL;

namespace MetBench_SystemMT.Tests.V2Coverage;

internal sealed class FakeMRRepository : IMetamorphicRelationRepository
{
    public List<MetamorphicRelation> Data { get; } = new();
    private int _nextId = 1;
    public DatatoImage datatoImage { get; set; } = null!;

    public ObservableCollection<MetamorphicRelation> GetAll() => new(Data);
    public MetamorphicRelation Get(int id) => Data.First(m => m.IdMR == id);
    public ObservableCollection<MetamorphicRelation> Get(MetamorphicRelation e) => new(Data);
    public bool Add(MetamorphicRelation e) { e.IdMR = _nextId++; Data.Add(e); return true; }
    public bool Modify(MetamorphicRelation e) { var i = Data.FindIndex(x => x.IdMR == e.IdMR); if (i < 0) return false; Data[i] = e; return true; }
    public bool Remove(MetamorphicRelation e) => Data.Remove(e);
    public ObservableCollection<MetamorphicRelations_QueryResultData> GetAll_MIX() => new();
    public ObservableCollection<MetamorphicRelations_QueryResultData> Get(MetamorphicRelation m, string d) => new();
    public ObservableCollection<MetamorphicRelations_QueryResultData> GetAll_MIXTwoTable() => new();
    public bool IsDuplicate(MetamorphicRelation m, bool excludeSelf = false) => false;
}

internal sealed class FakeMRBindingRepository : IMRBindingRepository
{
    public List<MRBinding> Data { get; } = new();
    public ObservableCollection<MRBinding> GetAll() => new(Data);
    public MRBinding Get(int id) => Data.First(b => b.IdMRBinding == id);
    public ObservableCollection<MRBinding> Get(MRBinding e) => new(Data);
    public bool Add(MRBinding e) { Data.Add(e); return true; }
    public bool Modify(MRBinding e) { var i = Data.FindIndex(x => x.IdMRBinding == e.IdMRBinding); if (i < 0) return false; Data[i] = e; return true; }
    public bool Remove(MRBinding e) => Data.Remove(e);
    public ObservableCollection<MRBinding> GetByMR(int mrId) => new(Data.Where(b => b.MRId == mrId).ToList());
    public ObservableCollection<MRBinding> GetByApplication(int appId) => new(Data.Where(b => b.ApplicationId == appId).ToList());
    public ObservableCollection<MRBinding> GetActive() => new(Data.Where(b => b.Status == "active").ToList());
    public ObservableCollection<MRBinding> GetByStatus(string status) => new(Data.Where(b => b.Status == status).ToList());
}

internal sealed class FakeApplicationRepository : IApplicationRepository
{
    public List<Application> Data { get; } = new();
    public ObservableCollection<Application> GetAll() => new(Data);
    public Application Get(int id) => Data.First(a => a.IdApplication == id);
    public ObservableCollection<Application> Get(Application e) => new(Data);
    public bool Add(Application e) { Data.Add(e); return true; }
    public bool Modify(Application e) { var i = Data.FindIndex(x => x.IdApplication == e.IdApplication); if (i < 0) return false; Data[i] = e; return true; }
    public bool Remove(Application e) => Data.Remove(e);
    public int Get(string name) => Data.FirstOrDefault(a => a.Name == name)?.IdApplication ?? -1;
    public ObservableCollection<Applications_QueryResultData> GetAll_MIX() => new();
    public ObservableCollection<Applications_QueryResultData> GetByName(string name) => new();
    public bool IsDuplicate(Application app, bool excludeSelf) => false;
}

internal sealed class FakeKnownBugRepository : IKnownBugRepository
{
    public List<KnownBug> Data { get; } = new();
    public ObservableCollection<KnownBug> GetAll() => new(Data);
    public KnownBug Get(int id) => Data.First(b => b.IdBug == id);
    public ObservableCollection<KnownBug> Get(KnownBug e) => new(Data);
    public bool Add(KnownBug e) { Data.Add(e); return true; }
    public bool Modify(KnownBug e) { var i = Data.FindIndex(x => x.IdBug == e.IdBug); if (i < 0) return false; Data[i] = e; return true; }
    public bool Remove(KnownBug e) => Data.Remove(e);
    public KnownBug? GetByCode(string code) => Data.FirstOrDefault(b => b.Code == code);
    public ObservableCollection<KnownBug> GetByStatus(string status) => new(Data.Where(b => b.Status == status).ToList());
}

internal sealed class FakeAnomalyRepo : IAnomalyRepository
{
    public List<MetBench_Domain.Anomaly> Data { get; } = new();
    public ObservableCollection<MetBench_Domain.Anomaly> GetAll() => new(Data);
    public MetBench_Domain.Anomaly? Get(Guid id) => Data.FirstOrDefault(a => a.IdAnomaly == id);
    public ObservableCollection<MetBench_Domain.Anomaly> Get(MetBench_Domain.Anomaly e) => new(Data);
    public bool Add(MetBench_Domain.Anomaly e) { Data.Add(e); return true; }
    public bool Modify(MetBench_Domain.Anomaly e) { var i = Data.FindIndex(x => x.IdAnomaly == e.IdAnomaly); if (i < 0) return false; Data[i] = e; return true; }
    public bool Remove(MetBench_Domain.Anomaly e) => Data.Remove(e);
    public ObservableCollection<MetBench_Domain.Anomaly> GetPage(int p, int s) => new(Data.Skip(p * s).Take(s).ToList());
    public int Count() => Data.Count;
    public MetBench_Domain.Anomaly? GetByResult(Guid id) => Data.FirstOrDefault(a => a.ResultId == id);
    public ObservableCollection<MetBench_Domain.Anomaly> GetByStatus(string s) => new(Data.Where(a => a.Status == s).ToList());
    public ObservableCollection<MetBench_Domain.Anomaly> GetByLinkedBug(int id) => new(Data.Where(a => a.LinkedKnownBugId == id).ToList());
}

internal sealed class FakeMutationResultRepo : IMutationResultRepository
{
    public List<MutationResult> Data { get; } = new();
    public ObservableCollection<MutationResult> GetAll() => new(Data);
    public MutationResult? Get(Guid id) => Data.FirstOrDefault(r => r.IdMutationResult == id);
    public ObservableCollection<MutationResult> Get(MutationResult e) => new(Data);
    public bool Add(MutationResult e) { Data.Add(e); return true; }
    public bool Modify(MutationResult e) { var i = Data.FindIndex(x => x.IdMutationResult == e.IdMutationResult); if (i < 0) return false; Data[i] = e; return true; }
    public bool Remove(MutationResult e) => Data.Remove(e);
    public ObservableCollection<MutationResult> GetPage(int p, int s) => new(Data.Skip(p * s).Take(s).ToList());
    public int Count() => Data.Count;
    public ObservableCollection<MutationResult> GetByCampaign(Guid id) => new(Data.Where(r => r.CampaignId == id).ToList());
    public MutationResult? GetByMutantBinding(int m, int b) => Data.FirstOrDefault(r => r.MutantId == m && r.MRBindingId == b);
    public ObservableCollection<MutationResult> GetByOutcome(Guid id, string o) => new(Data.Where(r => r.CampaignId == id && r.Outcome == o).ToList());
}
