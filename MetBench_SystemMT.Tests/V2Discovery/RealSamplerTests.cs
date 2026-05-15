using System.Collections.ObjectModel;
using MetBench_BLL.Discovery.Validators;
using MetBench_Domain;
using MetBench_IDAL;
using Xunit;

namespace MetBench_SystemMT.Tests.V2Discovery;

/// <summary>
/// TDD for T-C 真实 sampler —— 替换 App.xaml.cs 中 hardcoded stub。
/// </summary>
public sealed class RealSamplerTests
{
    // ===== EmpiricalRepoSampler =====

    [Fact]
    public async Task EmpiricalRepoSampler_returns_empty_when_no_results()
    {
        var sampler = new EmpiricalRepoSampler(new FakeResultRepo());
        var samples = await sampler.SampleAsync(MakeCandidate("greater"));
        Assert.Empty(samples);
    }

    [Fact]
    public async Task EmpiricalRepoSampler_judges_greater_assertion()
    {
        var repo = new FakeResultRepo();
        repo.Add(MakeResult(1.0, 1.1));  // greater → holds
        repo.Add(MakeResult(1.0, 0.9));  // greater → fails
        repo.Add(MakeResult(1.0, 1.5));  // greater → holds

        var sampler = new EmpiricalRepoSampler(repo);
        var samples = await sampler.SampleAsync(MakeCandidate("greater"));

        Assert.Equal(3, samples.Count);
        Assert.Equal(2, samples.Count(s => s.AssertionHeld));
    }

    [Fact]
    public async Task EmpiricalRepoSampler_judges_approx_invariant()
    {
        var repo = new FakeResultRepo();
        repo.Add(MakeResult(1.0, 1.0001));   // approx-invariant: |Δ/source| < 1e-3 → holds
        repo.Add(MakeResult(1.0, 1.5));       // 偏离 50% → fails
        repo.Add(MakeResult(1.0, 0.9999));    // holds

        var sampler = new EmpiricalRepoSampler(repo);
        var samples = await sampler.SampleAsync(MakeCandidate("approx-invariant"));

        Assert.Equal(2, samples.Count(s => s.AssertionHeld));
    }

    [Fact]
    public async Task EmpiricalRepoSampler_ignores_results_with_null_values()
    {
        var repo = new FakeResultRepo();
        repo.Add(MakeResult(1.0, 1.1));
        repo.Add(new Result { IdResult = Guid.NewGuid(), SourceValue = null, FollowupValue = 1.1 });
        repo.Add(new Result { IdResult = Guid.NewGuid(), SourceValue = 1.0, FollowupValue = null });

        var sampler = new EmpiricalRepoSampler(repo);
        var samples = await sampler.SampleAsync(MakeCandidate("greater"));

        Assert.Single(samples);  // 仅第 1 个 result 有效
    }

    [Fact]
    public async Task EmpiricalRepoSampler_respects_max_samples_cap()
    {
        var repo = new FakeResultRepo();
        for (int i = 0; i < 100; i++)
            repo.Add(MakeResult(1.0, 1.1));

        var sampler = new EmpiricalRepoSampler(repo, maxSamples: 25);
        var samples = await sampler.SampleAsync(MakeCandidate("greater"));

        Assert.Equal(25, samples.Count);
    }

    // ===== AdversarialCampaignSampler =====

    [Fact]
    public async Task AdversarialCampaignSampler_returns_empty_when_no_mutation_results()
    {
        var sampler = new AdversarialCampaignSampler(new FakeMutationResultRepo());
        var probes = await sampler.SampleAsync(MakeCandidate("greater"));
        Assert.Empty(probes);
    }

    [Fact]
    public async Task AdversarialCampaignSampler_groups_by_mutant_id()
    {
        var repo = new FakeMutationResultRepo();
        // mutant 1: 一个 detected → 该 MR 抓住 → AssertionStillHolds=false
        repo.Add(MakeMutationResult(mutantId: 1, "detected"));
        repo.Add(MakeMutationResult(mutantId: 1, "missed"));
        // mutant 2: 全 missed → MR 没抓 → AssertionStillHolds=true
        repo.Add(MakeMutationResult(mutantId: 2, "missed"));
        repo.Add(MakeMutationResult(mutantId: 2, "missed"));
        // mutant 3: 全 not-affected → MR 没机会 → AssertionStillHolds=true
        repo.Add(MakeMutationResult(mutantId: 3, "not-affected"));

        var sampler = new AdversarialCampaignSampler(repo);
        var probes = await sampler.SampleAsync(MakeCandidate("greater"));

        Assert.Equal(3, probes.Count);
        var m1 = probes.Single(p => p.MutantId == 1);
        Assert.False(m1.AssertionStillHolds);
        var m2 = probes.Single(p => p.MutantId == 2);
        Assert.True(m2.AssertionStillHolds);
    }

    [Fact]
    public async Task AdversarialCampaignSampler_respects_max_mutants_cap()
    {
        var repo = new FakeMutationResultRepo();
        for (int i = 0; i < 50; i++)
            repo.Add(MakeMutationResult(mutantId: i, "detected"));

        var sampler = new AdversarialCampaignSampler(repo, maxMutants: 10);
        var probes = await sampler.SampleAsync(MakeCandidate("greater"));

        Assert.Equal(10, probes.Count);
    }

    // ===== AssertionHolds helper =====

    [Fact]
    public void AssertionHolds_unknown_code_falls_back_to_approx()
    {
        Assert.True(EmpiricalRepoSampler.AssertionHolds("unknown-type", 1.0, 1.0));
        Assert.False(EmpiricalRepoSampler.AssertionHolds("unknown-type", 1.0, 2.0));
    }

    // ===== Fixtures =====

    private static CandidateMR MakeCandidate(string assertionTypeCode) => new()
    {
        IdCandidate = Guid.NewGuid(),
        ProposedCode = "MR-X",
        SuggestedAssertionTypeCode = assertionTypeCode,
        ProposedValueName = "k_eff",
    };

    private static Result MakeResult(double source, double followup) => new()
    {
        IdResult = Guid.NewGuid(),
        SourceValue = source,
        FollowupValue = followup,
    };

    private static MutationResult MakeMutationResult(int mutantId, string outcome) => new()
    {
        IdMutationResult = Guid.NewGuid(),
        MutantId = mutantId,
        MRBindingId = 1,
        Outcome = outcome,
    };
}

internal sealed class FakeResultRepo : IResultRepository
{
    public List<Result> Data { get; } = new();
    public ObservableCollection<Result> GetAll() => new(Data);
    public Result? Get(Guid id) => Data.FirstOrDefault(r => r.IdResult == id);
    public ObservableCollection<Result> Get(Result t) => new(Data);
    public bool Add(Result e) { Data.Add(e); return true; }
    public bool Modify(Result e) => true;
    public bool Remove(Result e) => Data.Remove(e);
    public ObservableCollection<Result> GetPage(int p, int s) => new(Data.Skip(p * s).Take(s).ToList());
    public int Count() => Data.Count;
    public Result? GetByExecution(Guid executionId) => Data.FirstOrDefault(r => r.ExecutionId == executionId);
}

internal sealed class FakeMutationResultRepo : IMutationResultRepository
{
    public List<MutationResult> Data { get; } = new();
    public ObservableCollection<MutationResult> GetAll() => new(Data);
    public MutationResult? Get(Guid id) => Data.FirstOrDefault(r => r.IdMutationResult == id);
    public ObservableCollection<MutationResult> Get(MutationResult t) => new(Data);
    public bool Add(MutationResult e) { Data.Add(e); return true; }
    public bool Modify(MutationResult e) => true;
    public bool Remove(MutationResult e) => Data.Remove(e);
    public ObservableCollection<MutationResult> GetPage(int p, int s) => new(Data.Skip(p * s).Take(s).ToList());
    public int Count() => Data.Count;
    public ObservableCollection<MutationResult> GetByCampaign(Guid id) => new(Data.Where(r => r.CampaignId == id).ToList());
    public MutationResult? GetByMutantBinding(int m, int b) => Data.FirstOrDefault(r => r.MutantId == m && r.MRBindingId == b);
    public ObservableCollection<MutationResult> GetByOutcome(Guid id, string o) => new(Data.Where(r => r.CampaignId == id && r.Outcome == o).ToList());
}
