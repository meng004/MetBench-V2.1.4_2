using MetBench_BLL.SystemMT;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT;

public sealed class GreaterThanAssertionTests
{
    [Fact]
    public void Evaluate_passes_when_followup_value_is_greater()
    {
        var assertion = new GreaterThanAssertion();
        var source = new ParsedOutput(new Dictionary<string, double> { ["result"] = 2 }, new Dictionary<string, string>());
        var followUp = new ParsedOutput(new Dictionary<string, double> { ["result"] = 5 }, new Dictionary<string, string>());

        var result = assertion.Evaluate("result", source, followUp);

        Assert.True(result.Passed, result.FailureReason);
        Assert.Equal(2, result.SourceValue);
        Assert.Equal(5, result.FollowUpValue);
    }

    [Fact]
    public void Evaluate_fails_when_followup_value_is_not_greater()
    {
        var assertion = new GreaterThanAssertion();
        var source = new ParsedOutput(new Dictionary<string, double> { ["result"] = 5 }, new Dictionary<string, string>());
        var followUp = new ParsedOutput(new Dictionary<string, double> { ["result"] = 5 }, new Dictionary<string, string>());

        var result = assertion.Evaluate("result", source, followUp);

        Assert.False(result.Passed);
        Assert.Contains("Expected follow-up value", result.FailureReason);
    }

    [Fact]
    public void Evaluate_fails_when_value_is_missing()
    {
        var assertion = new GreaterThanAssertion();
        var source = new ParsedOutput(new Dictionary<string, double>(), new Dictionary<string, string>());
        var followUp = new ParsedOutput(new Dictionary<string, double> { ["result"] = 5 }, new Dictionary<string, string>());

        var result = assertion.Evaluate("result", source, followUp);

        Assert.False(result.Passed);
        Assert.Contains("Missing parsed value", result.FailureReason);
    }
}
