using MetBench_BLL.SystemMT.Transformations;
using MetBench_BLL.SystemMT.Transformations.Math;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Transformations;

/// <summary>
/// P1 — mr-architecture §5 L0 数学基元 17 个算子单元测试。
/// 每算子 3-5 用例；聚合/特殊场景也覆盖边界和错误参数。
/// </summary>
public sealed class MathPrimitivesTests
{
    // ── helpers ─────────────────────────────────────────────────────────────
    private static Dictionary<string, object?> D(string k, object? v) => new() { [k] = v };
    private static Dictionary<string, object?> D(string k1, object? v1, string k2, object? v2)
        => new() { [k1] = v1, [k2] = v2 };
    private static Dictionary<string, string> P() => new();
    private static Dictionary<string, string> P(string k, string v) => new() { [k] = v };
    private static Dictionary<string, string> P(string k1, string v1, string k2, string v2)
        => new() { [k1] = v1, [k2] = v2 };

    private static double Val(Dictionary<string, object?> d, string k) => (double)d[k]!;

    // ── MathExp ──────────────────────────────────────────────────────────────

    [Fact]
    public void MathExp_of_zero_is_one()
        => Assert.Equal(1.0, Val(new MathExp().Apply(D("x", 0.0), "x", P()), "x"));

    [Fact]
    public void MathExp_of_one_matches_Math_E()
        => Assert.Equal(System.Math.E, Val(new MathExp().Apply(D("x", 1.0), "x", P()), "x"), 15);

    [Fact]
    public void MathExp_of_negative_is_positive_fraction()
    {
        var r = Val(new MathExp().Apply(D("x", -1.0), "x", P()), "x");
        Assert.True(r > 0 && r < 1);
        Assert.Equal(System.Math.Exp(-1.0), r, 15);
    }

    [Fact]
    public void MathExp_name_is_MathExp()
        => Assert.Equal("MathExp", new MathExp().Name);

    // ── MathLog ──────────────────────────────────────────────────────────────

    [Fact]
    public void MathLog_of_e_is_one()
        => Assert.Equal(1.0, Val(new MathLog().Apply(D("x", System.Math.E), "x", P()), "x"), 15);

    [Fact]
    public void MathLog_of_one_is_zero()
        => Assert.Equal(0.0, Val(new MathLog().Apply(D("x", 1.0), "x", P()), "x"));

    [Fact]
    public void MathLog_matches_System_Math()
    {
        var input = 7.389056; // ≈ e²
        var r = Val(new MathLog().Apply(D("x", input), "x", P()), "x");
        Assert.Equal(System.Math.Log(input), r, 10);
    }

    [Fact]
    public void MathLog_of_zero_or_negative_throws()
        => Assert.Throws<InvalidOperationException>(() =>
            new MathLog().Apply(D("x", -1.0), "x", P()));

    // ── MathSin ──────────────────────────────────────────────────────────────

    [Fact]
    public void MathSin_of_zero_is_zero()
        => Assert.Equal(0.0, Val(new MathSin().Apply(D("x", 0.0), "x", P()), "x"));

    [Fact]
    public void MathSin_of_pi_over_2_is_one()
        => Assert.Equal(1.0, Val(new MathSin().Apply(D("x", System.Math.PI / 2), "x", P()), "x"), 14);

    [Fact]
    public void MathSin_matches_System_Math()
    {
        const double v = 1.23;
        Assert.Equal(System.Math.Sin(v),
            Val(new MathSin().Apply(D("x", v), "x", P()), "x"), 15);
    }

    // ── MathCos ──────────────────────────────────────────────────────────────

    [Fact]
    public void MathCos_of_zero_is_one()
        => Assert.Equal(1.0, Val(new MathCos().Apply(D("x", 0.0), "x", P()), "x"));

    [Fact]
    public void MathCos_of_pi_is_minus_one()
        => Assert.Equal(-1.0, Val(new MathCos().Apply(D("x", System.Math.PI), "x", P()), "x"), 14);

    [Fact]
    public void MathCos_matches_System_Math()
    {
        const double v = 0.78;
        Assert.Equal(System.Math.Cos(v),
            Val(new MathCos().Apply(D("x", v), "x", P()), "x"), 15);
    }

    // ── MathSqrt ─────────────────────────────────────────────────────────────

    [Fact]
    public void MathSqrt_of_4_is_2()
        => Assert.Equal(2.0, Val(new MathSqrt().Apply(D("x", 4.0), "x", P()), "x"));

    [Fact]
    public void MathSqrt_of_zero_is_zero()
        => Assert.Equal(0.0, Val(new MathSqrt().Apply(D("x", 0.0), "x", P()), "x"));

    [Fact]
    public void MathSqrt_negative_throws()
        => Assert.Throws<InvalidOperationException>(() =>
            new MathSqrt().Apply(D("x", -1.0), "x", P()));

    [Fact]
    public void MathSqrt_matches_System_Math()
    {
        const double v = 2.0;
        Assert.Equal(System.Math.Sqrt(v),
            Val(new MathSqrt().Apply(D("x", v), "x", P()), "x"), 15);
    }

    // ── MathAbs ──────────────────────────────────────────────────────────────

    [Fact]
    public void MathAbs_of_positive_is_unchanged()
        => Assert.Equal(5.0, Val(new MathAbs().Apply(D("x", 5.0), "x", P()), "x"));

    [Fact]
    public void MathAbs_of_negative_is_positive()
        => Assert.Equal(3.0, Val(new MathAbs().Apply(D("x", -3.0), "x", P()), "x"));

    [Fact]
    public void MathAbs_of_zero_is_zero()
        => Assert.Equal(0.0, Val(new MathAbs().Apply(D("x", 0.0), "x", P()), "x"));

    // ── MathPow ──────────────────────────────────────────────────────────────

    [Fact]
    public void MathPow_square_of_3_is_9()
        => Assert.Equal(9.0, Val(new MathPow().Apply(D("x", 3.0), "x", P("exponent", "2")), "x"));

    [Fact]
    public void MathPow_to_zero_is_one()
        => Assert.Equal(1.0, Val(new MathPow().Apply(D("x", 7.0), "x", P("exponent", "0")), "x"));

    [Fact]
    public void MathPow_matches_System_Math()
    {
        var r = Val(new MathPow().Apply(D("x", 2.0), "x", P("exponent", "10")), "x");
        Assert.Equal(System.Math.Pow(2.0, 10.0), r, 15);
    }

    [Fact]
    public void MathPow_missing_exponent_throws()
        => Assert.Throws<ArgumentException>(() =>
            new MathPow().Apply(D("x", 2.0), "x", P()));

    // ── MathAdd ──────────────────────────────────────────────────────────────

    [Fact]
    public void MathAdd_adds_value()
        => Assert.Equal(7.0, Val(new MathAdd().Apply(D("x", 5.0), "x", P("value", "2")), "x"));

    [Fact]
    public void MathAdd_negative_value()
        => Assert.Equal(3.0, Val(new MathAdd().Apply(D("x", 5.0), "x", P("value", "-2")), "x"));

    [Fact]
    public void MathAdd_missing_value_throws()
        => Assert.Throws<ArgumentException>(() =>
            new MathAdd().Apply(D("x", 5.0), "x", P()));

    // ── MathSub ──────────────────────────────────────────────────────────────

    [Fact]
    public void MathSub_subtracts_value()
        => Assert.Equal(3.0, Val(new MathSub().Apply(D("x", 5.0), "x", P("value", "2")), "x"));

    [Fact]
    public void MathSub_negative_result_ok()
        => Assert.Equal(-2.0, Val(new MathSub().Apply(D("x", 1.0), "x", P("value", "3")), "x"));

    [Fact]
    public void MathSub_missing_value_throws()
        => Assert.Throws<ArgumentException>(() =>
            new MathSub().Apply(D("x", 5.0), "x", P()));

    // ── MathMul ──────────────────────────────────────────────────────────────

    [Fact]
    public void MathMul_multiplies_value()
        => Assert.Equal(10.0, Val(new MathMul().Apply(D("x", 5.0), "x", P("value", "2")), "x"));

    [Fact]
    public void MathMul_by_zero_is_zero()
        => Assert.Equal(0.0, Val(new MathMul().Apply(D("x", 5.0), "x", P("value", "0")), "x"));

    [Fact]
    public void MathMul_missing_value_throws()
        => Assert.Throws<ArgumentException>(() =>
            new MathMul().Apply(D("x", 5.0), "x", P()));

    // ── MathDiv ──────────────────────────────────────────────────────────────

    [Fact]
    public void MathDiv_divides_value()
        => Assert.Equal(2.5, Val(new MathDiv().Apply(D("x", 5.0), "x", P("value", "2")), "x"));

    [Fact]
    public void MathDiv_by_zero_throws()
        => Assert.Throws<InvalidOperationException>(() =>
            new MathDiv().Apply(D("x", 5.0), "x", P("value", "0")));

    [Fact]
    public void MathDiv_missing_value_throws()
        => Assert.Throws<ArgumentException>(() =>
            new MathDiv().Apply(D("x", 5.0), "x", P()));

    // ── MathLinComb ──────────────────────────────────────────────────────────

    [Fact]
    public void MathLinComb_two_fields_basic()
    {
        // result = 1.5*a + 2.0*b = 1.5*2 + 2.0*3 = 9.0; stored at "result"
        var src = new Dictionary<string, object?> { ["a"] = 2.0, ["b"] = 3.0, ["result"] = 0.0 };
        var r = new MathLinComb().Apply(src, "result",
            P("fields", "a,b", "coeffs", "1.5,2.0"));
        Assert.Equal(9.0, Val(r, "result"), 14);
    }

    [Fact]
    public void MathLinComb_single_field_acts_as_scale()
    {
        var src = new Dictionary<string, object?> { ["x"] = 4.0 };
        var r = new MathLinComb().Apply(src, "x", P("fields", "x", "coeffs", "3.0"));
        Assert.Equal(12.0, Val(r, "x"), 14);
    }

    [Fact]
    public void MathLinComb_mismatched_fields_and_coeffs_throws()
        => Assert.Throws<ArgumentException>(() =>
        {
            var src = new Dictionary<string, object?> { ["a"] = 1.0 };
            new MathLinComb().Apply(src, "out", P("fields", "a", "coeffs", "1.0,2.0"));
        });

    [Fact]
    public void MathLinComb_missing_params_throws()
        => Assert.Throws<ArgumentException>(() =>
        {
            var src = new Dictionary<string, object?> { ["a"] = 1.0 };
            new MathLinComb().Apply(src, "a", P());
        });

    // ── Linspace ─────────────────────────────────────────────────────────────

    [Fact]
    public void Linspace_generates_correct_count()
    {
        var parms = new Dictionary<string, string> { ["start"] = "0", ["stop"] = "10", ["count"] = "11" };
        var r = new Linspace().Apply(D("arr", (object?)null), "arr", parms);
        var arr = (List<double>)r["arr"]!;
        Assert.Equal(11, arr.Count);
    }

    [Fact]
    public void Linspace_endpoints_correct()
    {
        var parms = new Dictionary<string, string> { ["start"] = "0", ["stop"] = "1", ["count"] = "5" };
        var r = new Linspace().Apply(D("arr", (object?)null), "arr", parms);
        var arr = (List<double>)r["arr"]!;
        Assert.Equal(0.0, arr[0], 15);
        Assert.Equal(1.0, arr[4], 15);
    }

    [Fact]
    public void Linspace_evenly_spaced()
    {
        var parms = new Dictionary<string, string> { ["start"] = "0", ["stop"] = "4", ["count"] = "5" };
        var r = new Linspace().Apply(D("arr", (object?)null), "arr", parms);
        var arr = (List<double>)r["arr"]!;
        Assert.Equal(0.0, arr[0], 10); Assert.Equal(1.0, arr[1], 10);
        Assert.Equal(2.0, arr[2], 10); Assert.Equal(3.0, arr[3], 10);
        Assert.Equal(4.0, arr[4], 10);
    }

    [Fact]
    public void Linspace_count_1_returns_start()
    {
        var parms = new Dictionary<string, string> { ["start"] = "5", ["stop"] = "10", ["count"] = "1" };
        var r = new Linspace().Apply(D("arr", (object?)null), "arr", parms);
        var arr = (List<double>)r["arr"]!;
        Assert.Single(arr);
        Assert.Equal(5.0, arr[0], 15);
    }

    // ── Sum ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Sum_of_list_is_correct()
    {
        var arr = new List<object?> { 1.0, 2.0, 3.0 };
        var r = new Sum().Apply(D("data", arr), "data", P());
        Assert.Equal(6.0, Val(r, "data"), 14);
    }

    [Fact]
    public void Sum_of_single_element()
    {
        var arr = new List<object?> { 42.0 };
        Assert.Equal(42.0, Val(new Sum().Apply(D("data", arr), "data", P()), "data"), 14);
    }

    [Fact]
    public void Sum_of_empty_throws()
        => Assert.Throws<InvalidOperationException>(() =>
            new Sum().Apply(D("data", new List<object?>()), "data", P()));

    // ── Mean ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Mean_of_list_is_average()
    {
        var arr = new List<object?> { 2.0, 4.0, 6.0 };
        Assert.Equal(4.0, Val(new Mean().Apply(D("data", arr), "data", P()), "data"), 14);
    }

    [Fact]
    public void Mean_single_element_is_itself()
    {
        var arr = new List<object?> { 7.0 };
        Assert.Equal(7.0, Val(new Mean().Apply(D("data", arr), "data", P()), "data"), 14);
    }

    [Fact]
    public void Mean_of_empty_throws()
        => Assert.Throws<InvalidOperationException>(() =>
            new Mean().Apply(D("data", new List<object?>()), "data", P()));

    // ── Max ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Max_returns_largest()
    {
        var arr = new List<object?> { 3.0, 1.0, 4.0, 1.0, 5.0 };
        Assert.Equal(5.0, Val(new Max().Apply(D("data", arr), "data", P()), "data"), 14);
    }

    [Fact]
    public void Max_of_negatives()
    {
        var arr = new List<object?> { -5.0, -2.0, -8.0 };
        Assert.Equal(-2.0, Val(new Max().Apply(D("data", arr), "data", P()), "data"), 14);
    }

    [Fact]
    public void Max_of_empty_throws()
        => Assert.Throws<InvalidOperationException>(() =>
            new Max().Apply(D("data", new List<object?>()), "data", P()));

    // ── Min ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Min_returns_smallest()
    {
        var arr = new List<object?> { 3.0, 1.0, 4.0, 1.0, 5.0 };
        Assert.Equal(1.0, Val(new Min().Apply(D("data", arr), "data", P()), "data"), 14);
    }

    [Fact]
    public void Min_of_negatives()
    {
        var arr = new List<object?> { -5.0, -2.0, -8.0 };
        Assert.Equal(-8.0, Val(new Min().Apply(D("data", arr), "data", P()), "data"), 14);
    }

    [Fact]
    public void Min_of_empty_throws()
        => Assert.Throws<InvalidOperationException>(() =>
            new Min().Apply(D("data", new List<object?>()), "data", P()));

    // ── Registry ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("MathExp")] [InlineData("MathLog")] [InlineData("MathSin")]
    [InlineData("MathCos")] [InlineData("MathSqrt")] [InlineData("MathAbs")]
    [InlineData("MathPow")]
    [InlineData("MathAdd")] [InlineData("MathSub")] [InlineData("MathMul")] [InlineData("MathDiv")]
    [InlineData("MathLinComb")]
    [InlineData("Linspace")]
    [InlineData("Sum")] [InlineData("Mean")] [InlineData("Max")] [InlineData("Min")]
    public void TransformationRegistry_contains_math_primitive(string name)
        => Assert.Contains(name, TransformationRegistry.AvailableNames);
}
