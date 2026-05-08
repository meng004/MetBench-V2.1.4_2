using MetBench_BLL.SystemMT;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT;

public sealed class InputGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_returns_success_result_with_followup_path_and_log()
    {
        var workDir = Path.Combine(Path.GetTempPath(), "MetBenchSystemMt", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workDir);
        var sourcePath = Path.Combine(workDir, "source.txt");
        var followUpPath = Path.Combine(workDir, "followup.txt");
        await File.WriteAllTextAsync(sourcePath, "4", CancellationToken.None);

        var transformation = new MrTransformation(
            "ScalarMultiply",
            new Dictionary<string, string> { ["multiplier"] = "3" });
        var generator = new InputGenerator(
            new PythonInputAdapter(TestAssetPaths.PythonExecutable()),
            Path.Combine(TestAssetPaths.AssetRoot(), "example_output_adapter.py"));

        var result = await generator.GenerateAsync(
            sourcePath, followUpPath, transformation, CancellationToken.None);

        Assert.True(result.Succeeded, result.FailureReason);
        Assert.Equal(sourcePath, result.SourceInputPath);
        Assert.Equal(followUpPath, result.FollowUpInputPath);
        Assert.Same(transformation, result.Transformation);
        Assert.Contains("Multiplied 4.0 by 3", result.Log);
        Assert.Equal("12.0", (await File.ReadAllTextAsync(followUpPath, CancellationToken.None)).Trim());
    }

    [Fact]
    public async Task GenerateAsync_returns_failure_result_when_source_missing()
    {
        var transformation = new MrTransformation(
            "ScalarMultiply",
            new Dictionary<string, string> { ["multiplier"] = "2" });
        var generator = new InputGenerator(
            new PythonInputAdapter(TestAssetPaths.PythonExecutable()),
            Path.Combine(TestAssetPaths.AssetRoot(), "example_output_adapter.py"));

        var result = await generator.GenerateAsync(
            Path.Combine(Path.GetTempPath(), "does-not-exist.txt"),
            Path.Combine(Path.GetTempPath(), "followup.txt"),
            transformation,
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains("source input file does not exist", result.FailureReason, StringComparison.OrdinalIgnoreCase);
    }
}
