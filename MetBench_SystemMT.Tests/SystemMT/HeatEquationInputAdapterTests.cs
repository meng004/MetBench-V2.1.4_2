using System.Text.Json;
using MetBench_BLL.SystemMT;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT;

public sealed class HeatEquationInputAdapterTests
{
    private static string AdapterPath() =>
        Path.Combine(TestAssetPaths.AssetRoot(), "heat_equation", "heat_equation_input_adapter.py");

    private static string MinimalSourceJson() =>
        """
        {
          "initial": {
            "x_min":      0.0,
            "x_max":      1.0,
            "num_points": 51,
            "profile":    "gaussian",
            "amplitude":  1.0,
            "center":     0.5,
            "width":      0.08
          },
          "params": {
            "alpha":     0.005,
            "t_final":   0.2,
            "num_steps": 800
          }
        }
        """;

    private static (string source, string followUp) MakeWorkspace(string sourceContent)
    {
        var dir = Path.Combine(Path.GetTempPath(), "MetBenchHeatEqInputAdapterTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var source = Path.Combine(dir, "source.json");
        File.WriteAllText(source, sourceContent);
        return (source, Path.Combine(dir, "followup.json"));
    }

    [Fact]
    public async Task TransformAsync_scales_initial_amplitude_and_preserves_other_fields()
    {
        var (source, followUp) = MakeWorkspace(MinimalSourceJson());

        var adapter = new PythonInputAdapter(TestAssetPaths.PythonExecutable());
        var transformation = new MrTransformation(
            "ScaleAmplitude",
            new Dictionary<string, string> { ["factor"] = "2.5" });

        var log = await adapter.TransformAsync(AdapterPath(), source, followUp, transformation, CancellationToken.None);

        Assert.True(File.Exists(followUp));
        Assert.Contains("Scaled initial.amplitude by 2.5", log);

        using var produced = JsonDocument.Parse(await File.ReadAllTextAsync(followUp));
        var initial = produced.RootElement.GetProperty("initial");
        Assert.Equal(2.5, initial.GetProperty("amplitude").GetDouble());
        // Non-amplitude fields untouched.
        Assert.Equal(0.0, initial.GetProperty("x_min").GetDouble());
        Assert.Equal(1.0, initial.GetProperty("x_max").GetDouble());
        Assert.Equal(51, initial.GetProperty("num_points").GetInt32());
        Assert.Equal("gaussian", initial.GetProperty("profile").GetString());
        Assert.Equal(0.5, initial.GetProperty("center").GetDouble());
        Assert.Equal(0.08, initial.GetProperty("width").GetDouble());
        var parameters = produced.RootElement.GetProperty("params");
        Assert.Equal(0.005, parameters.GetProperty("alpha").GetDouble());
        Assert.Equal(0.2, parameters.GetProperty("t_final").GetDouble());
        Assert.Equal(800, parameters.GetProperty("num_steps").GetInt32());
    }

    [Fact]
    public async Task TransformAsync_rejects_non_positive_factor()
    {
        var (source, followUp) = MakeWorkspace(MinimalSourceJson());

        var adapter = new PythonInputAdapter(TestAssetPaths.PythonExecutable());
        var transformation = new MrTransformation(
            "ScaleAmplitude",
            new Dictionary<string, string> { ["factor"] = "-1" });

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
            "ScaleAmplitude",
            new Dictionary<string, string>());

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            adapter.TransformAsync(AdapterPath(), source, followUp, transformation, CancellationToken.None));

        Assert.Contains("Adapter failure", error.Message);
    }
}
