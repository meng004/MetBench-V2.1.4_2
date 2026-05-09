using MetBench_BLL.SystemMT;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT;

public sealed class HeatEquationOutputAdapterTests
{
    private static string AdapterPath() =>
        Path.Combine(TestAssetPaths.AssetRoot(), "heat_equation", "heat_equation_output_adapter.py");

    private static string MakeOutputJson(double maxU = 0.5, double minU = 0.0, double l2Norm = 0.3,
        double integral = 0.1, int numSteps = 800, double stabilityRatio = 0.05,
        bool stabilityViolated = false, double tFinal = 0.2)
    {
        var violated = stabilityViolated ? "true" : "false";
        return $$"""
            {
              "t_final":             {{tFinal}},
              "max_u":               {{maxU}},
              "min_u":               {{minU}},
              "l2_norm":             {{l2Norm}},
              "integral":            {{integral}},
              "num_steps":           {{numSteps}},
              "stability_ratio":     {{stabilityRatio}},
              "stability_violated":  {{violated}},
              "x":                   [0.0, 0.5, 1.0],
              "u":                   [0.0, {{maxU}}, 0.0]
            }
            """;
    }

    private static string MakeWorkspace(string outputJson)
    {
        var dir = Path.Combine(Path.GetTempPath(), "MetBenchHeatEqOutputAdapterTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "result.json");
        File.WriteAllText(path, outputJson);
        return path;
    }

    [Fact]
    public async Task ParseAsync_returns_max_l2_integral_and_stability_metrics()
    {
        var path = MakeWorkspace(MakeOutputJson(maxU: 0.874, integral: 0.07, l2Norm: 0.35));
        var adapter = new PythonOutputAdapter(TestAssetPaths.PythonExecutable());

        var output = await adapter.ParseAsync(AdapterPath(), path, CancellationToken.None);

        Assert.Equal(0.874, output.Values["max_u"]);
        Assert.Equal(0.0, output.Values["min_u"]);
        Assert.Equal(0.35, output.Values["l2_norm"]);
        Assert.Equal(0.07, output.Values["integral"]);
        Assert.Equal(800, output.Values["num_steps"]);
        Assert.Equal(0.05, output.Values["stability_ratio"]);
        Assert.Equal(0.0, output.Values["stability_violated"]);
        Assert.Equal("heat_equation", output.Metadata["program"]);
        Assert.Equal("0.2", output.Metadata["t_final"]);
    }

    [Fact]
    public async Task ParseAsync_emits_one_when_stability_violated()
    {
        var path = MakeWorkspace(MakeOutputJson(stabilityViolated: true, stabilityRatio: 0.7));
        var adapter = new PythonOutputAdapter(TestAssetPaths.PythonExecutable());

        var output = await adapter.ParseAsync(AdapterPath(), path, CancellationToken.None);

        Assert.Equal(1.0, output.Values["stability_violated"]);
        Assert.Equal(0.7, output.Values["stability_ratio"]);
    }

    [Fact]
    public async Task ParseAsync_rejects_missing_field()
    {
        var dir = Path.Combine(Path.GetTempPath(), "MetBenchHeatEqOutputAdapterTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "result.json");
        await File.WriteAllTextAsync(path, """{ "max_u": 0.5 }""");

        var adapter = new PythonOutputAdapter(TestAssetPaths.PythonExecutable());

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            adapter.ParseAsync(AdapterPath(), path, CancellationToken.None));

        Assert.Contains("missing", error.Message);
    }
}
