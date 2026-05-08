using System.Text.Json;
using MetBench_BLL.SystemMT;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT;

public sealed class OpenMocSigmaAInputAdapterTests
{
    private static string AdapterPath() =>
        Path.Combine(TestAssetPaths.AssetRoot(), "openmoc", "openmoc_input_adapter_sigma_a.py");

    private static string MinimalSourceJson() =>
        """
        {
          "geometry": { "x_extent_cm": 1.26, "y_extent_cm": 1.26, "z_extent_cm": 1.0, "fuel_radius_cm": 0.4 },
          "tracking": { "num_azim": 4, "azim_spacing_cm": 0.1, "z_coord_cm": 0.0 },
          "solver":   { "convergence_threshold": 0.001, "max_iters": 50, "num_threads": 1 },
          "materials": {
            "fuel": {
              "num_groups": 2,
              "sigma_t":    [0.222222, 0.833333],
              "sigma_a":    [0.010120, 0.080032],
              "sigma_s":    [0.192423, 0.020000, 0.000000, 0.753300],
              "nu_sigma_f": [0.006400, 0.156500],
              "sigma_f":    [0.002500, 0.066600],
              "chi":        [1.000000, 0.000000]
            },
            "moderator": {
              "num_groups": 2,
              "sigma_t":    [0.230000, 1.530000],
              "sigma_a":    [0.000400, 0.020000],
              "sigma_s":    [0.219000, 0.010600, 0.000000, 1.510000],
              "nu_sigma_f": [0.0, 0.0],
              "sigma_f":    [0.0, 0.0],
              "chi":        [0.0, 0.0]
            }
          }
        }
        """;

    private static (string source, string followUp) MakeWorkspace(string sourceContent)
    {
        var dir = Path.Combine(Path.GetTempPath(), "MetBenchOpenMocSigmaAAdapterTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var source = Path.Combine(dir, "source.json");
        File.WriteAllText(source, sourceContent);
        return (source, Path.Combine(dir, "followup.json"));
    }

    [Fact]
    public async Task TransformAsync_scales_fuel_sigma_a_and_increases_sigma_t_by_delta()
    {
        var (source, followUp) = MakeWorkspace(MinimalSourceJson());

        var adapter = new PythonInputAdapter(TestAssetPaths.PythonExecutable());
        var transformation = new MrTransformation(
            "ScaleFuelSigmaA",
            new Dictionary<string, string> { ["factor"] = "1.5" });

        var log = await adapter.TransformAsync(AdapterPath(), source, followUp, transformation, CancellationToken.None);

        Assert.True(File.Exists(followUp));
        Assert.Contains("Scaled fuel.sigma_a by 1.5", log);

        using var produced = JsonDocument.Parse(await File.ReadAllTextAsync(followUp));
        var fuel = produced.RootElement.GetProperty("materials").GetProperty("fuel");

        // sigma_a per-group scaled by factor
        var sigmaA = fuel.GetProperty("sigma_a");
        Assert.Equal(0.010120 * 1.5, sigmaA[0].GetDouble(), 9);
        Assert.Equal(0.080032 * 1.5, sigmaA[1].GetDouble(), 9);

        // sigma_t increased by (factor - 1) * old_sigma_a per group
        var sigmaT = fuel.GetProperty("sigma_t");
        Assert.Equal(0.222222 + 0.5 * 0.010120, sigmaT[0].GetDouble(), 9);
        Assert.Equal(0.833333 + 0.5 * 0.080032, sigmaT[1].GetDouble(), 9);

        // Other arrays untouched.
        var sigmaS = fuel.GetProperty("sigma_s");
        Assert.Equal(0.192423, sigmaS[0].GetDouble(), 9);
        Assert.Equal(0.020000, sigmaS[1].GetDouble(), 9);
        Assert.Equal(0.000000, sigmaS[2].GetDouble(), 9);
        Assert.Equal(0.753300, sigmaS[3].GetDouble(), 9);

        var nuSigmaF = fuel.GetProperty("nu_sigma_f");
        Assert.Equal(0.006400, nuSigmaF[0].GetDouble(), 9);
        Assert.Equal(0.156500, nuSigmaF[1].GetDouble(), 9);

        // Moderator must remain bit-identical.
        var moderator = produced.RootElement.GetProperty("materials").GetProperty("moderator");
        Assert.Equal(0.230000, moderator.GetProperty("sigma_t")[0].GetDouble(), 9);
        Assert.Equal(0.0, moderator.GetProperty("nu_sigma_f")[0].GetDouble());
    }

    [Fact]
    public async Task TransformAsync_rejects_non_positive_factor()
    {
        var (source, followUp) = MakeWorkspace(MinimalSourceJson());

        var adapter = new PythonInputAdapter(TestAssetPaths.PythonExecutable());
        var transformation = new MrTransformation(
            "ScaleFuelSigmaA",
            new Dictionary<string, string> { ["factor"] = "0" });

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            adapter.TransformAsync(AdapterPath(), source, followUp, transformation, CancellationToken.None));

        Assert.Contains("Adapter failure", error.Message);
        Assert.Contains("factor", error.Message);
    }

    [Fact]
    public async Task TransformAsync_rejects_missing_factor_param()
    {
        var (source, followUp) = MakeWorkspace(MinimalSourceJson());

        var adapter = new PythonInputAdapter(TestAssetPaths.PythonExecutable());
        var transformation = new MrTransformation(
            "ScaleFuelSigmaA",
            new Dictionary<string, string>());

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            adapter.TransformAsync(AdapterPath(), source, followUp, transformation, CancellationToken.None));

        Assert.Contains("Adapter failure", error.Message);
    }
}
