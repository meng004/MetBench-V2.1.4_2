using System.Text.Json;
using MetBench_BLL.SystemMT;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT;

public sealed class OpenMcInputAdapterTests
{
    private static string NuSigmaFAdapter() =>
        Path.Combine(TestAssetPaths.AssetRoot(), "openmc", "openmc_input_adapter.py");

    private static string SigmaAAdapter() =>
        Path.Combine(TestAssetPaths.AssetRoot(), "openmc", "openmc_input_adapter_sigma_a.py");

    private static string SamplePincell() =>
        Path.Combine(TestAssetPaths.AssetRoot(), "openmc", "sample", "pincell.json");

    private static (string source, string followUp) MakeWorkspace(string sourceContent)
    {
        var dir = Path.Combine(Path.GetTempPath(), "MetBenchOpenMcInputAdapterTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var source = Path.Combine(dir, "source.json");
        File.WriteAllText(source, sourceContent);
        return (source, Path.Combine(dir, "followup.json"));
    }

    [Fact]
    public async Task NuSigmaF_scales_fuel_array_and_preserves_other_fields()
    {
        var (source, followUp) = MakeWorkspace(File.ReadAllText(SamplePincell()));

        var adapter = new PythonInputAdapter(TestAssetPaths.PythonExecutable());
        var transformation = new MrTransformation(
            "ScaleNuSigmaF",
            new Dictionary<string, string> { ["factor"] = "2.0" });

        var log = await adapter.TransformAsync(NuSigmaFAdapter(), source, followUp, transformation, CancellationToken.None);

        Assert.True(File.Exists(followUp));
        Assert.Contains("Scaled fuel.nu_sigma_f by 2.0", log);

        using var produced = JsonDocument.Parse(await File.ReadAllTextAsync(followUp));
        var fuel = produced.RootElement.GetProperty("materials").GetProperty("fuel");

        // nu_sigma_f doubled
        var newNsf = fuel.GetProperty("nu_sigma_f").EnumerateArray().Select(e => e.GetDouble()).ToArray();
        Assert.Equal(2, newNsf.Length);
        Assert.Equal(0.0128, newNsf[0], 6);  // 0.006400 * 2
        Assert.Equal(0.3130, newNsf[1], 6);  // 0.156500 * 2

        // sigma_a, sigma_t, sigma_s unchanged on fuel
        Assert.Equal(0.010120, fuel.GetProperty("sigma_a")[0].GetDouble(), 6);
        Assert.Equal(0.222222, fuel.GetProperty("sigma_t")[0].GetDouble(), 6);
        Assert.Equal(0.192423, fuel.GetProperty("sigma_s")[0].GetDouble(), 6);

        // Moderator entirely unchanged
        var mod = produced.RootElement.GetProperty("materials").GetProperty("moderator");
        Assert.Equal(0.230000, mod.GetProperty("sigma_t")[0].GetDouble(), 6);
        Assert.Equal(0.000000, mod.GetProperty("nu_sigma_f")[0].GetDouble(), 6);
    }

    [Fact]
    public async Task NuSigmaF_rejects_non_positive_factor()
    {
        var (source, followUp) = MakeWorkspace(File.ReadAllText(SamplePincell()));

        var adapter = new PythonInputAdapter(TestAssetPaths.PythonExecutable());
        var transformation = new MrTransformation(
            "ScaleNuSigmaF",
            new Dictionary<string, string> { ["factor"] = "-1" });

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            adapter.TransformAsync(NuSigmaFAdapter(), source, followUp, transformation, CancellationToken.None));

        Assert.Contains("Adapter failure", error.Message);
        Assert.Contains("factor", error.Message);
    }

    [Fact]
    public async Task NuSigmaF_rejects_missing_factor()
    {
        var (source, followUp) = MakeWorkspace(File.ReadAllText(SamplePincell()));

        var adapter = new PythonInputAdapter(TestAssetPaths.PythonExecutable());
        var transformation = new MrTransformation("ScaleNuSigmaF", new Dictionary<string, string>());

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            adapter.TransformAsync(NuSigmaFAdapter(), source, followUp, transformation, CancellationToken.None));

        Assert.Contains("Adapter failure", error.Message);
    }

    [Fact]
    public async Task SigmaA_scales_fuel_absorption_and_updates_total_consistently()
    {
        var (source, followUp) = MakeWorkspace(File.ReadAllText(SamplePincell()));

        var adapter = new PythonInputAdapter(TestAssetPaths.PythonExecutable());
        var transformation = new MrTransformation(
            "ScaleFuelSigmaA",
            new Dictionary<string, string> { ["factor"] = "1.5" });

        var log = await adapter.TransformAsync(SigmaAAdapter(), source, followUp, transformation, CancellationToken.None);

        Assert.True(File.Exists(followUp));
        Assert.Contains("Scaled fuel.sigma_a by 1.5", log);
        Assert.Contains("updated sigma_t", log);

        using var produced = JsonDocument.Parse(await File.ReadAllTextAsync(followUp));
        var fuel = produced.RootElement.GetProperty("materials").GetProperty("fuel");

        // sigma_a multiplied by 1.5
        var newSa = fuel.GetProperty("sigma_a").EnumerateArray().Select(e => e.GetDouble()).ToArray();
        Assert.Equal(0.01518, newSa[0], 6);   // 0.010120 * 1.5
        Assert.Equal(0.120048, newSa[1], 6);  // 0.080032 * 1.5

        // sigma_t bumped by (factor - 1) * old_sigma_a
        var newSt = fuel.GetProperty("sigma_t").EnumerateArray().Select(e => e.GetDouble()).ToArray();
        Assert.Equal(0.222222 + 0.5 * 0.010120, newSt[0], 6);
        Assert.Equal(0.833333 + 0.5 * 0.080032, newSt[1], 6);

        // nu_sigma_f, sigma_f, sigma_s unchanged
        Assert.Equal(0.006400, fuel.GetProperty("nu_sigma_f")[0].GetDouble(), 6);
        Assert.Equal(0.002500, fuel.GetProperty("sigma_f")[0].GetDouble(), 6);
        Assert.Equal(0.192423, fuel.GetProperty("sigma_s")[0].GetDouble(), 6);
    }

    [Fact]
    public async Task SigmaA_rejects_non_positive_factor()
    {
        var (source, followUp) = MakeWorkspace(File.ReadAllText(SamplePincell()));

        var adapter = new PythonInputAdapter(TestAssetPaths.PythonExecutable());
        var transformation = new MrTransformation(
            "ScaleFuelSigmaA",
            new Dictionary<string, string> { ["factor"] = "0" });

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            adapter.TransformAsync(SigmaAAdapter(), source, followUp, transformation, CancellationToken.None));

        Assert.Contains("factor", error.Message);
    }
}
