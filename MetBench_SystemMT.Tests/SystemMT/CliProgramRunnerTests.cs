using MetBench_BLL;
using MetBench_BLL.SystemMT;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT;

public sealed class CliProgramRunnerTests
{
    [Fact]
    public async Task RunAsync_starts_program_and_writes_output_file()
    {
        var assetRoot = TestAssetPaths.AssetRoot();
        var workDir = Path.Combine(Path.GetTempPath(), "MetBenchSystemMt", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workDir);
        var inputPath = Path.Combine(workDir, "input.txt");
        var outputPath = Path.Combine(workDir, "output.txt");
        await File.WriteAllTextAsync(inputPath, "7", CancellationToken.None);

        var program = new SystemProgram(
            ProgramLanguage.Python,
            "example-cli",
            TestAssetPaths.PythonExecutable(),
            $"{Path.Combine(assetRoot, "example_cli.py")} --input {{input}} --output {{output}}",
            Path.Combine(assetRoot, "example_output_adapter.py"));
        var testCase = new SystemMtCase("source", inputPath, workDir, outputPath);
        var runner = new CliProgramRunner();

        var result = await runner.RunAsync(program, testCase, TimeSpan.FromSeconds(10), CancellationToken.None);

        Assert.True(result.Succeeded, result.FailureReason);
        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(outputPath));
        Assert.Contains("result=7", await File.ReadAllTextAsync(outputPath, CancellationToken.None));
    }

    [Fact]
    public async Task RunAsync_reports_missing_input_file()
    {
        var assetRoot = TestAssetPaths.AssetRoot();
        var workDir = Path.Combine(Path.GetTempPath(), "MetBenchSystemMt", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workDir);

        var program = new SystemProgram(
            ProgramLanguage.Python,
            "example-cli",
            TestAssetPaths.PythonExecutable(),
            $"{Path.Combine(assetRoot, "example_cli.py")} --input {{input}} --output {{output}}",
            Path.Combine(assetRoot, "example_output_adapter.py"));
        var testCase = new SystemMtCase(
            "source",
            Path.Combine(workDir, "missing.txt"),
            workDir,
            Path.Combine(workDir, "output.txt"));
        var runner = new CliProgramRunner();

        var result = await runner.RunAsync(program, testCase, TimeSpan.FromSeconds(10), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains("Input file does not exist", result.FailureReason);
    }
}
