using System.Collections.ObjectModel;
using MetBench_Domain;
using MetBench_IDAL;

namespace MetBench_SystemMT.Tests.V2Trend;

internal sealed class FakeExecutionRepository : IExecutionRepository
{
    public List<Execution> Data { get; } = new();
    public ObservableCollection<Execution> GetAll() => new(Data);
    public Execution? Get(Guid id) => Data.FirstOrDefault(e => e.IdExecution == id);
    public ObservableCollection<Execution> Get(Execution e) => new(Data);
    public bool Add(Execution e) { Data.Add(e); return true; }
    public bool Modify(Execution e) { var i = Data.FindIndex(x => x.IdExecution == e.IdExecution); if (i < 0) return false; Data[i] = e; return true; }
    public bool Remove(Execution e) => Data.Remove(e);
    public ObservableCollection<Execution> GetPage(int p, int s) => new(Data.Skip(p * s).Take(s).ToList());
    public int Count() => Data.Count;
    public ObservableCollection<Execution> GetByMRInstance(int id) => new(Data.Where(e => e.MRInstanceId == id).ToList());
    public ObservableCollection<Execution> GetByBatch(Guid id) => new(Data.Where(e => e.BatchId == id).ToList());
    public ObservableCollection<Execution> GetByStatus(string s, int p, int sz)
        => new(Data.Where(e => e.Status == s).Skip(p * sz).Take(sz).ToList());
    public ObservableCollection<Execution> GetByDateRange(DateTime from, DateTime to)
        => new(Data.Where(e => e.QueuedAt >= from && e.QueuedAt < to).ToList());
}

internal sealed class FakeAnomalyRepoT : IAnomalyRepository
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

internal sealed class FakeCandidateRepoT : ICandidateMRRepository
{
    public List<CandidateMR> Data { get; } = new();
    public ObservableCollection<CandidateMR> GetAll() => new(Data);
    public CandidateMR? Get(Guid id) => Data.FirstOrDefault(c => c.IdCandidate == id);
    public ObservableCollection<CandidateMR> Get(CandidateMR e) => new(Data);
    public bool Add(CandidateMR e) { Data.Add(e); return true; }
    public bool Modify(CandidateMR e) { var i = Data.FindIndex(x => x.IdCandidate == e.IdCandidate); if (i < 0) return false; Data[i] = e; return true; }
    public bool Remove(CandidateMR e) => Data.Remove(e);
    public ObservableCollection<CandidateMR> GetPage(int p, int s) => new(Data.Skip(p * s).Take(s).ToList());
    public int Count() => Data.Count;
    public ObservableCollection<CandidateMR> GetByDiscoveryRun(Guid id) => new(Data.Where(c => c.DiscoveryRunId == id).ToList());
    public ObservableCollection<CandidateMR> GetByStatus(string s) => new(Data.Where(c => c.Status == s).ToList());
    public int? GetPromotedMRId(Guid id) => Get(id)?.PromotedToMRId;
}

internal sealed class FakeBugRepoT : IKnownBugRepository
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
