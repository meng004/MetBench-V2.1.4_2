using System.Text.Json;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT;

public sealed class OpenMocSampleCaseTests
{
    private static string SampleCasePath() =>
        Path.Combine(TestAssetPaths.AssetRoot(), "openmoc", "sample", "pincell.json");

    [Fact]
    public void Sample_pincell_json_satisfies_runner_contract()
    {
        Assert.True(File.Exists(SampleCasePath()),
            $"Stage 3 sample case missing: {SampleCasePath()}");

        using var doc = JsonDocument.Parse(File.ReadAllText(SampleCasePath()));
        var root = doc.RootElement;

        var geometry = root.GetProperty("geometry");
        Assert.True(geometry.GetProperty("fuel_radius_cm").GetDouble() > 0);
        Assert.True(geometry.GetProperty("x_extent_cm").GetDouble() > 0);
        Assert.True(geometry.GetProperty("y_extent_cm").GetDouble() > 0);

        var fuel = root.GetProperty("materials").GetProperty("fuel");
        Assert.Equal(2, fuel.GetProperty("num_groups").GetInt32());
        Assert.Equal(2, fuel.GetProperty("nu_sigma_f").GetArrayLength());
        Assert.Equal(2, fuel.GetProperty("sigma_a").GetArrayLength());
        Assert.Equal(2, fuel.GetProperty("sigma_t").GetArrayLength());
        Assert.Equal(4, fuel.GetProperty("sigma_s").GetArrayLength());
        Assert.Equal(2, fuel.GetProperty("chi").GetArrayLength());

        // Fuel must be fissile (otherwise the MR has nothing to scale).
        Assert.True(fuel.GetProperty("nu_sigma_f")[0].GetDouble() > 0,
            "fuel.nu_sigma_f[0] must be > 0 for the MR to be meaningful");
        Assert.True(fuel.GetProperty("nu_sigma_f")[1].GetDouble() > 0,
            "fuel.nu_sigma_f[1] must be > 0 for the MR to be meaningful");
        Assert.Equal(1.0, fuel.GetProperty("chi")[0].GetDouble());

        var moderator = root.GetProperty("materials").GetProperty("moderator");
        Assert.Equal(2, moderator.GetProperty("num_groups").GetInt32());
        // Moderator must be non-fissile so 'ScaleNuSigmaF' on fuel really
        // is the only thing that changes between source and follow-up.
        Assert.Equal(0.0, moderator.GetProperty("nu_sigma_f")[0].GetDouble());
        Assert.Equal(0.0, moderator.GetProperty("nu_sigma_f")[1].GetDouble());
        Assert.Equal(0.0, moderator.GetProperty("sigma_f")[0].GetDouble());
        Assert.Equal(0.0, moderator.GetProperty("sigma_f")[1].GetDouble());

        var solver = root.GetProperty("solver");
        Assert.True(solver.GetProperty("convergence_threshold").GetDouble() > 0);
        Assert.True(solver.GetProperty("max_iters").GetInt32() > 0);
    }
}
