using System.Collections.ObjectModel;
using MetBench_Domain;
using MetBench_IDAL;

namespace MetBench_SystemMT.Tests.V2Mutation;

internal sealed class FakeMutationCampaignRepository : IMutationCampaignRepository
{
    public List<MutationCampaign> Data { get; } = new();
    public ObservableCollection<MutationCampaign> GetAll() => new(Data);
    public MutationCampaign? Get(Guid id) => Data.FirstOrDefault(c => c.IdCampaign == id);
    public ObservableCollection<MutationCampaign> Get(MutationCampaign t) => new(Data);
    public bool Add(MutationCampaign e) { Data.Add(e); return true; }
    public bool Modify(MutationCampaign e)
    {
        var i = Data.FindIndex(x => x.IdCampaign == e.IdCampaign);
        if (i < 0) return false;
        Data[i] = e; return true;
    }
    public bool Remove(MutationCampaign e) => Data.Remove(e);
    public ObservableCollection<MutationCampaign> GetPage(int p, int s)
        => new(Data.Skip(p * s).Take(s).ToList());
    public int Count() => Data.Count;
    public ObservableCollection<MutationCampaign> GetByStatus(string status)
        => new(Data.Where(c => c.Status == status).ToList());
    public ObservableCollection<MutationCampaign> GetRecent(int limit)
        => new(Data.OrderByDescending(c => c.StartedAt).Take(limit).ToList());
}

internal sealed class FakeMutationResultRepository : IMutationResultRepository
{
    public List<MutationResult> Data { get; } = new();
    public ObservableCollection<MutationResult> GetAll() => new(Data);
    public MutationResult? Get(Guid id) => Data.FirstOrDefault(r => r.IdMutationResult == id);
    public ObservableCollection<MutationResult> Get(MutationResult t) => new(Data);
    public bool Add(MutationResult e) { Data.Add(e); return true; }
    public bool Modify(MutationResult e)
    {
        var i = Data.FindIndex(x => x.IdMutationResult == e.IdMutationResult);
        if (i < 0) return false;
        Data[i] = e; return true;
    }
    public bool Remove(MutationResult e) => Data.Remove(e);
    public ObservableCollection<MutationResult> GetPage(int p, int s)
        => new(Data.Skip(p * s).Take(s).ToList());
    public int Count() => Data.Count;
    public ObservableCollection<MutationResult> GetByCampaign(Guid id)
        => new(Data.Where(r => r.CampaignId == id).ToList());
    public MutationResult? GetByMutantBinding(int mutantId, int mrBindingId)
        => Data.FirstOrDefault(r => r.MutantId == mutantId && r.MRBindingId == mrBindingId);
    public ObservableCollection<MutationResult> GetByOutcome(Guid id, string outcome)
        => new(Data.Where(r => r.CampaignId == id && r.Outcome == outcome).ToList());
}

internal sealed class FakeMutationAuditLogRepository : IAuditLogRepository
{
    public List<AuditLog> Logs { get; } = new();
    public ObservableCollection<AuditLog> GetAll() => new(Logs);
    public AuditLog? Get(Guid id) => Logs.FirstOrDefault(l => l.IdLog == id);
    public ObservableCollection<AuditLog> Get(AuditLog t) => new(Logs);
    public bool Add(AuditLog e) { Logs.Add(e); return true; }
    public bool Modify(AuditLog e) => true;
    public bool Remove(AuditLog e) => Logs.Remove(e);
    public ObservableCollection<AuditLog> GetPage(int p, int s)
        => new(Logs.Skip(p * s).Take(s).ToList());
    public int Count() => Logs.Count;
    public ObservableCollection<AuditLog> GetByDateRange(DateTime from, DateTime to)
        => new(Logs.Where(l => l.Timestamp >= from && l.Timestamp < to).ToList());
    public ObservableCollection<AuditLog> GetByAction(string a)
        => new(Logs.Where(l => l.Action == a).ToList());
    public ObservableCollection<AuditLog> GetByTargetEntity(string et, string eid)
        => new(Logs.Where(l => l.TargetEntityType == et && l.TargetEntityId == eid).ToList());
}
