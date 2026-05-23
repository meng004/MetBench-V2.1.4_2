using MetBench_BLL.SystemMT.Transformations;
using Xunit;

namespace MetBench_SystemMT.Tests.V2Transformations;

/// <summary>
/// P3.1 prerequisite — <see cref="ScaleFuelAbsorption"/>:中子物理一致性变换。
/// 将 fuel container 的 <c>sigma_a</c> 按 factor 缩放,同时 <c>sigma_t += (factor-1)·old_sigma_a</c>,
/// 维持 transport 方程的 <c>sigma_t ≡ sigma_a + Σ sigma_s</c> 关系。
/// 用于 openmoc/openmc 的 ScaleFuelSigmaA MR —— 替代 W1 的
/// <c>SUT/openm{oc,c}/*_input_adapter_sigma_a.py</c> 跨字段更新逻辑。
/// </summary>
public sealed class ScaleFuelAbsorptionTests
{
    private static Dictionary<string, object?> MakePincellData() => new()
    {
        ["materials"] = new Dictionary<string, object?>
        {
            ["fuel"] = new Dictionary<string, object?>
            {
                ["sigma_a"]    = new List<object?> { 0.01012, 0.080032 },
                ["sigma_t"]    = new List<object?> { 0.222222, 0.833333 },
                ["sigma_s"]    = new List<object?> { 0.192423, 0.020000, 0.000000, 0.753300 },
                ["nu_sigma_f"] = new List<object?> { 0.006400, 0.156500 },
                ["chi"]        = new List<object?> { 1.000000, 0.000000 },
            }
        }
    };

    [Fact]
    public void Apply_scales_sigma_a_by_factor_and_adjusts_sigma_t()
    {
        var t = new ScaleFuelAbsorption();
        var src = MakePincellData();

        var result = t.Apply(src, "/materials/fuel",
            new Dictionary<string, string> { ["factor"] = "1.5" });

        var fuel = (Dictionary<string, object?>)
            ((Dictionary<string, object?>)result["materials"]!)["fuel"]!;
        var sa = (List<object?>)fuel["sigma_a"]!;
        var st = (List<object?>)fuel["sigma_t"]!;

        // sigma_a = factor × old
        Assert.Equal(0.01012 * 1.5,  (double)sa[0]!, 9);
        Assert.Equal(0.080032 * 1.5, (double)sa[1]!, 9);
        // sigma_t += (factor − 1) × old_sigma_a
        Assert.Equal(0.222222 + 0.5 * 0.01012,  (double)st[0]!, 9);
        Assert.Equal(0.833333 + 0.5 * 0.080032, (double)st[1]!, 9);
    }

    [Fact]
    public void Apply_preserves_other_fuel_fields()
    {
        var t = new ScaleFuelAbsorption();
        var src = MakePincellData();

        var result = t.Apply(src, "/materials/fuel",
            new Dictionary<string, string> { ["factor"] = "1.5" });

        var fuel = (Dictionary<string, object?>)
            ((Dictionary<string, object?>)result["materials"]!)["fuel"]!;
        var nsf = (List<object?>)fuel["nu_sigma_f"]!;
        var chi = (List<object?>)fuel["chi"]!;
        var ss  = (List<object?>)fuel["sigma_s"]!;

        Assert.Equal(0.006400, (double)nsf[0]!);
        Assert.Equal(0.156500, (double)nsf[1]!);
        Assert.Equal(1.0, (double)chi[0]!);
        Assert.Equal(0.0, (double)chi[1]!);
        Assert.Equal(0.192423, (double)ss[0]!);
    }

    [Fact]
    public void Apply_does_not_mutate_source_dict()
    {
        var t = new ScaleFuelAbsorption();
        var src = MakePincellData();

        _ = t.Apply(src, "/materials/fuel",
            new Dictionary<string, string> { ["factor"] = "2.0" });

        var srcFuel = (Dictionary<string, object?>)
            ((Dictionary<string, object?>)src["materials"]!)["fuel"]!;
        var srcSa = (List<object?>)srcFuel["sigma_a"]!;
        var srcSt = (List<object?>)srcFuel["sigma_t"]!;
        Assert.Equal(0.01012,   (double)srcSa[0]!);
        Assert.Equal(0.222222,  (double)srcSt[0]!);
    }

    [Fact]
    public void Apply_rejects_missing_factor()
    {
        var t = new ScaleFuelAbsorption();
        Assert.Throws<ArgumentException>(() =>
            t.Apply(MakePincellData(), "/materials/fuel",
                new Dictionary<string, string>()));
    }

    [Fact]
    public void Apply_rejects_non_numeric_factor()
    {
        var t = new ScaleFuelAbsorption();
        Assert.Throws<ArgumentException>(() =>
            t.Apply(MakePincellData(), "/materials/fuel",
                new Dictionary<string, string> { ["factor"] = "not-a-number" }));
    }

    [Fact]
    public void Apply_rejects_container_missing_sigma_a()
    {
        var t = new ScaleFuelAbsorption();
        var src = new Dictionary<string, object?>
        {
            ["fuel"] = new Dictionary<string, object?>
            {
                ["sigma_t"] = new List<object?> { 0.222, 0.833 },
            }
        };
        Assert.Throws<InvalidOperationException>(() =>
            t.Apply(src, "/fuel",
                new Dictionary<string, string> { ["factor"] = "1.5" }));
    }

    [Fact]
    public void Apply_rejects_mismatched_array_lengths()
    {
        var t = new ScaleFuelAbsorption();
        var src = new Dictionary<string, object?>
        {
            ["fuel"] = new Dictionary<string, object?>
            {
                ["sigma_a"] = new List<object?> { 0.01, 0.02, 0.03 },
                ["sigma_t"] = new List<object?> { 0.2, 0.8 },
            }
        };
        Assert.Throws<InvalidOperationException>(() =>
            t.Apply(src, "/fuel",
                new Dictionary<string, string> { ["factor"] = "1.5" }));
    }

    [Fact]
    public void Registry_resolves_ScaleFuelAbsorption()
    {
        Assert.IsType<ScaleFuelAbsorption>(TransformationRegistry.Get("ScaleFuelAbsorption"));
        Assert.Contains("ScaleFuelAbsorption", TransformationRegistry.AvailableNames);
    }
}
