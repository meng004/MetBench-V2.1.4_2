using MetBench_BLL.SystemMT.Transformations;
using Xunit;

namespace MetBench_SystemMT.Tests.V2Transformations;

/// <summary>
/// TDD 验证 P3.1 — 6 个 IMRTransformation 实现的 Apply round-trip。
/// </summary>
public sealed class IMRTransformationTests
{
    private static Dictionary<string, object?> MakePincellData() => new()
    {
        ["geometry"] = new Dictionary<string, object?>
        {
            ["x_extent_cm"] = 1.26,
            ["y_extent_cm"] = 1.26,
            ["fuel_offset_x_cm"] = 0.0,
            ["fuel_offset_y_cm"] = 0.0,
        },
        ["materials"] = new Dictionary<string, object?>
        {
            ["fuel"] = new Dictionary<string, object?>
            {
                ["temperature_kelvin"] = 600.0,
                ["nu_sigma_f"] = new List<object?> { 0.0046, 0.1114 },
                ["sigma_t"] = new List<object?> { 0.2222, 0.8333 },
            }
        }
    };

    // ============= IdentityTransform =============

    [Fact]
    public void Identity_does_not_change_data()
    {
        var t = new IdentityTransform();
        var source = MakePincellData();
        var result = t.Apply(source, "ignored", new Dictionary<string, string>());
        // Identity: 内容相等
        Assert.Equal(600.0, ((Dictionary<string, object?>)
            ((Dictionary<string, object?>)result["materials"]!)["fuel"]!)["temperature_kelvin"]);
    }

    [Fact]
    public void Identity_returns_independent_copy_top_level()
    {
        var t = new IdentityTransform();
        var source = MakePincellData();
        var result = t.Apply(source, "ignored", new Dictionary<string, string>());
        Assert.NotSame(source, result);
    }

    // ============= ScaleField =============

    [Fact]
    public void ScaleField_scalar()
    {
        var t = new ScaleField();
        var source = MakePincellData();
        var result = t.Apply(source, "materials.fuel.temperature_kelvin",
            new Dictionary<string, string> { ["factor"] = "1.5" });
        var fuel = (Dictionary<string, object?>)
            ((Dictionary<string, object?>)result["materials"]!)["fuel"]!;
        Assert.Equal(900.0, fuel["temperature_kelvin"]);
        // 源不变
        var srcFuel = (Dictionary<string, object?>)
            ((Dictionary<string, object?>)source["materials"]!)["fuel"]!;
        Assert.Equal(600.0, srcFuel["temperature_kelvin"]);
    }

    [Fact]
    public void ScaleField_array()
    {
        var t = new ScaleField();
        var source = MakePincellData();
        var result = t.Apply(source, "materials.fuel.nu_sigma_f",
            new Dictionary<string, string> { ["factor"] = "2.0" });
        var fuel = (Dictionary<string, object?>)
            ((Dictionary<string, object?>)result["materials"]!)["fuel"]!;
        var nsf = (List<object?>)fuel["nu_sigma_f"]!;
        Assert.Equal(0.0092, (double)nsf[0]!, 6);
        Assert.Equal(0.2228, (double)nsf[1]!, 6);
    }

    [Fact]
    public void ScaleField_rejects_missing_factor()
    {
        var t = new ScaleField();
        var source = MakePincellData();
        Assert.Throws<ArgumentException>(() =>
            t.Apply(source, "materials.fuel.temperature_kelvin", new Dictionary<string, string>()));
    }

    [Fact]
    public void ScaleField_rejects_non_numeric_factor()
    {
        var t = new ScaleField();
        var source = MakePincellData();
        Assert.Throws<ArgumentException>(() =>
            t.Apply(source, "materials.fuel.temperature_kelvin",
                new Dictionary<string, string> { ["factor"] = "not-a-number" }));
    }

    // ============= TranslateField =============

    [Fact]
    public void TranslateField_scalar()
    {
        var t = new TranslateField();
        var source = MakePincellData();
        var result = t.Apply(source, "geometry.fuel_offset_x_cm",
            new Dictionary<string, string> { ["offset"] = "0.15" });
        var geom = (Dictionary<string, object?>)result["geometry"]!;
        Assert.Equal(0.15, (double)geom["fuel_offset_x_cm"]!, 6);
    }

    [Fact]
    public void TranslateField_negative_offset()
    {
        var t = new TranslateField();
        var source = MakePincellData();
        var result = t.Apply(source, "geometry.fuel_offset_y_cm",
            new Dictionary<string, string> { ["offset"] = "-0.1" });
        var geom = (Dictionary<string, object?>)result["geometry"]!;
        Assert.Equal(-0.1, (double)geom["fuel_offset_y_cm"]!, 6);
    }

    // ============= PermuteIndices =============

    [Fact]
    public void PermuteIndices_swaps_group_order()
    {
        var t = new PermuteIndices();
        var source = MakePincellData();
        var result = t.Apply(source, "materials.fuel.nu_sigma_f",
            new Dictionary<string, string> { ["permutation"] = "1,0" });
        var fuel = (Dictionary<string, object?>)
            ((Dictionary<string, object?>)result["materials"]!)["fuel"]!;
        var nsf = (List<object?>)fuel["nu_sigma_f"]!;
        Assert.Equal(0.1114, (double)nsf[0]!, 6);  // 原 [1] → 新 [0]
        Assert.Equal(0.0046, (double)nsf[1]!, 6);  // 原 [0] → 新 [1]
    }

    [Fact]
    public void PermuteIndices_rejects_invalid_permutation()
    {
        var t = new PermuteIndices();
        var source = MakePincellData();
        // [1,1] 不是合法 permutation
        Assert.Throws<ArgumentException>(() =>
            t.Apply(source, "materials.fuel.nu_sigma_f",
                new Dictionary<string, string> { ["permutation"] = "1,1" }));
    }

    [Fact]
    public void PermuteIndices_rejects_wrong_length()
    {
        var t = new PermuteIndices();
        var source = MakePincellData();
        Assert.Throws<ArgumentException>(() =>
            t.Apply(source, "materials.fuel.nu_sigma_f",
                new Dictionary<string, string> { ["permutation"] = "0,1,2" }));
    }

    // ============= MirrorAxis =============

    [Fact]
    public void MirrorAxis_negates_scalar()
    {
        var t = new MirrorAxis();
        var source = new Dictionary<string, object?>
        {
            ["geometry"] = new Dictionary<string, object?>
            {
                ["fuel_offset_x_cm"] = 0.15
            }
        };
        var result = t.Apply(source, "geometry.fuel_offset_x_cm",
            new Dictionary<string, string>());
        var geom = (Dictionary<string, object?>)result["geometry"]!;
        Assert.Equal(-0.15, (double)geom["fuel_offset_x_cm"]!, 6);
    }

    [Fact]
    public void MirrorAxis_handles_zero()
    {
        var t = new MirrorAxis();
        var source = new Dictionary<string, object?>
        {
            ["v"] = 0.0
        };
        var result = t.Apply(source, "v", new Dictionary<string, string>());
        Assert.Equal(0.0, (double)result["v"]!, 9);
    }

    // ============= CompositeTransform =============

    [Fact]
    public void CompositeTransform_applies_steps_in_order()
    {
        var steps = new List<CompositeTransform.Step>
        {
            new(new ScaleField(),
                "materials.fuel.temperature_kelvin",
                new Dictionary<string, string> { ["factor"] = "1.5" }),
            new(new TranslateField(),
                "materials.fuel.temperature_kelvin",
                new Dictionary<string, string> { ["offset"] = "50.0" }),
        };
        var t = new CompositeTransform("ScaleThenTranslate", steps);
        var source = MakePincellData();
        var result = t.Apply(source, "ignored", new Dictionary<string, string>());

        var fuel = (Dictionary<string, object?>)
            ((Dictionary<string, object?>)result["materials"]!)["fuel"]!;
        // 600 × 1.5 = 900；900 + 50 = 950
        Assert.Equal(950.0, fuel["temperature_kelvin"]);
    }

    [Fact]
    public void CompositeTransform_requires_at_least_one_step()
    {
        Assert.Throws<ArgumentException>(() =>
            new CompositeTransform("empty", new List<CompositeTransform.Step>()));
    }

    [Fact]
    public void CompositeTransform_uses_call_time_params_when_step_params_empty()
    {
        // launcher 的多步 MR（decay-chain / damped-oscillator）用空 step.Parameters
        // 构造,运行期靠 PipelineContext.Parameters（含 factor）驱动每一步。
        var steps = new List<CompositeTransform.Step>
        {
            new(new ScaleField(),
                "materials.fuel.temperature_kelvin",
                new Dictionary<string, string>()),  // 空 step.Parameters
        };
        var t = new CompositeTransform("CallTimeFactor", steps);
        var source = MakePincellData();

        var result = t.Apply(source, "ignored",
            new Dictionary<string, string> { ["factor"] = "2.0" });

        var fuel = (Dictionary<string, object?>)
            ((Dictionary<string, object?>)result["materials"]!)["fuel"]!;
        Assert.Equal(1200.0, fuel["temperature_kelvin"]);  // 600 × 2.0
    }

    [Fact]
    public void CompositeTransform_prefers_step_params_when_both_present()
    {
        // 既有行为不变:step.Parameters 非空时,call-time params 被忽略。
        var steps = new List<CompositeTransform.Step>
        {
            new(new ScaleField(),
                "materials.fuel.temperature_kelvin",
                new Dictionary<string, string> { ["factor"] = "1.5" }),  // step 固化 1.5
        };
        var t = new CompositeTransform("StepWins", steps);
        var source = MakePincellData();

        var result = t.Apply(source, "ignored",
            new Dictionary<string, string> { ["factor"] = "999" });  // 应被忽略

        var fuel = (Dictionary<string, object?>)
            ((Dictionary<string, object?>)result["materials"]!)["fuel"]!;
        Assert.Equal(900.0, fuel["temperature_kelvin"]);  // 600 × 1.5,非 599400
    }

    // ============= TransformationRegistry =============

    [Fact]
    public void Registry_resolves_5_builtin_transforms()
    {
        Assert.IsType<IdentityTransform>(TransformationRegistry.Get("Identity"));
        Assert.IsType<ScaleField>(TransformationRegistry.Get("ScaleField"));
        Assert.IsType<TranslateField>(TransformationRegistry.Get("TranslateField"));
        Assert.IsType<PermuteIndices>(TransformationRegistry.Get("PermuteIndices"));
        Assert.IsType<MirrorAxis>(TransformationRegistry.Get("MirrorAxis"));
    }

    [Fact]
    public void Registry_rejects_unknown_name()
    {
        Assert.Throws<KeyNotFoundException>(() =>
            TransformationRegistry.Get("NotARealTransform"));
    }

    [Fact]
    public void Registry_lists_5_builtin_names()
    {
        var names = TransformationRegistry.AvailableNames;
        Assert.Contains("Identity", names);
        Assert.Contains("ScaleField", names);
        Assert.Contains("TranslateField", names);
        Assert.Contains("PermuteIndices", names);
        Assert.Contains("MirrorAxis", names);
    }
}
