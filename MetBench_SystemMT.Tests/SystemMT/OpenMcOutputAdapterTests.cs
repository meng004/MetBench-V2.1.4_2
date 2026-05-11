using MetBench_BLL.SystemMT;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT;

public sealed class OpenMcOutputAdapterTests
{
    private static string AdapterPath() =>
        Path.Combine(TestAssetPaths.AssetRoot(), "openmc", "openmc_output_adapter.py");

    private static string MakeOutputJson(double kEff = 1.13, double kEffStd = 0.0012,
        int batches = 60, int particles = 5000, bool converged = true)
    {
        var convergedLiteral = converged ? "true" : "false";
        return $$"""
            {
              "k_eff":      {{kEff}},
              "k_eff_std":  {{kEffStd}},
              "batches":    {{batches}},
              "particles":  {{particles}},
              "converged":  {{convergedLiteral}},
              "metadata":   { "runner": "openmc", "energy_mode": "multi-group" }
            }
            """;
    }

    private static string MakeWorkspace(string outputJson)
    {
        var dir = Path.Combine(Path.GetTempPath(), "MetBenchOpenMcOutputAdapterTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "result.json");
        File.WriteAllText(path, outputJson);
        return path;
    }

    [Fact]
    public async Task ParseAsync_returns_keff_std_batches_particles_and_metadata()
    {
        var path = MakeWorkspace(MakeOutputJson(kEff: 1.131, kEffStd: 0.0008, batches: 80, particles: 10000));
        var adapter = new PythonOutputAdapter(TestAssetPaths.PythonExecutable());

        var output = await adapter.ParseAsync(AdapterPath(), path, CancellationToken.None);

        Assert.Equal(1.131, output.Values["k_eff"]);
        Assert.Equal(0.0008, output.Values["k_eff_std"]);
        Assert.Equal(80, output.Values["batches"]);
        Assert.Equal(10000, output.Values["particles"]);
        Assert.Equal(1.0, output.Values["converged"]);
        Assert.Equal("openmc", output.Metadata["adapter"]);
    }

    [Fact]
    public async Task ParseAsync_emits_zero_for_unconverged()
    {
        var path = MakeWorkspace(MakeOutputJson(converged: false));
        var adapter = new PythonOutputAdapter(TestAssetPaths.PythonExecutable());

        var output = await adapter.ParseAsync(AdapterPath(), path, CancellationToken.None);

        Assert.Equal(0.0, output.Values["converged"]);
    }

    [Fact]
    public async Task ParseAsync_defaults_kEffStd_to_zero_when_absent()
    {
        var dir = Path.Combine(Path.GetTempPath(), "MetBenchOpenMcOutputAdapterTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "result.json");
        await File.WriteAllTextAsync(path,
            """
            {
              "k_eff": 1.0,
              "batches": 60,
              "particles": 5000,
              "converged": true
            }
            """);

        var adapter = new PythonOutputAdapter(TestAssetPaths.PythonExecutable());
        var output = await adapter.ParseAsync(AdapterPath(), path, CancellationToken.None);

        Assert.Equal(0.0, output.Values["k_eff_std"]);
    }

    [Fact]
    public async Task ParseAsync_rejects_non_bool_converged()
    {
        var dir = Path.Combine(Path.GetTempPath(), "MetBenchOpenMcOutputAdapterTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "result.json");
        await File.WriteAllTextAsync(path,
            """{ "k_eff": 1.0, "batches": 60, "particles": 5000, "converged": "yes" }""");

        var adapter = new PythonOutputAdapter(TestAssetPaths.PythonExecutable());

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            adapter.ParseAsync(AdapterPath(), path, CancellationToken.None));

        Assert.Contains("converged", error.Message);
    }

    [Fact]
    public async Task ParseAsync_rejects_missing_required_field()
    {
        var dir = Path.Combine(Path.GetTempPath(), "MetBenchOpenMcOutputAdapterTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "result.json");
        await File.WriteAllTextAsync(path, """{ "k_eff": 1.0 }""");

        var adapter = new PythonOutputAdapter(TestAssetPaths.PythonExecutable());

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            adapter.ParseAsync(AdapterPath(), path, CancellationToken.None));

        Assert.Contains("missing", error.Message);
    }
}
