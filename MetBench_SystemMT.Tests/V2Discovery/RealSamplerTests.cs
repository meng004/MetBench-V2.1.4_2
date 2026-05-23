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

    // AdversarialCampaignSampler 测试段已删除（next-stage P0 模型对齐）。

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
