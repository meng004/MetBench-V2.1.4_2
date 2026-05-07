using MetBench_BLL.SystemMT;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT;

public sealed class PythonOutputAdapterTests
{
    [Fact]
    public async Task ParseAsync_returns_normalized_values()
    {
        var workDir = Path.Combine(Path.GetTempPath(), "MetBenchSystemMt", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workDir);
        var outputPath = Path.Combine(workDir, "output.txt");
        await File.WriteAllTextAsync(outputPath, "result=12.5\n", CancellationToken.None);

        var adapter = new PythonOutputAdapter(TestAssetPaths.PythonExecutable());
        var parsed = await adapter.ParseAsync(
            Path.Combine(TestAssetPaths.AssetRoot(), "example_output_adapter.py"),
            outputPath,
            CancellationToken.None);

        Assert.Equal(12.5, parsed.Values["result"]);
        Assert.Equal("example", parsed.Metadata["adapter"]);
    }

    [Fact]
    public async Task ParseAsync_reports_missing_output_file()
    {
        var adapter = new PythonOutputAdapter(TestAssetPaths.PythonExecutable());

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            adapter.ParseAsync(
                Path.Combine(TestAssetPaths.AssetRoot(), "example_output_adapter.py"),
                Path.Combine(Path.GetTempPath(), "missing-output.txt"),
                CancellationToken.None));

        Assert.Contains("Output artifact failure", error.Message);
    }
}
