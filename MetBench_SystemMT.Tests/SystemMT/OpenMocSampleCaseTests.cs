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

        var moderator = root.GetProperty("materials").GetProperty("moderator");
        Assert.Equal(2, moderator.GetProperty("num_groups").GetInt32());
        Assert.Equal(0.0, moderator.GetProperty("nu_sigma_f")[0].GetDouble());
        Assert.Equal(0.0, moderator.GetProperty("nu_sigma_f")[1].GetDouble());
    }
}
