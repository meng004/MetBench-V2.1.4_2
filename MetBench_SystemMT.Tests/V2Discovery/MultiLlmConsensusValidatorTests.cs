using MetBench_BLL.Discovery;
using Xunit;

namespace MetBench_SystemMT.Tests.V2Discovery;

public sealed class MultiLlmConsensusValidatorTests
{
    // ===== construction =====

    [Fact]
    public void Ctor_throws_when_no_gateways_provided()
    {
        Assert.Throws<ArgumentException>(() => new MultiLlmConsensusValidator(Array.Empty<NamedGateway>()));
    }

    // ===== consensus =====

    [Fact]
    public async Task All_true_yields_consensus_true_and_unanimous()
    {
        var v = new MultiLlmConsensusValidator(new[]
        {
            new NamedGateway("deepseek", new ScriptedGateway("""{"plausible": true}""")),
            new NamedGateway("anthropic", new ScriptedGateway("""{"plausible": true}""")),
            new NamedGateway("openai",    new ScriptedGateway("""{"plausible": true}""")),
        });

        var report = await v.AssessAsync("prompt");

        Assert.Equal(3, report.ProviderCount);
        Assert.Equal(3, report.ValidResponses);
        Assert.True(report.Consensus);
        Assert.True(report.UnanimousAgreement);
        Assert.NotNull(report.CohenKappa);
    }

    [Fact]
    public async Task All_false_yields_consensus_false_and_unanimous()
    {
        var v = new MultiLlmConsensusValidator(new[]
        {
            new NamedGateway("a", new ScriptedGateway("""{"plausible": false}""")),
            new NamedGateway("b", new ScriptedGateway("""{"plausible": false}""")),
            new NamedGateway("c", new ScriptedGateway("""{"plausible": false}""")),
        });

        var report = await v.AssessAsync("prompt");

        Assert.False(report.Consensus);
        Assert.True(report.UnanimousAgreement);
    }

    [Fact]
    public async Task Strict_majority_two_of_three_yields_consensus_true()
    {
        var v = new MultiLlmConsensusValidator(new[]
        {
            new NamedGateway("a", new ScriptedGateway("""{"plausible": true}""")),
            new NamedGateway("b", new ScriptedGateway("""{"plausible": true}""")),
            new NamedGateway("c", new ScriptedGateway("""{"plausible": false}""")),
        });

        var report = await v.AssessAsync("prompt");

        Assert.True(report.Consensus);
        Assert.False(report.UnanimousAgreement);
    }

    [Fact]
    public async Task Tie_yields_inconclusive_consensus_null()
    {
        var v = new MultiLlmConsensusValidator(new[]
        {
            new NamedGateway("a", new ScriptedGateway("""{"plausible": true}""")),
            new NamedGateway("b", new ScriptedGateway("""{"plausible": false}""")),
        });

        var report = await v.AssessAsync("prompt");

        Assert.Null(report.Consensus);
        Assert.Equal(2, report.ValidResponses);
        Assert.False(report.UnanimousAgreement);
    }

    // ===== error handling =====

    [Fact]
    public async Task Parse_failure_excludes_response_from_voting()
    {
        var v = new MultiLlmConsensusValidator(new[]
        {
            new NamedGateway("good1", new ScriptedGateway("""{"plausible": true}""")),
            new NamedGateway("good2", new ScriptedGateway("""{"plausible": true}""")),
            new NamedGateway("garbage", new ScriptedGateway("not json at all")),
        });

        var report = await v.AssessAsync("prompt");

        Assert.Equal(2, report.ValidResponses);
        Assert.True(report.Consensus);
        var garbage = report.Responses.Single(r => r.ProviderName == "garbage");
        Assert.Null(garbage.Plausible);
    }

    [Fact]
    public async Task Gateway_exception_recorded_as_error_message()
    {
        var v = new MultiLlmConsensusValidator(new[]
        {
            new NamedGateway("ok",      new ScriptedGateway("""{"plausible": true}""")),
            new NamedGateway("broken",  new ThrowingGateway(new InvalidOperationException("boom"))),
        });

        var report = await v.AssessAsync("prompt");

        var broken = report.Responses.Single(r => r.ProviderName == "broken");
        Assert.Null(broken.Plausible);
        Assert.Contains("boom", broken.ErrorMessage);
        Assert.Equal(1, report.ValidResponses);
    }

    [Fact]
    public async Task All_gateways_throw_yields_null_consensus()
    {
        var v = new MultiLlmConsensusValidator(new[]
        {
            new NamedGateway("a", new ThrowingGateway(new Exception("x"))),
            new NamedGateway("b", new ThrowingGateway(new Exception("y"))),
        });

        var report = await v.AssessAsync("prompt");

        Assert.Equal(0, report.ValidResponses);
        Assert.Null(report.Consensus);
        Assert.Null(report.CohenKappa);
        Assert.False(report.UnanimousAgreement);
    }

    [Fact]
    public async Task Cancellation_propagates()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var v = new MultiLlmConsensusValidator(new[]
        {
            new NamedGateway("a", new CancellingGateway()),
        });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => v.AssessAsync("prompt", cts.Token));
    }

    // ===== Cohen's κ =====

    [Fact]
    public async Task Cohen_kappa_is_one_when_all_agree()
    {
        var v = new MultiLlmConsensusValidator(new[]
        {
            new NamedGateway("a", new ScriptedGateway("""{"plausible": true}""")),
            new NamedGateway("b", new ScriptedGateway("""{"plausible": true}""")),
        });

        var report = await v.AssessAsync("prompt");

        // Unanimous + only one rating-level (all true) → po=1, pe=1, formula degenerates → returns po=1
        Assert.NotNull(report.CohenKappa);
        Assert.Equal(1.0, report.CohenKappa!.Value, 6);
    }

    [Fact]
    public async Task Cohen_kappa_below_one_when_raters_disagree()
    {
        var v = new MultiLlmConsensusValidator(new[]
        {
            new NamedGateway("a", new ScriptedGateway("""{"plausible": true}""")),
            new NamedGateway("b", new ScriptedGateway("""{"plausible": true}""")),
            new NamedGateway("c", new ScriptedGateway("""{"plausible": false}""")),
        });

        var report = await v.AssessAsync("prompt");

        Assert.NotNull(report.CohenKappa);
        Assert.True(report.CohenKappa!.Value < 1.0,
            $"κ should be < 1 when raters disagree, got {report.CohenKappa}");
    }

    // ===== parsing edge cases =====

    [Fact]
    public void TryParsePlausible_returns_null_for_empty_string()
    {
        Assert.Null(MultiLlmConsensusValidator.TryParsePlausible(""));
        Assert.Null(MultiLlmConsensusValidator.TryParsePlausible("   "));
    }

    [Fact]
    public void TryParsePlausible_returns_null_when_field_missing()
    {
        Assert.Null(MultiLlmConsensusValidator.TryParsePlausible("""{"reason": "yes"}"""));
    }

    [Fact]
    public void TryParsePlausible_returns_null_for_non_boolean_value()
    {
        Assert.Null(MultiLlmConsensusValidator.TryParsePlausible("""{"plausible": "yes"}"""));
        Assert.Null(MultiLlmConsensusValidator.TryParsePlausible("""{"plausible": 1}"""));
        Assert.Null(MultiLlmConsensusValidator.TryParsePlausible("""{"plausible": null}"""));
    }

    [Fact]
    public void TryParsePlausible_returns_true_and_false_correctly()
    {
        Assert.True(MultiLlmConsensusValidator.TryParsePlausible("""{"plausible": true, "reason": "x"}"""));
        Assert.False(MultiLlmConsensusValidator.TryParsePlausible("""{"plausible": false}"""));
    }

    // ===== fakes =====

    private sealed class ScriptedGateway : ILlmGateway
    {
        private readonly string _payload;
        public ScriptedGateway(string payload) { _payload = payload; }
        public Task<string> CompleteAsync(string prompt, CancellationToken ct = default) =>
            Task.FromResult(_payload);
    }

    private sealed class ThrowingGateway : ILlmGateway
    {
        private readonly Exception _ex;
        public ThrowingGateway(Exception ex) { _ex = ex; }
        public Task<string> CompleteAsync(string prompt, CancellationToken ct = default) =>
            throw _ex;
    }

    private sealed class CancellingGateway : ILlmGateway
    {
        public Task<string> CompleteAsync(string prompt, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult("""{"plausible": true}""");
        }
    }
}
