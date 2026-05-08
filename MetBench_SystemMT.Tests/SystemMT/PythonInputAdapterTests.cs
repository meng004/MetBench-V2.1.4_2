using MetBench_BLL.SystemMT;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT;

public sealed class PythonInputAdapterTests
{
    [Fact]
    public async Task TransformAsync_writes_followup_file_and_returns_log()
    {
        var workDir = Path.Combine(Path.GetTempPath(), "MetBenchSystemMt", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workDir);
        var sourcePath = Path.Combine(workDir, "source.txt");
        var followUpPath = Path.Combine(workDir, "followup.txt");
        await File.WriteAllTextAsync(sourcePath, "3", CancellationToken.None);

        var adapter = new PythonInputAdapter(TestAssetPaths.PythonExecutable());
        var transformation = new MrTransformation(
            "ScalarMultiply",
            new Dictionary<string, string> { ["multiplier"] = "2.5" });

        var log = await adapter.TransformAsync(
            Path.Combine(TestAssetPaths.AssetRoot(), "example_output_adapter.py"),
            sourcePath,
            followUpPath,
            transformation,
            CancellationToken.None);

        Assert.True(File.Exists(followUpPath));
        Assert.Contains("Multiplied 3.0 by 2.5", log);
        Assert.Equal("7.5", (await File.ReadAllTextAsync(followUpPath, CancellationToken.None)).Trim());
    }

    [Fact]
    public async Task TransformAsync_reports_missing_source_file()
    {
        var adapter = new PythonInputAdapter(TestAssetPaths.PythonExecutable());
        var transformation = new MrTransformation(
            "ScalarMultiply",
            new Dictionary<string, string> { ["multiplier"] = "2" });

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            adapter.TransformAsync(
                Path.Combine(TestAssetPaths.AssetRoot(), "example_output_adapter.py"),
                Path.Combine(Path.GetTempPath(), "missing-source.txt"),
                Path.Combine(Path.GetTempPath(), "followup.txt"),
                transformation,
                CancellationToken.None));

        Assert.Contains("source input file does not exist", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TransformAsync_propagates_adapter_failures()
    {
        var workDir = Path.Combine(Path.GetTempPath(), "MetBenchSystemMt", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workDir);
        var sourcePath = Path.Combine(workDir, "empty.txt");
        await File.WriteAllTextAsync(sourcePath, "", CancellationToken.None);

        var adapter = new PythonInputAdapter(TestAssetPaths.PythonExecutable());
        var transformation = new MrTransformation(
            "ScalarMultiply",
            new Dictionary<string, string> { ["multiplier"] = "2" });

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            adapter.TransformAsync(
                Path.Combine(TestAssetPaths.AssetRoot(), "example_output_adapter.py"),
                sourcePath,
                Path.Combine(workDir, "followup.txt"),
                transformation,
                CancellationToken.None));

        Assert.Contains("Adapter failure", error.Message);
    }
}
