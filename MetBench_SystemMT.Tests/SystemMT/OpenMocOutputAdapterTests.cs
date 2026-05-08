using MetBench_BLL.SystemMT;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT;

public sealed class OpenMocOutputAdapterTests
{
    private static string AdapterPath() =>
        Path.Combine(TestAssetPaths.AssetRoot(), "openmoc", "openmoc_output_adapter.py");

    private static string FreshWorkDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "MetBenchOpenMocOutputAdapterTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    [Fact]
    public async Task ParseAsync_returns_keff_iterations_and_converged()
    {
        var workDir = FreshWorkDir();
        var outputPath = Path.Combine(workDir, "output.json");
        await File.WriteAllTextAsync(outputPath,
            """
            {
              "k_eff": 1.234,
              "iterations": 7,
              "converged": true,
              "metadata": { "runner": "openmoc" }
            }
            """,
            CancellationToken.None);

        var adapter = new PythonOutputAdapter(TestAssetPaths.PythonExecutable());
        var parsed = await adapter.ParseAsync(AdapterPath(), outputPath, CancellationToken.None);

        Assert.Equal(1.234, parsed.Values["k_eff"], 9);
        Assert.Equal(7.0, parsed.Values["iterations"]);
        Assert.Equal(1.0, parsed.Values["converged"]);
        Assert.Equal("openmoc", parsed.Metadata["adapter"]);
        Assert.Equal(Path.GetFullPath(outputPath), parsed.Metadata["outputFile"]);
    }

    [Fact]
    public async Task ParseAsync_emits_zero_for_unconverged()
    {
        var workDir = FreshWorkDir();
        var outputPath = Path.Combine(workDir, "output.json");
        await File.WriteAllTextAsync(outputPath,
            """
            { "k_eff": 0.95, "iterations": 50, "converged": false, "metadata": {} }
            """,
            CancellationToken.None);

        var adapter = new PythonOutputAdapter(TestAssetPaths.PythonExecutable());
        var parsed = await adapter.ParseAsync(AdapterPath(), outputPath, CancellationToken.None);

        Assert.Equal(0.0, parsed.Values["converged"]);
    }

    [Fact]
    public async Task ParseAsync_reports_malformed_json_as_adapter_failure()
    {
        var workDir = FreshWorkDir();
        var outputPath = Path.Combine(workDir, "output.json");
        await File.WriteAllTextAsync(outputPath, "not json at all", CancellationToken.None);

        var adapter = new PythonOutputAdapter(TestAssetPaths.PythonExecutable());

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            adapter.ParseAsync(AdapterPath(), outputPath, CancellationToken.None));

        Assert.Contains("Adapter failure", error.Message);
    }
}
