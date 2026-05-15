using System.Collections.ObjectModel;
using MetBench_Domain;
using MetBench_IDAL;

namespace MetBench_SystemMT.Tests.V2Discovery;

/// <summary>共享 fake：DiscoveryRun / CandidateMR / ValidationRun / MR / AuditLog。</summary>
internal sealed class FakeDiscoveryRunRepository : IDiscoveryRunRepository
{
    public List<DiscoveryRun> Data { get; } = new();
    public ObservableCollection<DiscoveryRun> GetAll() => new(Data);
    public DiscoveryRun? Get(Guid id) => Data.FirstOrDefault(r => r.IdRun == id);
    public ObservableCollection<DiscoveryRun> Get(DiscoveryRun template) => new(Data);
    public bool Add(DiscoveryRun e) { Data.Add(e); return true; }
    public bool Modify(DiscoveryRun e)
    {
        var i = Data.FindIndex(x => x.IdRun == e.IdRun);
        if (i < 0) return false;
        Data[i] = e; return true;
    }
    public bool Remove(DiscoveryRun e) => Data.Remove(e);
    public ObservableCollection<DiscoveryRun> GetPage(int p, int s)
        => new(Data.Skip(p * s).Take(s).ToList());
    public int Count() => Data.Count;
    public ObservableCollection<DiscoveryRun> GetByMethod(int methodId)
        => new(Data.Where(r => r.MethodId == methodId).ToList());
    public ObservableCollection<DiscoveryRun> GetByTargetApplication(int applicationId)
        => new(Data.Where(r => r.TargetApplicationId == applicationId).ToList());
}

internal sealed class FakeCandidateMRRepository : ICandidateMRRepository
{
    public List<CandidateMR> Data { get; } = new();
    public ObservableCollection<CandidateMR> GetAll() => new(Data);
    public CandidateMR? Get(Guid id) => Data.FirstOrDefault(c => c.IdCandidate == id);
    public ObservableCollection<CandidateMR> Get(CandidateMR template) => new(Data);
    public bool Add(CandidateMR e) { Data.Add(e); return true; }
    public bool Modify(CandidateMR e)
    {
        var i = Data.FindIndex(x => x.IdCandidate == e.IdCandidate);
        if (i < 0) return false;
        Data[i] = e; return true;
    }
    public bool Remove(CandidateMR e) => Data.Remove(e);
    public ObservableCollection<CandidateMR> GetPage(int p, int s)
        => new(Data.Skip(p * s).Take(s).ToList());
    public int Count() => Data.Count;
    public ObservableCollection<CandidateMR> GetByDiscoveryRun(Guid runId)
        => new(Data.Where(c => c.DiscoveryRunId == runId).ToList());
    public ObservableCollection<CandidateMR> GetByStatus(string status)
        => new(Data.Where(c => c.Status == status).ToList());
    public int? GetPromotedMRId(Guid id) => Get(id)?.PromotedToMRId;
}

internal sealed class FakeValidationRunRepository : IValidationRunRepository
{
    public List<ValidationRun> Data { get; } = new();
    public ObservableCollection<ValidationRun> GetAll() => new(Data);
    public ValidationRun? Get(Guid id) => Data.FirstOrDefault(v => v.IdValidation == id);
    public ObservableCollection<ValidationRun> Get(ValidationRun template) => new(Data);
    public bool Add(ValidationRun e) { Data.Add(e); return true; }
    public bool Modify(ValidationRun e)
    {
        var i = Data.FindIndex(x => x.IdValidation == e.IdValidation);
        if (i < 0) return false;
        Data[i] = e; return true;
    }
    public bool Remove(ValidationRun e) => Data.Remove(e);
    public ObservableCollection<ValidationRun> GetPage(int p, int s)
        => new(Data.Skip(p * s).Take(s).ToList());
    public int Count() => Data.Count;
    public ObservableCollection<ValidationRun> GetByCandidate(Guid candidateMRId)
        => new(Data.Where(v => v.CandidateMRId == candidateMRId).ToList());
    public int CountPassedValidators(Guid candidateMRId)
        => Data.Where(v => v.CandidateMRId == candidateMRId && v.Passed)
               .Select(v => v.ValidatorName).Distinct().Count();
}

internal sealed class FakeMetamorphicRelationRepository : IMetamorphicRelationRepository
{
    public List<MetamorphicRelation> Data { get; } = new();
    private int _nextId = 1;

    public DatatoImage datatoImage { get; set; } = null!;

    public ObservableCollection<MetamorphicRelation> GetAll() => new(Data);
    public MetamorphicRelation Get(int id) => Data.First(m => m.IdMR == id);
    public ObservableCollection<MetamorphicRelation> Get(MetamorphicRelation entity) => new(Data);
    public bool Add(MetamorphicRelation e) { e.IdMR = _nextId++; Data.Add(e); return true; }
    public bool Modify(MetamorphicRelation e)
    {
        var i = Data.FindIndex(x => x.IdMR == e.IdMR);
        if (i < 0) return false;
        Data[i] = e; return true;
    }
    public bool Remove(MetamorphicRelation e) => Data.Remove(e);
    public ObservableCollection<MetamorphicRelations_QueryResultData> GetAll_MIX() => new();
    public ObservableCollection<MetamorphicRelations_QueryResultData> Get(MetamorphicRelation mr, string d) => new();
    public ObservableCollection<MetamorphicRelations_QueryResultData> GetAll_MIXTwoTable() => new();
    public bool IsDuplicate(MetamorphicRelation mr, bool excludeSelf = false)
        => Data.Any(d => d.Code == mr.Code && (!excludeSelf || d.IdMR != mr.IdMR));
}

internal sealed class FakeDiscoveryMethodRepository : IDiscoveryMethodRepository
{
    public List<DiscoveryMethod> Data { get; } = new();
    public ObservableCollection<DiscoveryMethod> GetAll() => new(Data);
    public DiscoveryMethod Get(int id) => Data.First(m => m.IdMethod == id);
    public ObservableCollection<DiscoveryMethod> Get(DiscoveryMethod entity) => new(Data);
    public bool Add(DiscoveryMethod e) { Data.Add(e); return true; }
    public bool Modify(DiscoveryMethod e)
    {
        var i = Data.FindIndex(x => x.IdMethod == e.IdMethod);
        if (i < 0) return false;
        Data[i] = e; return true;
    }
    public bool Remove(DiscoveryMethod e) => Data.Remove(e);
    public ObservableCollection<DiscoveryMethod> GetEnabled()
        => new(Data.Where(m => m.Enabled).ToList());
    public DiscoveryMethod? GetByNameVersion(string name, string version)
        => Data.FirstOrDefault(m => m.Name == name && m.Version == version);
}

internal sealed class FakeAuditLogRepository : IAuditLogRepository
{
    public List<AuditLog> Logs { get; } = new();
    public ObservableCollection<AuditLog> GetAll() => new(Logs);
    public AuditLog? Get(Guid id) => Logs.FirstOrDefault(l => l.IdLog == id);
    public ObservableCollection<AuditLog> Get(AuditLog template) => new(Logs);
    public bool Add(AuditLog e) { Logs.Add(e); return true; }
    public bool Modify(AuditLog e) => true;
    public bool Remove(AuditLog e) => Logs.Remove(e);
    public ObservableCollection<AuditLog> GetPage(int p, int s)
        => new(Logs.Skip(p * s).Take(s).ToList());
    public int Count() => Logs.Count;
    public ObservableCollection<AuditLog> GetByDateRange(DateTime from, DateTime to)
        => new(Logs.Where(l => l.Timestamp >= from && l.Timestamp < to).ToList());
    public ObservableCollection<AuditLog> GetByAction(string action)
        => new(Logs.Where(l => l.Action == action).ToList());
    public ObservableCollection<AuditLog> GetByTargetEntity(string et, string eid)
        => new(Logs.Where(l => l.TargetEntityType == et && l.TargetEntityId == eid).ToList());
}
