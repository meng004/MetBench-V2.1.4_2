using MetBench_BLL.Discovery;
using MetBench_BLL.Discovery.Validators;
using MetBench_Domain;
using Xunit;

namespace MetBench_SystemMT.Tests.V2Discovery;

public sealed class ValidatorTests
{
    // ===== EmpiricalValidator =====

    [Fact]
    public async Task Empirical_passes_when_sample_pass_rate_above_threshold()
    {
        var samples = new EmpiricalSample[]
        {
            new(1.1, 1.09, true), new(1.1, 1.08, true), new(1.1, 1.07, true),
            new(1.1, 1.05, true), new(1.1, 1.04, true), new(1.1, 1.02, true),
            new(1.1, 1.01, true), new(1.1, 1.00, true), new(1.1, 0.95, true),
            new(1.1, 0.90, true),
        };
        var v = new EmpiricalValidator((_, _) => Task.FromResult((IReadOnlyList<EmpiricalSample>)samples), passThreshold: 0.9);
        var outcome = await v.ValidateAsync(MakeCandidate());
        Assert.True(outcome.Passed);
    }

    [Fact]
    public async Task Empirical_fails_when_pass_rate_below_threshold()
    {
        var samples = new EmpiricalSample[]
        {
            new(1.1, 1.09, true), new(1.1, 1.08, false), new(1.1, 1.07, false),
        };
        var v = new EmpiricalValidator((_, _) => Task.FromResult((IReadOnlyList<EmpiricalSample>)samples), passThreshold: 0.9);
        var outcome = await v.ValidateAsync(MakeCandidate());
        Assert.False(outcome.Passed);
    }

    [Fact]
    public async Task Empirical_fails_when_no_samples()
    {
        var v = new EmpiricalValidator((_, _) =>
            Task.FromResult((IReadOnlyList<EmpiricalSample>)Array.Empty<EmpiricalSample>()));
        var outcome = await v.ValidateAsync(MakeCandidate());
        Assert.False(outcome.Passed);
        Assert.Contains("no-samples", outcome.DetailsJson);
    }

    // ===== TheoreticalLlmValidator =====

    [Fact]
    public async Task TheoreticalLlm_passes_when_llm_returns_plausible_true()
    {
        var v = new TheoreticalLlmValidator(new NullLlmGateway("{\"plausible\": true, \"reason\": \"C4 symmetry\"}"));
        var outcome = await v.ValidateAsync(MakeCandidate());
        Assert.True(outcome.Passed);
        Assert.Contains("C4", outcome.DetailsJson);
    }

    [Fact]
    public async Task TheoreticalLlm_fails_when_llm_returns_plausible_false()
    {
        var v = new TheoreticalLlmValidator(new NullLlmGateway("{\"plausible\": false, \"reason\": \"breaks conservation\"}"));
        var outcome = await v.ValidateAsync(MakeCandidate());
        Assert.False(outcome.Passed);
    }

    [Fact]
    public async Task TheoreticalLlm_fails_on_unparseable_reply()
    {
        var v = new TheoreticalLlmValidator(new NullLlmGateway("not json at all"));
        var outcome = await v.ValidateAsync(MakeCandidate());
        Assert.False(outcome.Passed);
        Assert.Contains("unparseable", outcome.DetailsJson);
    }

    [Fact]
    public async Task TheoreticalLlm_fails_on_empty_reply()
    {
        var v = new TheoreticalLlmValidator(new NullLlmGateway(""));
        var outcome = await v.ValidateAsync(MakeCandidate());
        Assert.False(outcome.Passed);
        Assert.Contains("empty-reply", outcome.DetailsJson);
    }

    // AdversarialMutmutValidator 测试段已删除（next-stage P0 模型对齐：
    // MR 识别收敛为 meta-prompt + multi-LLM 共识，AdversarialMutmut 移出 T4 范围）。

    private static CandidateMR MakeCandidate() => new()
    {
        IdCandidate = Guid.NewGuid(),
        ProposedCode = "MR-T-001",
        ProposedName = "scale fuel σ_f",
        SuggestedAssertionTypeCode = "less",
        ProposedValueName = "k_eff",
        Rationale = "monotone",
    };
}
