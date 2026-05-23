using MetBench_BLL.Equations;
using MetBench_BLL.MethodMT.Assertions;
using MetBench_BLL.MethodMT.Transformations;
using MetBench_BLL.SystemMT.Transformations;
using Xunit;

namespace MetBench_SystemMT.Tests.MethodMT;

/// <summary>
/// P5 — method 侧执行栈：MethodTransformationRegistry（决策 B 对称）
///              + MethodAssertionEvaluator（子集词表，噪声相关拒绝）。
/// </summary>
public sealed class MethodMtRegistryP5Tests
{
    // ── MethodTransformationRegistry ─────────────────────────────────────────

    private static EquationFunctionRegistry EmptyEqReg() => new();

    private static EquationFunctionRegistry BatemanReg()
    {
        var reg = new EquationFunctionRegistry();
        const string json = """{"compose":[{"op":"ScaleField","path":"x","params":{"factor":"{f}"}}]}""";
        reg.Register(new RecipeBasedEquationFunction("bateman", "CustomFn", "transformation", json));
        return reg;
    }

    [Fact]
    public void MethodRegistry_finds_global_transformation()
    {
        var reg = new MethodTransformationRegistry(EmptyEqReg());
        var t = reg.Resolve("ScaleField", equationKey: "");
        Assert.NotNull(t);
        Assert.Equal("ScaleField", t.Name);
    }

    [Fact]
    public void MethodRegistry_finds_equation_function_when_not_in_global()
    {
        var reg = new MethodTransformationRegistry(BatemanReg());
        var t = reg.Resolve("CustomFn", equationKey: "bateman");
        Assert.NotNull(t);
        Assert.Equal("CustomFn", t.Name);
    }

    [Fact]
    public void MethodRegistry_global_wins_over_equation_namespace()
    {
        // 注册与全局同名的方程函数 — 全局应优先
        var eqReg = new EquationFunctionRegistry();
        const string json = """{"compose":[]}"""; // noop
        eqReg.Register(new RecipeBasedEquationFunction("bateman", "ScaleField", "transformation", json));
        var reg = new MethodTransformationRegistry(eqReg);

        var t = reg.Resolve("ScaleField", equationKey: "bateman");
        // 全局 ScaleField 实际 scale 值
        var result = t.Apply(new Dictionary<string, object?> { ["x"] = 3.0 }, "x",
            new Dictionary<string, string> { ["factor"] = "2" });
        Assert.Equal(6.0, (double)result["x"]!);
    }

    [Fact]
    public void MethodRegistry_throws_UnknownTransformation_when_both_miss()
    {
        var reg = new MethodTransformationRegistry(EmptyEqReg());
        Assert.Throws<UnknownTransformationException>(() =>
            reg.Resolve("NoSuchOp", equationKey: "bateman"));
    }

    [Fact]
    public void MethodRegistry_throws_when_empty_equationKey_and_global_misses()
    {
        var reg = new MethodTransformationRegistry(EmptyEqReg());
        Assert.Throws<UnknownTransformationException>(() =>
            reg.Resolve("MethodOnly", equationKey: ""));
    }

    [Fact]
    public void MethodRegistry_equation_lookup_skipped_when_equationKey_empty()
    {
        var eqReg = BatemanReg();
        var reg = new MethodTransformationRegistry(eqReg);
        Assert.Throws<UnknownTransformationException>(() =>
            reg.Resolve("CustomFn", equationKey: ""));
    }

    [Fact]
    public void MethodRegistry_MathAbs_is_in_global_and_resolves()
    {
        var reg = new MethodTransformationRegistry(EmptyEqReg());
        var t = reg.Resolve("MathAbs", equationKey: "");
        Assert.Equal("MathAbs", t.Name);
    }

    [Fact]
    public void MethodRegistry_can_be_constructed_with_empty_registry()
    {
        var reg = new MethodTransformationRegistry(new EquationFunctionRegistry());
        Assert.NotNull(reg);
    }

    // ── MethodAssertionEvaluator ──────────────────────────────────────────────

    private static readonly MethodAssertionEvaluator _eval = new();

    [Fact]
    public void Less_followup_less_than_source_passes()
    {
        var r = _eval.Evaluate("less", sourceValue: 10.0, followupValue: 5.0);
        Assert.True(r.Passed);
    }

    [Fact]
    public void Less_followup_greater_than_source_fails()
    {
        var r = _eval.Evaluate("less", sourceValue: 5.0, followupValue: 10.0);
        Assert.False(r.Passed);
    }

    [Fact]
    public void Greater_followup_greater_than_source_passes()
    {
        var r = _eval.Evaluate("greater", sourceValue: 5.0, followupValue: 10.0);
        Assert.True(r.Passed);
    }

    [Fact]
    public void Approx_within_tolerance_passes()
    {
        var r = _eval.Evaluate("approx", sourceValue: 100.0, followupValue: 100.5, tolerance: 1.0);
        Assert.True(r.Passed);
    }

    [Fact]
    public void Approx_outside_tolerance_fails()
    {
        var r = _eval.Evaluate("approx", sourceValue: 100.0, followupValue: 102.0, tolerance: 1.0);
        Assert.False(r.Passed);
    }

    [Theory]
    [InlineData("less-noise-aware")]
    [InlineData("greater-noise-aware")]
    [InlineData("approx-invariant")]
    [InlineData("variance-ratio")]
    [InlineData("cross-program-agree")]
    [InlineData("flux-pointwise-approx")]
    public void NoiseAware_and_system_codes_throw_ArgumentException(string code)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            _eval.Evaluate(code, sourceValue: 1.0, followupValue: 2.0));
        Assert.Contains(code, ex.Message);
    }
}
