using MetBench_BLL.SystemMT;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT;

public sealed class ApproxEqualAssertionTests
{
    private static ParsedOutput Out(double value) =>
        new(new Dictionary<string, double> { ["result"] = value }, new Dictionary<string, string>());

    [Fact]
    public void Evaluate_passes_when_values_are_exactly_equal()
    {
        var result = new ApproxEqualAssertion().Evaluate("result", Out(1.13), Out(1.13));

        Assert.True(result.Passed, result.FailureReason);
        Assert.Equal("ApproxEqual", result.AssertionName);
        Assert.Equal(1.13, result.SourceValue);
        Assert.Equal(1.13, result.FollowUpValue);
    }

    [Fact]
    public void Evaluate_passes_within_relative_tolerance()
    {
        // src=100, flw=103 -> delta 3, tol = 1e-5 + 0.05*100 = 5.00001 -> pass (3% < 5%)
        var result = new ApproxEqualAssertion().Evaluate("result", Out(100.0), Out(103.0));

        Assert.True(result.Passed, result.FailureReason);
    }

    [Fact]
    public void Evaluate_fails_outside_relative_tolerance()
    {
        // src=100, flw=110 -> delta 10, tol ≈ 5 -> fail (10% > 5%)
        var result = new ApproxEqualAssertion().Evaluate("result", Out(100.0), Out(110.0));

        Assert.False(result.Passed);
        Assert.Contains("exceeds tolerance", result.FailureReason);
    }

    [Fact]
    public void Evaluate_uses_absolute_tolerance_near_zero()
    {
        // src≈0: relative term ≈ 0, so atol governs. delta 5e-6 < atol 1e-5 -> pass
        var pass = new ApproxEqualAssertion().Evaluate("result", Out(0.0), Out(5e-6));
        Assert.True(pass.Passed, pass.FailureReason);

        // delta 2e-5 > atol 1e-5 -> fail
        var fail = new ApproxEqualAssertion().Evaluate("result", Out(0.0), Out(2e-5));
        Assert.False(fail.Passed);
    }

    [Fact]
    public void Evaluate_honors_custom_thresholds()
    {
        // src=100, flw=110 -> delta 10. rtol=0.2 -> tol ≈ 20 -> pass under custom
        var custom = new EqualityThresholds(AbsoluteTolerance: 1e-5, RelativeTolerance: 0.2);
        var result = new ApproxEqualAssertion(custom).Evaluate("result", Out(100.0), Out(110.0));

        Assert.True(result.Passed, result.FailureReason);
    }

    [Fact]
    public void Evaluate_fails_when_source_value_missing()
    {
        var source = new ParsedOutput(new Dictionary<string, double>(), new Dictionary<string, string>());
        var result = new ApproxEqualAssertion().Evaluate("result", source, Out(1.0));

        Assert.False(result.Passed);
        Assert.Contains("Missing parsed value", result.FailureReason);
    }

    [Fact]
    public void Evaluate_fails_when_followup_value_missing()
    {
        var followUp = new ParsedOutput(new Dictionary<string, double>(), new Dictionary<string, string>());
        var result = new ApproxEqualAssertion().Evaluate("result", Out(1.0), followUp);

        Assert.False(result.Passed);
        Assert.Contains("Missing parsed value", result.FailureReason);
    }
}
