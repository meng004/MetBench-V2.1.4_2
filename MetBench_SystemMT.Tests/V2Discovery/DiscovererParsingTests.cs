using MetBench_BLL.Discovery;
using Xunit;

namespace MetBench_SystemMT.Tests.V2Discovery;

public sealed class DiscovererParsingTests
{
    [Fact]
    public void MetaPatternDiscoverer_parses_realizable_candidates()
    {
        const string json = @"{
          ""metapatterns"": {},
          ""candidates"": [
            {""id"": ""MR01"", ""meta_pattern"": ""m_inv"", ""block"": ""G"", ""parameter"": ""rot"",
             ""description"": ""rotate 90"", ""hypothesis"": ""k=k"", ""assertion"": ""approx"",
             ""physical_rationale"": ""C4 symmetry"", ""realizable_with_current_sut"": true},
            {""id"": ""MR99"", ""meta_pattern"": ""m_rev"", ""block"": ""T"", ""parameter"": ""x"",
             ""description"": ""skipped"", ""hypothesis"": """", ""assertion"": ""bound"",
             ""physical_rationale"": """", ""realizable_with_current_sut"": false}
          ]
        }";

        var parsed = MetaPatternDiscoverer.ParseProposals(json);

        Assert.Single(parsed);
        Assert.Equal("MR01", parsed[0].ProposedCode);
        Assert.Equal("approx", parsed[0].SuggestedAssertionTypeCode);
        Assert.Equal("k_eff", parsed[0].ProposedValueName);
        Assert.Contains("C4 symmetry", parsed[0].Rationale);
    }

    [Fact]
    public void MetaPatternDiscoverer_handles_missing_candidates_array()
    {
        var parsed = MetaPatternDiscoverer.ParseProposals("{}");
        Assert.Empty(parsed);
    }

    [Fact]
    public void LlmNativeDiscoverer_parses_well_formed_reply()
    {
        const string reply = @"{""candidates"": [
            {""code"":""MR-T-auto-001"",""name"":""scale fuel"",""assertion"":""less"",
             ""value"":""k_eff"",""rationale"":""monotone"",""confidence"":0.85},
            {""code"":""MR-T-auto-002"",""name"":""scale mod"",""assertion"":""greater"",
             ""value"":""k_eff"",""rationale"":""inverse"",""confidence"":0.6}
        ]}";

        var parsed = LlmNativeDiscoverer.ParseLlmReply(reply);

        Assert.Equal(2, parsed.Count);
        Assert.Equal("MR-T-auto-001", parsed[0].ProposedCode);
        Assert.Equal(0.85, parsed[0].Confidence, 3);
        Assert.Equal("less", parsed[0].SuggestedAssertionTypeCode);
    }

    [Fact]
    public void LlmNativeDiscoverer_defaults_confidence_when_missing()
    {
        const string reply = @"{""candidates"":[{""code"":""MR-X""}]}";
        var parsed = LlmNativeDiscoverer.ParseLlmReply(reply);
        Assert.Single(parsed);
        Assert.Equal(0.5, parsed[0].Confidence, 3);
        Assert.Equal("k_eff", parsed[0].ProposedValueName);
    }

    [Fact]
    public async Task LlmNativeDiscoverer_with_null_gateway_returns_zero_proposals()
    {
        var discoverer = new LlmNativeDiscoverer(new NullLlmGateway(""));
        var outcome = await discoverer.DiscoverAsync(targetApplicationId: 1);
        Assert.Equal("ok", outcome.Status);
        Assert.Empty(outcome.Proposals);
    }

    [Fact]
    public async Task LlmNativeDiscoverer_marks_error_on_bad_json()
    {
        var discoverer = new LlmNativeDiscoverer(new NullLlmGateway("not-json"));
        var outcome = await discoverer.DiscoverAsync(null);
        Assert.Equal("error", outcome.Status);
        Assert.Contains("LLM JSON parse", outcome.ErrorMessage ?? "");
    }

    [Fact]
    public async Task LlmNativeDiscoverer_catches_gateway_exception()
    {
        var discoverer = new LlmNativeDiscoverer(new ThrowingGateway());
        var outcome = await discoverer.DiscoverAsync(null);
        Assert.Equal("error", outcome.Status);
        Assert.NotNull(outcome.ErrorMessage);
    }

    private sealed class ThrowingGateway : ILlmGateway
    {
        public Task<string> CompleteAsync(string prompt, CancellationToken ct = default)
            => throw new InvalidOperationException("network down");
    }
}
