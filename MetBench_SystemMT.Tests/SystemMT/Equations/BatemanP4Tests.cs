using MetBench_BLL.Equations;
using MetBench_BLL.Equations.Bateman;
using Xunit;
using SysMath = System.Math;

namespace MetBench_SystemMT.Tests.SystemMT.Equations;

/// <summary>
/// P4 — Bateman 样板方程：BatemanAnalyticSolution L2 单元测试
///       + bateman.ScaleInitial L1 Recipe 经 TransformationResolver 可解析。
/// </summary>
public sealed class BatemanP4Tests
{
    // ── sample parameters ────────────────────────────────────────────────────
    // 与 SUT/decay_chain/sample/three_nuclide.json 一致
    private const double N_A0 = 1000.0, N_B0 = 0.0, N_C0 = 0.0;
    private const double LambdaA = 0.5, LambdaB = 0.2, T = 10.0;

    private static IReadOnlyDictionary<string, object?> SampleData() =>
        new Dictionary<string, object?>
        {
            ["initial"] = new Dictionary<string, object?>
                { ["N_A"] = N_A0, ["N_B"] = N_B0, ["N_C"] = N_C0 },
            ["params"] = new Dictionary<string, object?>
                { ["lambda_A"] = LambdaA, ["lambda_B"] = LambdaB, ["t_final"] = T },
        };

    // ── BatemanAnalyticSolution L2 ───────────────────────────────────────────

    [Fact]
    public void BatemanAnalytic_N_A_matches_formula()
    {
        var fn = new BatemanAnalyticSolution();
        var result = fn.Execute(SampleData(), new Dictionary<string, string>());

        var expected = N_A0 * SysMath.Exp(-LambdaA * T);
        var actual = (double)result["N_A"]!;
        Assert.Equal(expected, actual, precision: 9);
    }

    [Fact]
    public void BatemanAnalytic_N_B_matches_formula()
    {
        var fn = new BatemanAnalyticSolution();
        var result = fn.Execute(SampleData(), new Dictionary<string, string>());

        // N_B = N_A0 * λ_A/(λ_B-λ_A) * (e^{-λ_At} - e^{-λ_Bt}) + N_B0*e^{-λ_Bt}
        var expected = N_A0 * LambdaA / (LambdaB - LambdaA)
                       * (SysMath.Exp(-LambdaA * T) - SysMath.Exp(-LambdaB * T))
                       + N_B0 * SysMath.Exp(-LambdaB * T);
        var actual = (double)result["N_B"]!;
        Assert.Equal(expected, actual, precision: 9);
    }

    [Fact]
    public void BatemanAnalytic_N_C_satisfies_conservation()
    {
        // C stable (no λ_C): total N conserved → N_C = total - N_A(t) - N_B(t)
        var fn = new BatemanAnalyticSolution();
        var result = fn.Execute(SampleData(), new Dictionary<string, string>());

        var total = N_A0 + N_B0 + N_C0;
        var nA = (double)result["N_A"]!;
        var nB = (double)result["N_B"]!;
        var nC = (double)result["N_C"]!;

        Assert.Equal(total - nA - nB, nC, precision: 9);
    }

    [Fact]
    public void BatemanAnalytic_metadata()
    {
        var fn = new BatemanAnalyticSolution();
        Assert.Equal("bateman", fn.EquationKey);
        Assert.Equal("AnalyticSolution", fn.Name);
        Assert.Equal("solution", fn.Kind);
    }

    // ── bateman.ScaleInitial Recipe resolves via TransformationResolver ────

    private static EquationFunctionRegistry BuildRegistry()
    {
        const string recipeJson = """
            {"compose":[
              {"op":"ScaleField","path":"/initial/N_A","params":{"factor":"{factor}"}},
              {"op":"ScaleField","path":"/initial/N_B","params":{"factor":"{factor}"}},
              {"op":"ScaleField","path":"/initial/N_C","params":{"factor":"{factor}"}}
            ]}
            """;
        var reg = new EquationFunctionRegistry();
        reg.Register(new RecipeBasedEquationFunction("bateman", "ScaleInitial", "transformation", recipeJson));
        return reg;
    }

    [Fact]
    public void ScaleInitial_resolves_as_equation_function()
    {
        var resolver = new TransformationResolver(BuildRegistry());
        var t = resolver.Resolve("ScaleInitial", "bateman");
        Assert.NotNull(t);
        Assert.Equal("ScaleInitial", t.Name);
    }

    [Fact]
    public void ScaleInitial_scales_all_three_nuclides()
    {
        var resolver = new TransformationResolver(BuildRegistry());
        var t = resolver.Resolve("ScaleInitial", "bateman");

        var sourceData = new Dictionary<string, object?>
        {
            ["initial"] = new Dictionary<string, object?>
                { ["N_A"] = 100.0, ["N_B"] = 200.0, ["N_C"] = 50.0 }
        };
        var result = t.Apply(sourceData, "", new Dictionary<string, string> { ["factor"] = "2" });

        var initial = (Dictionary<string, object?>)result["initial"]!;
        Assert.Equal(200.0, (double)initial["N_A"]!);
        Assert.Equal(400.0, (double)initial["N_B"]!);
        Assert.Equal(100.0, (double)initial["N_C"]!);
    }

    [Fact]
    public void ScaleInitial_not_in_global_registry()
    {
        // 决策 B: ScaleInitial 应只在方程命名空间，不污染全局
        Assert.DoesNotContain("ScaleInitial",
            MetBench_BLL.SystemMT.Transformations.TransformationRegistry.AvailableNames);
    }
}
