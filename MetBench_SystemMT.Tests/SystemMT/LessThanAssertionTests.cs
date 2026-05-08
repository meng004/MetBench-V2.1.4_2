using MetBench_BLL.SystemMT;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT;

public sealed class LessThanAssertionTests
{
    [Fact]
    public void Name_is_LessThan()
    {
        Assert.Equal("LessThan", new LessThanAssertion().Name);
    }

    [Fact]
    public void Evaluate_passes_when_followup_value_is_less()
    {
        var assertion = new LessThanAssertion();
        var source = new ParsedOutput(new Dictionary<string, double> { ["k_eff"] = 1.13 }, new Dictionary<string, string>());
        var followUp = new ParsedOutput(new Dictionary<string, double> { ["k_eff"] = 0.81 }, new Dictionary<string, string>());

        var result = assertion.Evaluate("k_eff", source, followUp);

        Assert.True(result.Passed, result.FailureReason);
        Assert.Equal("LessThan", result.AssertionName);
        Assert.Equal(1.13, result.SourceValue);
        Assert.Equal(0.81, result.FollowUpValue);
    }

    [Fact]
    public void Evaluate_fails_when_followup_value_is_not_less()
    {
        var assertion = new LessThanAssertion();
        var source = new ParsedOutput(new Dictionary<string, double> { ["k_eff"] = 1.0 }, new Dictionary<string, string>());
        var followUp = new ParsedOutput(new Dictionary<string, double> { ["k_eff"] = 1.0 }, new Dictionary<string, string>());

        var result = assertion.Evaluate("k_eff", source, followUp);

        Assert.False(result.Passed);
        Assert.Contains("Expected follow-up value", result.FailureReason);
        Assert.Contains("less than", result.FailureReason);
    }

    [Fact]
    public void Evaluate_fails_when_value_missing_in_source()
    {
        var assertion = new LessThanAssertion();
        var source = new ParsedOutput(new Dictionary<string, double>(), new Dictionary<string, string>());
        var followUp = new ParsedOutput(new Dictionary<string, double> { ["k_eff"] = 0.5 }, new Dictionary<string, string>());

        var result = assertion.Evaluate("k_eff", source, followUp);

        Assert.False(result.Passed);
        Assert.Contains("Missing parsed value", result.FailureReason);
        Assert.Contains("source", result.FailureReason);
    }

    [Fact]
    public void Evaluate_fails_when_value_missing_in_followup()
    {
        var assertion = new LessThanAssertion();
        var source = new ParsedOutput(new Dictionary<string, double> { ["k_eff"] = 1.0 }, new Dictionary<string, string>());
        var followUp = new ParsedOutput(new Dictionary<string, double>(), new Dictionary<string, string>());

        var result = assertion.Evaluate("k_eff", source, followUp);

        Assert.False(result.Passed);
        Assert.Contains("Missing parsed value", result.FailureReason);
        Assert.Contains("follow-up", result.FailureReason);
    }
}
