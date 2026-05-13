using MetBench_BLL.SystemMT.Assertions;
using Xunit;

namespace MetBench_SystemMT.Tests.V2Pipeline;

/// <summary>
/// TDD 验证 P4.2 AssertionEvaluator —— switch 按 AssertionTypeCode 分派 +
/// 包装为 SystemMtAssertionResultV2 + 不让 FA 异常逃逸。
/// </summary>
public sealed class AssertionEvaluatorTests
{
    private readonly AssertionEvaluator _eval = new();

    [Fact]
    public void Less_pass_returns_passed_result()
    {
        var input = new AssertionInput(
            SourceValue: 1.0, SourceStd: 0.0,
            FollowupValue: 0.5, FollowupStd: 0.0,
            ValueName: "k_eff");
        var result = _eval.Evaluate(input, new AssertionTolerance(),
            AssertionTypeCodes.Less);
        Assert.True(result.Passed);
        Assert.Null(result.FailureReason);
    }

    [Fact]
    public void Less_fail_returns_failed_result_with_message()
    {
        var input = new AssertionInput(
            SourceValue: 1.0, SourceStd: 0.0,
            FollowupValue: 1.5, FollowupStd: 0.0,
            ValueName: "k_eff");
        var result = _eval.Evaluate(input, new AssertionTolerance(),
            AssertionTypeCodes.Less);
        Assert.False(result.Passed);
        Assert.NotNull(result.FailureReason);
        // FA 没有 throw 出来
    }

    [Fact]
    public void LessNoiseAware_uses_3sigma_threshold()
    {
        var input = new AssertionInput(
            SourceValue: 1.0, SourceStd: 0.01,
            FollowupValue: 0.95, FollowupStd: 0.01,
            ValueName: "k_eff");
        // 3σ_composite = 3 × sqrt(0.0001+0.0001) ≈ 0.0424
        // threshold = 1.0 - 0.0424 ≈ 0.9576；followup 0.95 < 0.9576 → pass
        var result = _eval.Evaluate(input,
            new AssertionTolerance(NoiseAware: true, NoiseMultiplier: 3.0),
            AssertionTypeCodes.LessNoiseAware);
        Assert.True(result.Passed);
    }

    [Fact]
    public void GreaterNoiseAware_pass()
    {
        var input = new AssertionInput(
            SourceValue: 1.0, SourceStd: 0.0,
            FollowupValue: 1.5, FollowupStd: 0.0,
            ValueName: "k_eff");
        var result = _eval.Evaluate(input, new AssertionTolerance(),
            AssertionTypeCodes.GreaterNoiseAware);
        Assert.True(result.Passed);
    }

    [Fact]
    public void Approx_uses_tolerance_rel()
    {
        var input = new AssertionInput(
            SourceValue: 1.0, SourceStd: 0.0,
            FollowupValue: 1.0005, FollowupStd: 0.0,
            ValueName: "k_eff");
        var result = _eval.Evaluate(input,
            new AssertionTolerance(ToleranceRel: 0.001),
            AssertionTypeCodes.Approx);
        Assert.True(result.Passed);
    }

    [Fact]
    public void VarianceRatio_requires_extra_refinement_factor()
    {
        var input = new AssertionInput(
            SourceValue: 0.0, SourceStd: 0.002,
            FollowupValue: 0.0, FollowupStd: 0.001,
            ValueName: "k_eff_std");  // 注意：值名仅为元数据
        var withFactor = new AssertionInput(
            SourceValue: 0.0, SourceStd: 0.002,
            FollowupValue: 0.0, FollowupStd: 0.001,
            ValueName: "k_eff_std",
            ExtraValues: new Dictionary<string, double> { ["refinement_factor"] = 4.0 });

        var fail = _eval.Evaluate(input, new AssertionTolerance(ToleranceRel: 0.05),
            AssertionTypeCodes.VarianceRatio);
        Assert.False(fail.Passed);
        Assert.Contains("refinement_factor", fail.FailureReason ?? "");

        var pass = _eval.Evaluate(withFactor, new AssertionTolerance(ToleranceRel: 0.05),
            AssertionTypeCodes.VarianceRatio);
        Assert.True(pass.Passed);
    }

    [Fact]
    public void CrossProgramAgree_requires_reference_value()
    {
        var withRef = new AssertionInput(
            SourceValue: 0.0, SourceStd: 0.0,
            FollowupValue: 1.13, FollowupStd: 0.001,
            ValueName: "k_eff",
            ExtraValues: new Dictionary<string, double>
            {
                ["reference_value"] = 1.12,
                ["reference_std"] = 0.001,
            });

        var result = _eval.Evaluate(withRef, new AssertionTolerance(ToleranceRel: 0.05),
            AssertionTypeCodes.CrossProgramAgree);
        Assert.True(result.Passed);
    }

    [Fact]
    public void Unknown_type_code_returns_unknown_result()
    {
        var input = new AssertionInput(
            SourceValue: 1.0, SourceStd: 0.0,
            FollowupValue: 0.5, FollowupStd: 0.0,
            ValueName: "k_eff");
        var result = _eval.Evaluate(input, new AssertionTolerance(),
            "non-existent-assertion-type");
        Assert.False(result.Passed);
        Assert.Contains("Unknown", result.FailureReason ?? "");
    }

    [Fact]
    public void FluxPointwiseApprox_with_arrays()
    {
        var input = new AssertionInput(
            SourceValue: 0.0, SourceStd: 0.0,
            FollowupValue: 0.0, FollowupStd: 0.0,
            ValueName: "flux",
            ExtraValues: new Dictionary<string, double>
            {
                ["source_flux_0"] = 1.0,
                ["source_flux_1"] = 2.0,
                ["followup_flux_0"] = 1.0,
                ["followup_flux_1"] = 2.0,
            });
        var result = _eval.Evaluate(input, new AssertionTolerance(ToleranceRel: 0.01),
            AssertionTypeCodes.FluxPointwiseApprox);
        Assert.True(result.Passed);
    }
}
